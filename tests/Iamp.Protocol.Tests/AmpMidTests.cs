using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class AmpMidTests
{
    private static PresetEffectBlock Baseline=>new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1});
    [Theory]
    [InlineData(42,"AA5516002503021201001F00640023002A0028002D004B0001004EC1")]
    [InlineData(43,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    public void MatchesCapturedPhoneFrames(int value,string frame)
        =>Assert.Equal(frame,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,ObservedParameter.AmpMid,(ushort)value))));
    [Fact]
    public void ChangesOnlyMidAndPreservesInput()
    {
        Assert.Equal("2503021201001F00640023002A0028002D004B000100",Convert.ToHexString(ObservedParameterCommands.Create(Baseline,ObservedParameter.AmpMid,42)));
        Assert.Equal((ushort)43,Baseline.Values[4]);
    }
    [Fact]
    public void PreservesTheOtherSupportedDirtyAmpValue()
    {
        var values=Baseline.Values.ToArray();values[3]=34;
        Assert.Equal("2503021201001F00640022002A0028002D004B000100",Convert.ToHexString(ObservedParameterCommands.Create(Baseline with {Values=values},ObservedParameter.AmpMid,42)));
        values[4]=42;
        Assert.Equal("2503021201001F00640023002A0028002D004B000100",Convert.ToHexString(ObservedParameterCommands.Create(Baseline with {Values=values},ObservedParameter.AmpBass,35)));
    }
    [Theory]
    [InlineData(65535)][InlineData(101)]
    public void RejectsUnverifiedMidTargets(int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,ObservedParameter.AmpMid,(ushort)value));
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)]
    public void RejectsChangedUnsupportedFields(int index)
    {
        var values=Baseline.Values.ToArray();values[index]=index is >=2 and <=7?(ushort)101:(ushort)(values[index]+1);
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},ObservedParameter.AmpMid,42));
    }
}
