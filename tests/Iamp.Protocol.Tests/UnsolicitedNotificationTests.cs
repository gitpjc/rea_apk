using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class UnsolicitedNotificationTests
{
    private static CommandSession Session() => new((_, _) => Task.CompletedTask);
    private static byte[] Reading(byte value) => FrameCodec.Encode([0x82, value]);

    [Fact]
    public void FragmentedAndCoalescedFramesPublishOnlyCompletePayloadsInOrder()
    {
        using var session = Session();
        List<byte[]> received = [];
        session.UnsolicitedPayloadReceived += received.Add;
        var first = Reading(1);
        session.Receive(first.AsSpan(0, 3));
        Assert.Empty(received);
        session.Receive(first.AsSpan(3).ToArray().Concat(Reading(2)).ToArray());
        Assert.Equal(2, received.Count);
        Assert.Equal(new byte[] { 0x82, 1 }, received[0]);
        Assert.Equal(new byte[] { 0x82, 2 }, received[1]);
    }

    [Fact]
    public async Task MatchingRepliesRemainExclusiveToTheActiveQuery()
    {
        using var session = Session();
        List<byte[]> received = [];
        session.UnsolicitedPayloadReceived += received.Add;
        var query = session.QueryAsync([0x11], p => p[0] == 0x12, default);
        session.Receive(Reading(3).Concat(FrameCodec.Encode([0x12, 4])).ToArray());
        Assert.Equal(new byte[] { 0x12, 4 }, await query.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(new byte[] { 0x82, 3 }, Assert.Single(received));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransferStateAnywhereInBatchSuppressesAllDeliveriesAndMatchingReply(bool transferFirst)
    {
        using var session = Session();
        List<byte[]> received = [];
        session.UnsolicitedPayloadReceived += received.Add;
        var query = session.QueryAsync([0x11], p => p[0] == 0x12, default);
        var ordinary = Reading(3).Concat(FrameCodec.Encode([0x12, 4]));
        var transfer = FrameCodec.Encode([0x9B, 0]);
        session.Receive(transferFirst ? transfer.Concat(ordinary).ToArray() : ordinary.Concat(transfer).ToArray());
        await Assert.ThrowsAsync<TransferStateChangedException>(() => query);
        Assert.Empty(received);
        Assert.False(session.IsAvailable);
    }

    [Fact]
    public async Task SubscribersReceiveIndependentCopiesOutsideGateAndExceptionsAreIsolated()
    {
        using var session = Session();
        List<byte[]> received = [];
        session.UnsolicitedPayloadReceived += payload => { payload[1] = 99; throw new InvalidOperationException("Consumer error"); };
        session.UnsolicitedPayloadReceived += payload =>
        {
            // A separate thread can acquire the gate during this callback.
            Assert.True(Task.Run(() => session.IsAvailable).Wait(TimeSpan.FromSeconds(2)));
            received.Add(payload);
        };
        session.Receive(Reading(7));
        Assert.Equal(new byte[] { 0x82, 7 }, Assert.Single(received));
        Assert.True(session.IsAvailable);
        await session.WriteAsync([0x11], default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailureOrDisposalSuppressesLaterNotifications(bool dispose)
    {
        using var session = Session();
        var count = 0;
        session.UnsolicitedPayloadReceived += _ => count++;
        Assert.True(session.IsAvailable);
        if (dispose) session.Dispose(); else session.Fail(new IOException("Disconnected"));
        session.Receive(Reading(1));
        Assert.False(session.IsAvailable);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SynchronousWriterNotificationDoesNotInvokeSubscribersUnderOuterGate()
    {
        CommandSession? session = null;
        using var created = session = new CommandSession((_, _) =>
        {
            session!.Receive(Reading(8));
            return Task.CompletedTask;
        });
        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.UnsolicitedPayloadReceived += _ =>
        {
            try { received.TrySetResult(Task.Run(() => session.IsAvailable).Wait(TimeSpan.FromSeconds(2))); }
            catch (Exception error) { received.TrySetException(error); }
        };
        await session.WriteAsync([0x11], default);
        Assert.True(await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void SubscriberDisposalStopsRemainingSubscribersAndBatch()
    {
        using var session = Session();
        var count = 0;
        session.UnsolicitedPayloadReceived += _ => session.Dispose();
        session.UnsolicitedPayloadReceived += _ => count++;
        session.Receive(Reading(1).Concat(Reading(2)).ToArray());
        Assert.Equal(0, count);
    }
}
