using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class FootswitchStatusTests
{
    [Fact]
    public void ObservedPowerOffAndOnNotificationsDescribeConnectionNotPairing()
    {
        Assert.Equal(FootswitchConnectionStatus.Disconnected,FootswitchStatusNotification.Parse([0x97,0]));
        Assert.Equal(FootswitchConnectionStatus.Connected,FootswitchStatusNotification.Parse([0x97,1]));
    }

    public static IEnumerable<object[]> InvalidPayloads()=>new byte[][]{
        [],[0x97],[0x97,0,0],[0x97,1,0],[0x97,2],[0x97,255],[0x96,1],[0x12,1]
    }.Select(bytes=>new object[]{bytes});

    [Theory]
    [MemberData(nameof(InvalidPayloads))]
    public void UnknownOrMalformedPacketsAreRejectedWithoutChangingStatus(byte[] payload)
    {
        Assert.False(FootswitchStatusNotification.TryParse(payload,out var status));
        Assert.Equal(FootswitchConnectionStatus.Unknown,status);
        Assert.Throws<FormatException>(()=>FootswitchStatusNotification.Parse(payload));
        var monitor=new FootswitchStatusMonitor();monitor.Observe([0x97,1]);
        Assert.False(monitor.Observe(payload));Assert.Equal(FootswitchConnectionStatus.Connected,monitor.Status);
    }

    [Fact]
    public void NewSessionStartsUnknownAndDuplicateNotificationsAreHarmless()
    {
        var monitor=new FootswitchStatusMonitor();var changes=new List<FootswitchConnectionStatus>();monitor.StatusChanged+=changes.Add;
        Assert.Equal(FootswitchConnectionStatus.Unknown,monitor.Status);
        Assert.True(monitor.Observe([0x97,0]));Assert.True(monitor.Observe([0x97,0]));
        Assert.True(monitor.Observe([0x97,1]));Assert.True(monitor.Observe([0x97,1]));Assert.True(monitor.Observe([0x97,1]));
        Assert.Equal(new[]{FootswitchConnectionStatus.Disconnected,FootswitchConnectionStatus.Connected},changes);
        Assert.Equal(FootswitchConnectionStatus.Connected,monitor.Status);
        Assert.Equal(FootswitchConnectionStatus.Unknown,new FootswitchStatusMonitor().Status);
    }

    [Fact]
    public void ConsumerFailureDoesNotDropStatusOrOtherConsumers()
    {
        var monitor=new FootswitchStatusMonitor();var received=new List<FootswitchConnectionStatus>();
        monitor.StatusChanged+=_=>throw new InvalidOperationException("Consumer failure");monitor.StatusChanged+=received.Add;
        Assert.True(monitor.Observe([0x97,1]));
        Assert.Equal(FootswitchConnectionStatus.Connected,monitor.Status);Assert.Equal(FootswitchConnectionStatus.Connected,Assert.Single(received));
    }

    [Fact]
    public async Task StatusNotificationsDoNotRequireQueriesOrInterfereWithAnActivePresetRead()
    {
        var writes=0;using var session=new CommandSession((_,_)=>{writes++;return Task.CompletedTask;});
        var monitor=new FootswitchStatusMonitor();session.UnsolicitedPayloadReceived+=payload=>monitor.Observe(payload);
        session.Receive(FrameCodec.Encode([0x97,1]));
        Assert.Equal(0,writes);Assert.Equal(FootswitchConnectionStatus.Connected,monitor.Status);
        var read=session.QueryAsync([0x11],p=>p.Length==2&&p[0]==0x12,default);
        session.Receive(FrameCodec.Encode([0x97,0]).Concat(FrameCodec.Encode([0x12,4])).ToArray());
        Assert.Equal(new byte[]{0x12,4},await read.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(1,writes);Assert.Equal(FootswitchConnectionStatus.Disconnected,monitor.Status);
    }
}
