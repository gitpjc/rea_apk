using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class WorkflowQueueTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    [Fact]
    public async Task EntireCommandSequencesStayTogetherAcrossAwaitBoundaries()
    {
        using var queue = new WorkflowQueue();
        var steps = new List<byte>(); var entered = Signal(); var release = Signal();
        using var commands = new CommandSession((payload, _) => { steps.Add(payload[0]); return Task.CompletedTask; });
        var first = queue.RunAsync(async ct =>
        {
            await commands.WriteAsync([0x11], ct); entered.SetResult();
            await release.Task.WaitAsync(ct);
            await commands.WriteAsync([0x21], ct); await commands.WriteAsync([0x11], ct);
            return 1;
        }, default);
        await entered.Task.WaitAsync(Limit);
        var second = queue.RunAsync(async ct => { await commands.WriteAsync([0x29], ct); return 2; }, default);
        Assert.Equal(new byte[] { 0x11 }, steps); Assert.False(second.IsCompleted);
        release.SetResult();
        Assert.Equal(1, await first.WaitAsync(Limit)); Assert.Equal(2, await second.WaitAsync(Limit));
        Assert.Equal(new byte[] { 0x11, 0x21, 0x11, 0x29 }, steps);
    }

    [Fact]
    public async Task CancellingQueuedWorkflowDoesNotEnterItOrInterruptActiveWorkflow()
    {
        using var queue = new WorkflowQueue(); using var cancel = new CancellationTokenSource();
        var entered = Signal(); var release = Signal(); var queuedEntered = false;
        var first = queue.RunAsync(async ct => { entered.SetResult(); await release.Task.WaitAsync(ct); return 1; }, default);
        await entered.Task.WaitAsync(Limit);
        var second = queue.RunAsync(ct => { queuedEntered = true; return Task.FromResult(2); }, cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.WaitAsync(Limit));
        Assert.False(queuedEntered); Assert.False(first.IsCompleted);
        release.SetResult(); await first.WaitAsync(Limit);
        Assert.Equal(3, await queue.RunAsync(_ => Task.FromResult(3), default));
    }

    [Fact]
    public async Task ActiveCancellationBlocksQueuedWorkflowsWithoutReplay()
    {
        using var queue = new WorkflowQueue(); using var cancel = new CancellationTokenSource();
        var entered = Signal(); var release = Signal(); var queuedEntered = false;
        var first = queue.RunAsync(async ct => { entered.SetResult(); await release.Task.WaitAsync(ct); return 1; }, cancel.Token);
        await entered.Task.WaitAsync(Limit);
        var second = queue.RunAsync(_ => { queuedEntered = true; return Task.FromResult(2); }, default);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(Limit));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.WaitAsync(Limit));
        Assert.False(queuedEntered);
    }

    [Fact]
    public async Task FailureAfterInitialStepsPreventsQueuedAndFutureActions()
    {
        using var queue = new WorkflowQueue(); var entered = Signal(); var release = Signal(); var count = 0;
        var first = queue.RunAsync<int>(async ct => { count++; entered.SetResult(); await release.Task.WaitAsync(ct); throw new IOException("Readback failed"); }, default);
        await entered.Task.WaitAsync(Limit);
        var second = queue.RunAsync(_ => { count++; return Task.FromResult(2); }, default);
        release.SetResult();
        await Assert.ThrowsAsync<IOException>(() => first.WaitAsync(Limit));
        await Assert.ThrowsAsync<IOException>(() => second.WaitAsync(Limit));
        await Assert.ThrowsAsync<IOException>(() => queue.RunAsync(_ => { count++; return Task.FromResult(3); }, default));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DisposeCancelsActiveAndWaitingWorkflows()
    {
        using var queue = new WorkflowQueue(); var entered = Signal(); var release = Signal(); var queuedEntered = false;
        var first = queue.RunAsync(async ct => { entered.SetResult(); await release.Task.WaitAsync(ct); return 1; }, default);
        await entered.Task.WaitAsync(Limit);
        var second = queue.RunAsync(_ => { queuedEntered = true; return Task.FromResult(2); }, default);
        queue.Dispose(); queue.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => first.WaitAsync(Limit));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => second.WaitAsync(Limit));
        Assert.False(queuedEntered);
    }

    [Fact]
    public async Task UncooperativeActionIsNotAbandonedWhileStillRunning()
    {
        using var queue = new WorkflowQueue(); var entered = Signal(); var release = Signal(); var queuedEntered = false;
        var first = queue.RunAsync(async _ => { entered.SetResult(); await release.Task; return 1; }, default);
        await entered.Task.WaitAsync(Limit);
        var second = queue.RunAsync(_ => { queuedEntered = true; return Task.FromResult(2); }, default);
        queue.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => second.WaitAsync(Limit));
        Assert.False(first.IsCompleted); Assert.False(queuedEntered);
        release.SetResult(); await Assert.ThrowsAsync<ObjectDisposedException>(() => first.WaitAsync(Limit));
    }

    [Fact]
    public async Task SwallowedCancellationCannotReportSuccessOrAllowNextWorkflow()
    {
        using var queue = new WorkflowQueue(); using var cancel = new CancellationTokenSource();
        var first = queue.RunAsync(_ => { cancel.Cancel(); return Task.FromResult(1); }, cancel.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(Limit));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.RunAsync(_ => Task.FromResult(2), default));
    }

    [Fact]
    public async Task SecondEditMustRecheckStateAfterFirstCompleteWorkflow()
    {
        using var queue = new WorkflowQueue(); var checkedFirst = Signal(); var release = Signal(); var value = 25; var writes = 0;
        var first = queue.RunAsync(async ct =>
        {
            if (value != 25) throw new IOException("Stale snapshot");
            checkedFirst.SetResult(); await release.Task.WaitAsync(ct);
            value = 26; writes++;
            Assert.Equal(26, value); return value;
        }, default);
        await checkedFirst.Task.WaitAsync(Limit);
        var second = queue.RunAsync(_ =>
        {
            if (value != 25) throw new IOException("Stale snapshot");
            value = 27; writes++; return Task.FromResult(value);
        }, default);
        release.SetResult(); Assert.Equal(26, await first.WaitAsync(Limit));
        await Assert.ThrowsAsync<IOException>(() => second.WaitAsync(Limit));
        Assert.Equal(1, writes); Assert.Equal(26, value);
    }
}
