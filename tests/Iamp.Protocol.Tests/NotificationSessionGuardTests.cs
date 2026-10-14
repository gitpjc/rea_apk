using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class NotificationSessionGuardTests
{
    private static byte[] CapturedStatus(byte value)
    {
        using var fixture=Fixture();
        return Convert.FromHexString(fixture.RootElement.GetProperty("statuses").EnumerateArray()
            .Single(item=>item.GetProperty("payloadHex").GetString()==$"9b{value:x2}").GetProperty("hex").GetString()!);
    }
    private static byte[] CapturedAck()
    {
        using var fixture=Fixture();
        return Convert.FromHexString(fixture.RootElement.GetProperty("ack").GetProperty("hex").GetString()!);
    }
    private static JsonDocument Fixture()=>JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures/f15-observed-transfer-status.json")));

    [Fact]
    public void FixtureContainsIndependentCapturedStatusAndAckBytes()
    {
        using var fixture=Fixture();
        Assert.Equal("f0176ec3722fdad743925dcf702330a1a9cd1e8262493db59cce08423001c906",fixture.RootElement.GetProperty("sourceSha256").GetString());
        Assert.Equal(new byte[]{0x9B,0x00},FrameCodec.Decode(CapturedStatus(0)).Payload);
        Assert.Equal(new byte[]{0x9B,0x01},FrameCodec.Decode(CapturedStatus(1)).Payload);
        Assert.Equal(new byte[]{0x14,0x01},FrameCodec.Decode(CapturedAck()).Payload);
    }

    [Fact]
    public void OrdinaryFramesRemainAvailableWithoutInvalidatingSession()
    {
        var guard=new NotificationSessionGuard();
        var frames=guard.ProcessNotification(CapturedAck());
        Assert.Equal(new byte[]{0x14,0x01},Assert.Single(frames).Payload);
        Assert.False(guard.IsInvalid);guard.ThrowIfInvalid();
    }

    [Fact]
    public void CapturedZeroThenOneAndLaterAckCannotReenableSession()
    {
        var guard=new NotificationSessionGuard();
        var error=Assert.Throws<TransferStateChangedException>(()=>guard.ProcessNotification(CapturedStatus(0)));
        Assert.Equal(NotificationSessionGuard.FaultMessage,error.Message);
        guard.ResetDecoder();
        Assert.Throws<TransferStateChangedException>(()=>guard.ProcessNotification(CapturedStatus(1)));
        Assert.Throws<TransferStateChangedException>(()=>guard.ProcessNotification(CapturedAck()));
        Assert.Throws<TransferStateChangedException>(()=>guard.ThrowIfInvalid());
        Assert.True(guard.IsInvalid);
    }

    [Theory]
    [InlineData(false,0)]
    [InlineData(true,0)]
    [InlineData(false,1)]
    [InlineData(true,1)]
    public void CoalescedResponseCannotBeDeliveredBeforeWholeNotificationIsInspected(bool statusFirst,int status)
    {
        var guard=new NotificationSessionGuard();
        var ack=CapturedAck();var transfer=CapturedStatus((byte)status);
        var notification=statusFirst?transfer.Concat(ack).ToArray():ack.Concat(transfer).ToArray();
        var delivered=new List<ProtocolFrame>();
        Assert.Throws<TransferStateChangedException>(()=>{
            // Same production ordering: decoder/guard inspects all frames before callers resolve responses.
            foreach(var frame in guard.ProcessNotification(notification))delivered.Add(frame);
        });
        Assert.Empty(delivered);Assert.True(guard.IsInvalid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FragmentedCapturedStatusInvalidatesOnlyAfterChecksumValidFrameCompletes(int status)
    {
        var frame=CapturedStatus((byte)status);
        for(var split=1;split<frame.Length;split++) {
            var guard=new NotificationSessionGuard();
            Assert.Empty(guard.ProcessNotification(frame.AsSpan(0,split)));
            Assert.False(guard.IsInvalid);
            Assert.Throws<TransferStateChangedException>(()=>guard.ProcessNotification(frame.AsSpan(split)));
            Assert.True(guard.IsInvalid);
        }
    }

    [Fact]
    public void CorruptedStatusIsRejectedBeforeItCanInvalidateSession()
    {
        var bad=CapturedStatus(0);bad[^1]^=1;
        var guard=new NotificationSessionGuard();
        var frames=guard.ProcessNotification(bad.Concat(CapturedAck()).ToArray());
        Assert.Equal(new byte[]{0x14,0x01},Assert.Single(frames).Payload);
        Assert.False(guard.IsInvalid);
        Assert.Throws<TransferStateChangedException>(()=>guard.ProcessNotification(CapturedStatus(1)));
    }

    [Fact]
    public void UnrecognizedStatusValueIsAlsoTerminal()
    {
        var guard=new NotificationSessionGuard();
        Assert.Throws<TransferStateChangedException>(()=>guard.ProcessNotification(FrameCodec.Encode(new byte[]{0x9B,0xFE})));
        Assert.True(guard.IsInvalid);
    }
}
