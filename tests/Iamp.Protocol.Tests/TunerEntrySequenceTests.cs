using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class TunerEntrySequenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CoalescedRequestAndAcknowledgementLeadToOrderedEntry(bool requestFirst)
    {
        List<string> writes=[];
        CommandSession? session=null;
        using var created=session=new CommandSession((payload,_)=> {
            writes.Add(Convert.ToHexString(payload));
            if(payload.AsSpan().SequenceEqual(new byte[]{0x41,0x01})) {
                var ack=FrameCodec.Encode([0x42,0x01]);var request=FrameCodec.Encode([0x49,0x00]);
                session!.Receive(requestFirst?request.Concat(ack).ToArray():ack.Concat(request).ToArray());
            }
            else if(payload[0]==0x41)session!.Receive(FrameCodec.Encode([0x42,0x01]));
            return Task.CompletedTask;
        });
        await TunerEntrySequence.EnterAsync(session,default).WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(new[]{"4101","4A01","4102"},writes);
    }

    [Fact]
    public async Task DoesNotSendReplyOrEnterUntilTheExactReadinessRequestArrives()
    {
        List<string> writes=[];var toolsWritten=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CommandSession? session=null;
        using var created=session=new CommandSession((payload,_)=> {
            writes.Add(Convert.ToHexString(payload));
            if(payload[0]==0x41)session!.Receive(FrameCodec.Encode([0x42,0x01]));
            if(payload[1]==1 && payload[0]==0x41)toolsWritten.TrySetResult();
            return Task.CompletedTask;
        });
        var entry=TunerEntrySequence.EnterAsync(session,default);
        await toolsWritten.Task.WaitAsync(TimeSpan.FromSeconds(3));
        session.Receive(FrameCodec.Encode([0x49,0x01]));
        Assert.False(entry.IsCompleted);Assert.Equal(new[]{"4101"},writes);
        var request=FrameCodec.Encode([0x49,0x00]);session.Receive(request.AsSpan(0,4));
        Assert.False(entry.IsCompleted);Assert.Single(writes);
        session.Receive(request.AsSpan(4));
        await entry.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(new[]{"4101","4A01","4102"},writes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingRequestStopsWithoutSendingAnUnrequestedReplyOrTunerCommand(bool cancel)
    {
        List<string> writes=[];CommandSession? session=null;
        using var created=session=new CommandSession((payload,_)=> {
            writes.Add(Convert.ToHexString(payload));session!.Receive(FrameCodec.Encode([0x42,0x01]));return Task.CompletedTask;
        });
        using var cancellation=new CancellationTokenSource();
        var entry=TunerEntrySequence.EnterAsync(session,cancellation.Token,TimeSpan.FromMilliseconds(cancel?5000:30));
        if(cancel){cancellation.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>entry);}
        else await Assert.ThrowsAsync<TimeoutException>(()=>entry);
        session.Receive(FrameCodec.Encode([0x49,0x00]));
        Assert.Equal(new[]{"4101"},writes);
    }

    [Fact]
    public async Task RejectedToolsAcknowledgementDoesNotContinue()
    {
        List<string> writes=[];CommandSession? session=null;
        using var created=session=new CommandSession((payload,_)=> {
            writes.Add(Convert.ToHexString(payload));session!.Receive(FrameCodec.Encode([0x42,0x00]));return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<IOException>(()=>TunerEntrySequence.EnterAsync(session,default));
        session.Receive(FrameCodec.Encode([0x49,0x00]));Assert.Equal(new[]{"4101"},writes);
    }
}
