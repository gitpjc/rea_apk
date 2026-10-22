using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class FootswitchPairingTests
{
    [Theory]
    [InlineData(false,"9500","AA5502009500F589")]
    [InlineData(true,"9501","AA5502009501E5A8")]
    public void ExplicitPairingCommandsMatchMobileFrames(bool pairing,string payloadHex,string frameHex)
    {
        var command=FootswitchCommands.CreatePairingCommand(pairing);
        Assert.Equal(payloadHex,Convert.ToHexString(command));Assert.Equal(frameHex,Convert.ToHexString(FrameCodec.Encode(command)));
    }

    public static IEnumerable<object[]> UnobservedAcknowledgements()=>new byte[][]{
        [],[0x96],[0x96,0],[0x96,2],[0x96,255],[0x96,1,0],[0x97,1],[0x14,1]
    }.Select(bytes=>new object[]{bytes});

    [Theory]
    [MemberData(nameof(UnobservedAcknowledgements))]
    public void OnlyExactObservedAcceptedAcknowledgementIsAccepted(byte[] payload)
    {
        FootswitchCommands.RequirePairingAcknowledgement([0x96,1]);
        Assert.Throws<IOException>(()=>FootswitchCommands.RequirePairingAcknowledgement(payload));
    }

    [Theory]
    [InlineData("unpair",false,FootswitchConnectionStatus.Disconnected)]
    [InlineData("pair",true,FootswitchConnectionStatus.Connected)]
    public async Task CapturedNotificationOrderingWorksBeforeOrAfterAcknowledgement(string operation,bool pairing,FootswitchConnectionStatus expected)
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-footswitch-observations.json")));
        var frames=fixture.RootElement.GetProperty(operation).EnumerateArray().ToArray();var writes=new List<byte[]>();
        using var session=new CommandSession((frame,_)=>{writes.Add(frame.ToArray());return Task.CompletedTask;});
        var monitor=new FootswitchStatusMonitor();var changes=new List<FootswitchConnectionStatus>();monitor.StatusChanged+=changes.Add;
        session.UnsolicitedPayloadReceived+=payload=>monitor.Observe(payload);
        var request=FootswitchCommands.CreatePairingCommand(pairing);
        var read=session.QueryAsync(request,p=>p.Length>0&&p[0]==0x96,default);
        foreach(var frame in frames){
            Assert.Equal(frame.GetProperty("frameHex").GetString(),Convert.ToHexString(FrameCodec.Encode(Convert.FromHexString(frame.GetProperty("payloadHex").GetString()!))));
            if(frame.GetProperty("direction").GetString()=="incoming")session.Receive(Convert.FromHexString(frame.GetProperty("frameHex").GetString()!));
        }
        FootswitchCommands.RequirePairingAcknowledgement(await read.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(expected,monitor.Status);Assert.Equal(expected,Assert.Single(changes));Assert.Equal(request,Assert.Single(writes));
    }

    [Fact]
    public async Task AcknowledgementAloneDoesNotInventConnectionAndCancellationDoesNotWriteCleanup()
    {
        var writes=new List<byte[]>();using var session=new CommandSession((request,_)=>{writes.Add(request.ToArray());return Task.CompletedTask;});
        var monitor=new FootswitchStatusMonitor();session.UnsolicitedPayloadReceived+=payload=>monitor.Observe(payload);
        var first=session.QueryAsync(FootswitchCommands.CreatePairingCommand(true),p=>p.Length>0&&p[0]==0x96,default);
        session.Receive(FrameCodec.Encode([0x96,1]));FootswitchCommands.RequirePairingAcknowledgement(await first.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(FootswitchConnectionStatus.Unknown,monitor.Status);
        using var cancellation=new CancellationTokenSource();var next=session.QueryAsync(FootswitchCommands.CreatePairingCommand(true),p=>p.Length>0&&p[0]==0x96,cancellation.Token);
        cancellation.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(async()=>await next);
        Assert.Equal(2,writes.Count);Assert.All(writes,p=>Assert.Equal(new byte[]{0x95,1},p));Assert.Equal(FootswitchConnectionStatus.Unknown,monitor.Status);
    }
}
