using System.Buffers.Binary;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class AmpEqTests
{
    private static PresetEffectBlock Baseline=>new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1});
    [Theory]
    [InlineData(ObservedParameter.AmpTreble,39,"AA5516002503021201001F00640023002B0027002D004B000100F846")]
    [InlineData(ObservedParameter.AmpTreble,40,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData(ObservedParameter.AmpPresence,44,"AA5516002503021201001F00640023002B0028002C004B0001006424")]
    [InlineData(ObservedParameter.AmpPresence,45,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    public void MatchesIndependentAdbPhoneFrames(ObservedParameter parameter,int value,string frame)
        =>Assert.Equal(frame,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.AmpTreble,65535)][InlineData(ObservedParameter.AmpTreble,101)]
    [InlineData(ObservedParameter.AmpPresence,65535)][InlineData(ObservedParameter.AmpPresence,101)]
    public void RejectsUnverifiedTargets(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
    [Theory]
    [InlineData(ObservedParameter.AmpTreble)][InlineData(ObservedParameter.AmpPresence)]
    public void RejectsOtherLayoutsAndFixedWordChanges(ObservedParameter parameter)
    {
        foreach(var index in new[]{0,1,2,7,8}) {
            var values=Baseline.Values.ToArray();values[index]=index is >=2 and <=7?(ushort)101:(ushort)(values[index]+1);
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,ObservedParameterCommands.Definition(parameter).Minimum));
        }
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=21},parameter,40));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=1},parameter,40));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{1,31}},parameter,40));
    }
    [Fact]
    public void EveryBoundedFourControlCombinationPreservesOtherWords()
    {
        var parameters=new[]{ObservedParameter.AmpBass,ObservedParameter.AmpMid,ObservedParameter.AmpTreble,ObservedParameter.AmpPresence};
        for(var mask=0;mask<16;mask++) {
            var values=Baseline.Values.ToArray();
            for(var i=0;i<4;i++)values[i+3]=(ushort)(Baseline.Values[i+3]-((mask>>i)&1));
            foreach(var parameter in parameters) {
                var definition=ObservedParameterCommands.Definition(parameter);
                var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,definition.Maximum);
                for(var i=0;i<9;i++)Assert.Equal(i==definition.ValueIndex?definition.Maximum:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
            }
        }
    }
}
