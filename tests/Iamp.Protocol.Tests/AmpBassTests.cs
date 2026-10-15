using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class AmpBassTests
{
    private static PresetEffectBlock Baseline=>new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1});
    [Theory]
    [InlineData(34,"AA5516002503021201001F00640022002B0028002D004B00010022F1")]
    [InlineData(35,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    public void MatchesCapturedPhoneBassFrames(int value,string frame)
        =>Assert.Equal(frame,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,ObservedParameter.AmpBass,(ushort)value))));
    [Fact]
    public void ChangesOnlyTheBassWordAndPreservesInput()
    {
        var block=Baseline;
        Assert.Equal("2503021201001F00640022002B0028002D004B000100",Convert.ToHexString(ObservedParameterCommands.Create(block,ObservedParameter.AmpBass,34)));
        Assert.Equal((ushort)35,block.Values[3]);
        Assert.Equal("2503021201001F00640023002B0028002D004B000100",Convert.ToHexString(ObservedParameterCommands.Create(block,ObservedParameter.AmpBass,35)));
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)]
    public void RejectsUnverifiedOtherFields(int index)
    {
        var values=Baseline.Values.ToArray();values[index]=index is >=2 and <=7?(ushort)101:(ushort)(values[index]+1);
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},ObservedParameter.AmpBass,34));
    }
    [Theory]
    [InlineData(65535)][InlineData(101)]
    public void RejectsUnverifiedTargets(int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,ObservedParameter.AmpBass,(ushort)value));
    [Fact]
    public void RejectsWrongPositionTypeAndLength()
    {
        foreach(var block in new[]{Baseline with {Position=0},Baseline with {TypeCode=1},Baseline with {Values=new ushort[]{1,31,100,35}}})
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block,ObservedParameter.AmpBass,34));
    }
}
