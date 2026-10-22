using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class FootswitchStatusReadTests
{
    [Fact]
    public void StartupRequestAndReplyMatchMobileButGlobalFieldsRemainUninterpreted()
    {
        Assert.Equal(new byte[]{3},FootswitchCommands.CreateStatusReadCommand());
        Assert.Equal("AA55010003F8AC",Convert.ToHexString(FrameCodec.Encode(FootswitchCommands.CreateStatusReadCommand())));
        FootswitchCommands.RequireStatusReadReply([4,1,0,0,0,0]);
        FootswitchCommands.RequireStatusReadReply([4,255,254,253,252,251]);
    }

    public static IEnumerable<object[]> MalformedReplies()=>new byte[][]{
        [],[4],[4,1,0,0,0],[4,1,0,0,0,0,0],[3,1,0,0,0,0],[0x97,1]
    }.Select(payload=>new object[]{payload});

    [Theory]
    [MemberData(nameof(MalformedReplies))]
    public void MalformedStartupRepliesAreRejected(byte[] payload)
        =>Assert.Throws<IOException>(()=>FootswitchCommands.RequireStatusReadReply(payload));

    [Theory]
    [InlineData("startupDisconnected",FootswitchConnectionStatus.Disconnected)]
    [InlineData("startupConnected",FootswitchConnectionStatus.Connected)]
    public async Task CapturedStartupReadObtainsOnlySeparateStatusNotification(string operation,FootswitchConnectionStatus expected)
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-footswitch-observations.json")));
        var writes=new List<byte[]>();using var session=new CommandSession((request,_)=>{writes.Add(request.ToArray());return Task.CompletedTask;});
        var monitor=new FootswitchStatusMonitor();session.UnsolicitedPayloadReceived+=payload=>monitor.Observe(payload);
        var request=FootswitchCommands.CreateStatusReadCommand();var reply=session.QueryAsync(request,p=>p.Length>0&&p[0]==4,default);
        foreach(var frame in fixture.RootElement.GetProperty(operation).EnumerateArray()){
            Assert.Equal(frame.GetProperty("frameHex").GetString(),Convert.ToHexString(FrameCodec.Encode(Convert.FromHexString(frame.GetProperty("payloadHex").GetString()!))));
            if(frame.GetProperty("direction").GetString()=="incoming")session.Receive(Convert.FromHexString(frame.GetProperty("frameHex").GetString()!));
        }
        FootswitchCommands.RequireStatusReadReply(await reply.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(expected,monitor.Status);Assert.Equal(request,Assert.Single(writes));
    }

    [Fact]
    public async Task ReplyAloneLeavesStatusUnknownAndLaterNotificationStillUpdatesIt()
    {
        var writes=0;using var session=new CommandSession((_,_)=>{writes++;return Task.CompletedTask;});
        var monitor=new FootswitchStatusMonitor();session.UnsolicitedPayloadReceived+=payload=>monitor.Observe(payload);
        var reply=session.QueryAsync(FootswitchCommands.CreateStatusReadCommand(),p=>p.Length>0&&p[0]==4,default);
        session.Receive(FrameCodec.Encode([4,1,0,0,0,0]));FootswitchCommands.RequireStatusReadReply(await reply.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(FootswitchConnectionStatus.Unknown,monitor.Status);
        session.Receive(FrameCodec.Encode([0x97,1]));Assert.Equal(FootswitchConnectionStatus.Connected,monitor.Status);Assert.Equal(1,writes);
    }
}
