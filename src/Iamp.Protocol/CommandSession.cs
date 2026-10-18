using System.Runtime.ExceptionServices;
using System.Diagnostics;

namespace Iamp.Protocol;

/// <summary>Serializes commands and permanently stops a failed session. The writer starts under the notification gate.</summary>
public sealed class CommandSession : IDisposable
{
    private readonly Func<byte[], CancellationToken, Task> writer;
    private readonly TimeSpan timeout;
    private readonly SemaphoreSlim queue = new(1, 1);
    private readonly object gate = new();
    private readonly NotificationSessionGuard notifications = new();
    private Func<byte[], bool>? predicate;
    private TaskCompletionSource<byte[]>? pending;
    private CancellationTokenSource? pendingCancellation;
    private Exception? failure;
    private bool disposed;
    private readonly DiagnosticJournal? diagnostics;

    /// <summary>Validated frames that did not match the active query. Each subscriber receives its own copy outside the command gate.</summary>
    public event Action<byte[]>? UnsolicitedPayloadReceived;

    public bool IsAvailable
    {
        get { lock (gate) return !disposed && failure is null; }
    }

    public CommandSession(Func<byte[], CancellationToken, Task> writer, TimeSpan? timeout = null, DiagnosticJournal? diagnostics = null)
    {
        this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this.diagnostics = diagnostics;
        this.timeout = timeout ?? TimeSpan.FromSeconds(15);
        if (this.timeout <= TimeSpan.Zero || this.timeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(timeout));
    }

    public async Task<byte[]> QueryAsync(byte[] request, Func<byte[], bool> match, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(match);
        return (await ExecuteAsync(request, match, ct))!;
    }

    public async Task WriteAsync(byte[] request, CancellationToken ct)
        => _ = await ExecuteAsync(request, null, ct);

    private async Task<byte[]?> ExecuteAsync(byte[] request, Func<byte[], bool>? match, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Cancellation before acquiring the queue does not affect another active command.
        await queue.WaitAsync(ct);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(timeout);
        var kind = DiagnosticJournal.CommandKind(request);
        var started = false;
        var began = Stopwatch.GetTimestamp();
        try
        {
            Task writeTask;
            TaskCompletionSource<byte[]>? completion = null;
            lock (gate)
            {
                ThrowIfUnavailableCore();
                deadline.Token.ThrowIfCancellationRequested();
                predicate = match;
                if (match is not null)
                    completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                pending = completion;
                pendingCancellation = deadline;
                diagnostics?.Record(kind, DiagnosticOutcome.Started);
                started = true;
                // Copy caller-owned data. Writer must initiate the transport operation here, before returning its Task.
                writeTask = writer(request.ToArray(), deadline.Token);
                // A timed-out native operation may finish later; observe its failure without replaying it.
                _ = writeTask.ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            // Bound even transports that cannot promptly cancel their underlying I/O.
            await writeTask.WaitAsync(deadline.Token);
            lock (gate) { ThrowIfUnavailableCore(); deadline.Token.ThrowIfCancellationRequested(); }
            var response = completion is null ? null : await completion.Task.WaitAsync(deadline.Token);
            lock (gate) { ThrowIfUnavailableCore(); deadline.Token.ThrowIfCancellationRequested(); }
            diagnostics?.Record(kind, DiagnosticOutcome.Completed, (long)Stopwatch.GetElapsedTime(began).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            lock (gate)
            {
                var outcome = DiagnosticJournal.OutcomeFor(failure ?? ex,
                    failure is null && deadline.IsCancellationRequested && !ct.IsCancellationRequested);
                diagnostics?.Record(kind, started ? outcome : DiagnosticOutcome.Rejected,
                    (long)Stopwatch.GetElapsedTime(began).TotalMilliseconds);
                FailCore(ex, outcome);
                ThrowIfUnavailableCore();
            }
            throw;
        }
        finally
        {
            lock (gate) { predicate = null; pending = null; pendingCancellation = null; }
            queue.Release();
        }
    }

    public void Receive(ReadOnlySpan<byte> bytes)
    {
        List<byte[]> unsolicited = [];
        Delegate[] subscribers;
        lock (gate)
        {
            if (disposed || failure is not null) return;
            try
            {
                // Inspect the whole batch before accepting any matching reply.
                foreach (var frame in notifications.ProcessNotification(bytes))
                {
                    if (predicate?.Invoke(frame.Payload) == true) pending?.TrySetResult(frame.Payload);
                    else unsolicited.Add(frame.Payload);
                }
                subscribers = UnsolicitedPayloadReceived?.GetInvocationList() ?? [];
            }
            catch (Exception ex) { FailCore(ex); return; }
        }
        // A writer may synchronously feed Receive while ExecuteAsync still owns this reentrant gate.
        // Defer that unusual path so consumers never run under the writer's outer gate either.
        if (Monitor.IsEntered(gate))
            ThreadPool.QueueUserWorkItem(_ => PublishUnsolicited(unsolicited, subscribers));
        else PublishUnsolicited(unsolicited, subscribers);
    }

    private void PublishUnsolicited(List<byte[]> unsolicited, Delegate[] subscribers)
    {
        foreach (var payload in unsolicited)
        foreach (var subscriber in subscribers)
        {
            // A previous subscriber can close/fail the session. Do not publish its remaining batch afterward.
            lock (gate) { if (disposed || failure is not null) return; }
            try { ((Action<byte[]>)subscriber)(payload.ToArray()); }
            catch (Exception) { /* Consumer failures must not break command correlation or the transport callback. */ }
        }
    }

    public void Fail(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        lock (gate) { if (!disposed) FailCore(error); }
    }

    private void FailCore(Exception error, DiagnosticOutcome? outcome = null)
    {
        if (failure is null) diagnostics?.Record(DiagnosticOperation.Session, outcome ?? DiagnosticJournal.OutcomeFor(error));
        failure ??= error;
        notifications.ResetDecoder();
        // The stored failure is rethrown by ExecuteAsync. Cancel the wait without creating an unobserved faulted Task.
        pending?.TrySetCanceled();
        pendingCancellation?.Cancel();
    }

    public void ThrowIfUnavailable()
    {
        lock (gate) ThrowIfUnavailableCore();
    }

    private void ThrowIfUnavailableCore()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        notifications.ThrowIfInvalid();
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            FailCore(new ObjectDisposedException(nameof(CommandSession)));
        }
    }
}
