using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class ToolRequestResponderTests
{
    [Fact]
    public async Task StartupAndToolsRequestsAreBothAcknowledgedOnce()
    {
        List<string> writes=[];var startupAck=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CommandSession? session=null;
        using var created=session=new CommandSession((payload,_)=> {
            var hex=Convert.ToHexString(payload);writes.Add(hex);
            if(hex=="0101")session!.Receive(FrameCodec.Encode([0x02,0x41,0x01]).Concat(FrameCodec.Encode([0x49,0x00])).ToArray());
            else if(hex=="4101")session!.Receive(FrameCodec.Encode([0x42,0x01]).Concat(FrameCodec.Encode([0x49,0x00])).ToArray());
            else if(hex=="4102")session!.Receive(FrameCodec.Encode([0x42,0x01]));
            else if(hex=="4A01")startupAck.TrySetResult();
            return Task.CompletedTask;
        });
        using var responder=new ToolRequestResponder(session);
        await session.QueryAsync([0x01,0x01],p=>p[0]==0x02,default);
        await startupAck.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await responder.LastAcknowledgement;
        await TunerEntrySequence.EnterAsync(session,default,responder:responder).WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(new[]{"0101","4A01","4101","4A01","4102"},writes);
    }

    [Fact]
    public async Task ReplyQueuesBehindAnUnrelatedQueryWithoutStealingItsResponse()
    {
        List<string> writes=[];
        using var session=new CommandSession((payload,_)=>{writes.Add(Convert.ToHexString(payload));return Task.CompletedTask;});
        using var responder=new ToolRequestResponder(session);
        var query=session.QueryAsync([0x11],p=>p[0]==0x12,default);
        session.Receive(FrameCodec.Encode([0x49,0x00]));
        Assert.Equal(new[]{"11"},writes);
        session.Receive(FrameCodec.Encode([0x12,0x01]));
        Assert.Equal(new byte[]{0x12,0x01},await query);
        await responder.LastAcknowledgement.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(new[]{"11","4A01"},writes);
    }

    [Fact]
    public async Task OnlyObservedStoppedOrPlayingRequestsAreAcknowledgedAndDisposalUnsubscribes()
    {
        List<string> writes=[];
        using var session=new CommandSession((payload,_)=>{writes.Add(Convert.ToHexString(payload));return Task.CompletedTask;});
        using var responder=new ToolRequestResponder(session);
        foreach(var payload in new byte[][]{[0x49],[0x49,0x02],[0x49,0x00,0x00],[0x45,0x06,0x08]})session.Receive(FrameCodec.Encode(payload));
        Assert.Empty(writes);
        session.Receive(FrameCodec.Encode([0x49,0x00]));await responder.LastAcknowledgement;
        session.Receive(FrameCodec.Encode([0x49,0x01]));await responder.LastAcknowledgement;
        responder.Dispose();session.Receive(FrameCodec.Encode([0x49,0x00]));
        Assert.Equal(new[]{"4A01","4A01"},writes);
    }

    [Fact]
    public async Task FailedAcknowledgementMakesTheSessionUnavailableWithoutReplay()
    {
        var writes=0;
        using var session=new CommandSession((_,_)=>{writes++;return Task.FromException(new IOException("Lost"));});
        using var responder=new ToolRequestResponder(session);
        session.Receive(FrameCodec.Encode([0x49,0x00]));
        await Assert.ThrowsAnyAsync<Exception>(()=>responder.LastAcknowledgement);
        Assert.False(session.IsAvailable);
        session.Receive(FrameCodec.Encode([0x49,0x00]));Assert.Equal(1,writes);
    }
}
