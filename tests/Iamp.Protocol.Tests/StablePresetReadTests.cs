using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class StablePresetReadTests
{
    static (byte[] State,byte[] Detail) Captured()
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures/f15-observed-preset.json")));
        byte[] Payload(string name)=>FrameCodec.Decode(Convert.FromHexString(fixture.RootElement.GetProperty(name).GetProperty("frameHex").GetString()!)).Payload;
        return(Payload("state"),Payload("detail"));
    }

    [Fact]
    public async Task PhysicalSwitchDuringReadSettlesWithoutFailingTheWorkflow()
    {
        var (old,detail)=Captured();var next=old.ToArray();next[1]=17;var nextDetail=detail.ToArray();nextDetail[1]=17;
        var states=new Queue<byte[]>(new[]{old,next,next,next});var details=new Queue<byte[]>(new[]{detail,nextDetail});var indices=new List<byte>();
        using var workflow=new WorkflowQueue();
        var read=await workflow.RunAsync(ct=>CurrentPresetReadGuard.ReadStableAsync(
            _=>Task.FromResult(states.Dequeue()),(index,_)=>{indices.Add(index);return Task.FromResult(details.Dequeue());},ct),default);
        Assert.Equal(next,read.State);Assert.Equal(nextDetail,read.Detail);Assert.Equal((byte)17,read.Preset.WireIndex);
        Assert.Equal(new byte[]{16,17},indices);Assert.Empty(states);Assert.Empty(details);
        Assert.Equal(7,await workflow.RunAsync(_=>Task.FromResult(7),default));
    }

    [Fact]
    public async Task SamePresetGlobalChangeAndMismatchedDetailAreRetriedFromTheBeginning()
    {
        var (old,detail)=Captured();var after=old.ToArray();after[^1]^=1;
        var wrong=detail.ToArray();wrong[1]=17;
        var states=new Queue<byte[]>(new[]{old,after,after,after,after,after});
        var details=new Queue<byte[]>(new[]{detail,wrong,detail});
        var read=await CurrentPresetReadGuard.ReadStableAsync(_=>Task.FromResult(states.Dequeue()),(_,_)=>Task.FromResult(details.Dequeue()),default);
        Assert.Equal(after,read.State);Assert.Equal(detail,read.Detail);Assert.Empty(states);Assert.Empty(details);
    }

    [Fact]
    public async Task ContinuousChangesStopAfterThreeCompleteAttempts()
    {
        var (state,detail)=Captured();var changed=state.ToArray();changed[^1]^=1;var calls=0;var details=0;
        await Assert.ThrowsAsync<CurrentPresetChangedException>(()=>CurrentPresetReadGuard.ReadStableAsync(
            _=>Task.FromResult(++calls%2==1?state:changed),(_,_)=>{details++;return Task.FromResult(detail);},default));
        Assert.Equal(6,calls);Assert.Equal(3,details);
    }

    [Theory]
    [InlineData("transport")]
    [InlineData("timeout")]
    [InlineData("format")]
    [InlineData("cancelled")]
    [InlineData("transfer")]
    public async Task CommandFailuresAreNeverRetried(string kind)
    {
        var calls=0;
        Exception error=kind switch{"transport"=>new IOException("transport"),"timeout"=>new TimeoutException(),"format"=>new FormatException(),"cancelled"=>new OperationCanceledException(),_=>new TransferStateChangedException()};
        var actual=await Record.ExceptionAsync(()=>CurrentPresetReadGuard.ReadStableAsync(_=>{calls++;return Task.FromException<byte[]>(error);},(_,_)=>throw new Exception("No detail read expected"),default));
        Assert.Same(error,actual);Assert.Equal(1,calls);
    }

    [Fact]
    public async Task MalformedStateAndDetailDoNotEnterTheRetryLoop()
    {
        var (state,_)=Captured();var calls=0;
        await Assert.ThrowsAsync<FormatException>(()=>CurrentPresetReadGuard.ReadStableAsync(_=>{calls++;return Task.FromResult(new byte[]{0x12});},(_,_)=>throw new Exception(),default));
        Assert.Equal(1,calls);calls=0;
        await Assert.ThrowsAsync<FormatException>(()=>CurrentPresetReadGuard.ReadStableAsync(_=>{calls++;return Task.FromResult(state);},(_,_)=>Task.FromResult(new byte[]{0x22}),default));
        Assert.Equal(2,calls);
    }

    [Fact]
    public async Task CancellationDuringSettlingPreventsAnotherQuery()
    {
        var (state,detail)=Captured();var changed=state.ToArray();changed[^1]^=1;using var ct=new CancellationTokenSource();var calls=0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>CurrentPresetReadGuard.ReadStableAsync(
            _=>{calls++;return Task.FromResult(calls==1?state:changed);},(_,_)=>{ct.Cancel();return Task.FromResult(detail);},ct.Token));
        Assert.Equal(2,calls);
    }
}
