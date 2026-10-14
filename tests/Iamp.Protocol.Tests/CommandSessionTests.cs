using System.Threading.Channels;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class CommandSessionTests
{
    private static readonly TimeSpan TestDeadline = TimeSpan.FromSeconds(5);
    private static byte[] Ack(byte opcode = 0x14) => FrameCodec.Encode([opcode, 1]);
    private static bool Matches(byte[] payload) => payload.Length == 2 && payload[0] == 0x14;

    private sealed class Transport : IDisposable
    {
        private readonly Channel<byte[]> writes = Channel.CreateUnbounded<byte[]>();
        public readonly TaskCompletionSource WriteCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CommandSession Session { get; }
        public int Count { get; private set; }
        public CancellationToken LastToken { get; private set; }

        public Transport(bool blockWrite = false, TimeSpan? timeout = null)
        {
            Session = new((payload, ct) =>
            {
                Count++; LastToken = ct; writes.Writer.TryWrite(payload);
                return blockWrite ? WriteCompletion.Task : Task.CompletedTask;
            }, timeout);
        }
        public async Task<byte[]> NextWrite() => await writes.Reader.ReadAsync().AsTask().WaitAsync(TestDeadline);
        public void Dispose() => Session.Dispose();
    }

    [Fact]
    public async Task SerializesQueriesAndCorrelatesOnlyMatchingResponses()
    {
        using var transport = new Transport();
        var first = transport.Session.QueryAsync([0x13, 4], Matches, default);
        Assert.Equal(new byte[] { 0x13, 4 }, await transport.NextWrite());
        var second = transport.Session.QueryAsync([0x11], p => p.Length == 2 && p[0] == 0x12, default);
        transport.Session.Receive(Ack(0x12));
        Assert.False(first.IsCompleted); Assert.Equal(1, transport.Count);
        transport.Session.Receive(Ack());
        Assert.Equal(new byte[] { 0x14, 1 }, await first.WaitAsync(TestDeadline));
        Assert.Equal(new byte[] { 0x11 }, await transport.NextWrite());
        transport.Session.Receive(Ack(0x12));
        Assert.Equal(new byte[] { 0x12, 1 }, await second.WaitAsync(TestDeadline));
    }

    [Fact]
    public async Task ReceivesFragmentedResponseAndIgnoresBadChecksum()
    {
        using var transport = new Transport();
        var task = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite();
        var corrupt = Ack(); corrupt[^1] ^= 1;
        transport.Session.Receive(corrupt);
        var ack = Ack(); transport.Session.Receive(ack.AsSpan(0, 4));
        Assert.False(task.IsCompleted);
        transport.Session.Receive(ack.AsSpan(4));
        Assert.Equal(new byte[] { 0x14, 1 }, await task.WaitAsync(TestDeadline));
    }

    [Fact]
    public async Task DisconnectCancelsActiveTransportAndBlocksQueuedAndFutureWrites()
    {
        using var transport = new Transport(blockWrite: true);
        var first = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite();
        var second = transport.Session.WriteAsync([0x30, 1], default);
        transport.Session.Fail(new IOException("Disconnected"));
        Assert.True(transport.LastToken.IsCancellationRequested);
        transport.Session.Receive(Ack());
        await Assert.ThrowsAsync<IOException>(() => first.WaitAsync(TestDeadline));
        await Assert.ThrowsAsync<IOException>(() => second.WaitAsync(TestDeadline));
        await Assert.ThrowsAsync<IOException>(() => transport.Session.WriteAsync([0x11], default));
        Assert.Equal(1, transport.Count);
        transport.WriteCompletion.TrySetResult();
    }

    [Fact]
    public async Task DisconnectAfterMatchingReplyButBeforeWriteCompletionStillFails()
    {
        using var transport = new Transport(blockWrite: true);
        var task = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite();
        transport.Session.Receive(Ack());
        transport.Session.Fail(new IOException("Disconnected"));
        transport.WriteCompletion.TrySetResult();
        await Assert.ThrowsAsync<IOException>(() => task.WaitAsync(TestDeadline));
    }

    [Fact]
    public async Task CancellationOfQueuedRequestDoesNotCancelActiveRequest()
    {
        using var transport = new Transport();
        var active = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite();
        using var cancel = new CancellationTokenSource();
        var queued = transport.Session.WriteAsync([0x13, 5], cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued.WaitAsync(TestDeadline));
        Assert.False(transport.LastToken.IsCancellationRequested);
        transport.Session.Receive(Ack());
        await active.WaitAsync(TestDeadline);
        await transport.Session.WriteAsync([0x11], default);
        Assert.Equal(2, transport.Count);
    }

    [Fact]
    public async Task ActiveCancellationIsTerminalAndLateReplyCannotReenableSession()
    {
        using var transport = new Transport();
        using var cancel = new CancellationTokenSource();
        var task = transport.Session.QueryAsync([0x13, 4], Matches, cancel.Token);
        await transport.NextWrite(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TestDeadline));
        transport.Session.Receive(Ack());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.Session.WriteAsync([0x11], default));
        Assert.Equal(1, transport.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TimeoutIsTerminalWithNoAutomaticReplay(bool blockWrite)
    {
        using var transport = new Transport(blockWrite, TimeSpan.FromMilliseconds(100));
        var task = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TestDeadline));
        transport.Session.Receive(Ack());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.Session.WriteAsync([0x11], default));
        Assert.Equal(1, transport.Count);
        transport.WriteCompletion.TrySetResult();
    }

    [Fact]
    public async Task DisposeInterruptsActiveAndQueuedRequestsAndIgnoresLateNotifications()
    {
        using var transport = new Transport(blockWrite: true);
        var first = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite();
        var second = transport.Session.WriteAsync([0x13, 5], default);
        transport.Session.Dispose(); transport.Session.Dispose();
        transport.Session.Receive(Ack());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => first.WaitAsync(TestDeadline));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => second.WaitAsync(TestDeadline));
        Assert.Equal(1, transport.Count);
        transport.WriteCompletion.TrySetResult();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CoalescedTransferStatusRejectsReplyAndStopsQueuedCommands(bool statusFirst)
    {
        using var transport = new Transport();
        var first = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite();
        var second = transport.Session.WriteAsync([0x11], default);
        var status = FrameCodec.Encode([0x9B, 0]);
        transport.Session.Receive(statusFirst ? status.Concat(Ack()).ToArray() : Ack().Concat(status).ToArray());
        await Assert.ThrowsAsync<TransferStateChangedException>(() => first.WaitAsync(TestDeadline));
        await Assert.ThrowsAsync<TransferStateChangedException>(() => second.WaitAsync(TestDeadline));
        transport.Session.Receive(FrameCodec.Encode([0x9B, 1]));
        Assert.Throws<TransferStateChangedException>(() => transport.Session.ThrowIfUnavailable());
        Assert.Equal(1, transport.Count);
    }

    [Fact]
    public async Task WriteFailureCannotBeOverriddenByAnEarlySuccessfulReply()
    {
        using var transport = new Transport(blockWrite: true);
        var task = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite(); transport.Session.Receive(Ack());
        transport.WriteCompletion.SetException(new IOException("Write failed"));
        await Assert.ThrowsAsync<IOException>(() => task.WaitAsync(TestDeadline));
        await Assert.ThrowsAsync<IOException>(() => transport.Session.WriteAsync([0x11], default));
        Assert.Equal(1, transport.Count);
    }

    [Fact]
    public async Task WriteOnlyCommandsAlsoStopOnTransferStatus()
    {
        using var transport = new Transport(blockWrite: true);
        var task = transport.Session.WriteAsync([0x13, 4], default);
        await transport.NextWrite();
        transport.Session.Receive(FrameCodec.Encode([0x9B, 1]));
        await Assert.ThrowsAsync<TransferStateChangedException>(() => task.WaitAsync(TestDeadline));
        Assert.True(transport.LastToken.IsCancellationRequested);
        transport.WriteCompletion.TrySetResult();
    }

    [Fact]
    public async Task NewSessionDoesNotInheritOldSessionFragmentsOrFailure()
    {
        using var old = new Transport();
        old.Session.Receive(Ack().AsSpan(0, 4));
        old.Session.Fail(new IOException("Disconnected"));
        using var fresh = new Transport();
        var task = fresh.Session.QueryAsync([0x13, 4], Matches, default);
        await fresh.NextWrite();
        fresh.Session.Receive(Ack());
        Assert.Equal(new byte[] { 0x14, 1 }, await task.WaitAsync(TestDeadline));
        Assert.Equal(0, old.Count); Assert.Equal(1, fresh.Count);
    }

    [Fact]
    public async Task AlreadyCancelledRequestSendsNothingAndLeavesSessionUsable()
    {
        using var transport = new Transport();
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.Session.WriteAsync([0x11], cancel.Token));
        Assert.Equal(0, transport.Count);
        await transport.Session.WriteAsync([0x11], default);
        Assert.Equal(1, transport.Count);
    }

    [Fact]
    public async Task ReplyWhileIdleIsNotReusedForNextCommand()
    {
        using var transport = new Transport();
        transport.Session.Receive(Ack());
        var task = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite(); Assert.False(task.IsCompleted);
        transport.Session.Receive(Ack());
        await task.WaitAsync(TestDeadline);
    }

    [Fact]
    public async Task SuccessfulEarlyReplyStillWaitsForTransportCompletion()
    {
        using var transport = new Transport(blockWrite: true);
        var task = transport.Session.QueryAsync([0x13, 4], Matches, default);
        await transport.NextWrite(); transport.Session.Receive(Ack());
        Assert.False(task.IsCompleted);
        transport.WriteCompletion.SetResult();
        await task.WaitAsync(TestDeadline);
    }

    [Fact]
    public async Task LateTransportFailureAfterTimeoutDoesNotRestartOrReplaceSessionFailure()
    {
        using var transport = new Transport(blockWrite: true, TimeSpan.FromMilliseconds(100));
        var task = transport.Session.WriteAsync([0x13, 4], default);
        await transport.NextWrite();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TestDeadline));
        transport.WriteCompletion.SetException(new IOException("Late native failure"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.Session.WriteAsync([0x11], default));
        Assert.Equal(1, transport.Count);
    }
}
