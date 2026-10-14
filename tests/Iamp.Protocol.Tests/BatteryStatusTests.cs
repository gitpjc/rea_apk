using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class BatteryStatusTests
{
    [Theory]
    [InlineData("924D01",77,1)][InlineData("924E01",78,1)][InlineData("924F01",79,1)][InlineData("925001",80,1)]
    public void DecodesRetainedPhoneBatteryResponses(string payload,int percentage,int power)
    {
        var reading=BatteryStatus.Parse(Convert.FromHexString(payload));
        Assert.Equal(percentage,reading.Percentage);Assert.Equal(power,reading.RawPowerState);
    }
    [Theory]
    [InlineData("")][InlineData("9264")][InlineData("92640200")][InlineData("916402")][InlineData("926502")]
    public void RejectsMalformedOrOutOfRangeReplies(string payload)
        =>Assert.Throws<FormatException>(()=>BatteryStatus.Parse(Convert.FromHexString(payload)));
    [Fact]
    public void KeepsUnknownPowerStateOpaqueAndAcceptsPercentageEndpoints()
    {
        Assert.Equal(new BatteryStatus(0,255),BatteryStatus.Parse(new byte[]{0x92,0,255}));
        Assert.Equal(new BatteryStatus(100,2),BatteryStatus.Parse(new byte[]{0x92,100,2}));
        Assert.Equal("AA550100915B57",Convert.ToHexString(FrameCodec.Encode(new byte[]{0x91})));
    }
}
