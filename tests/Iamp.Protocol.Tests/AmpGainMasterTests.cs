using System.Buffers.Binary;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class AmpGainMasterTests
{
    private static PresetEffectBlock Baseline=>new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1});
    [Theory]
    [InlineData(ObservedParameter.AmpGain,99,"AA5516002503021201001F00630023002B0028002D004B0001005182")]
    [InlineData(ObservedParameter.AmpGain,100,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData(ObservedParameter.AmpMaster,74,"AA5516002503021201001F00640023002B0028002D004A0001005730")]
    [InlineData(ObservedParameter.AmpMaster,75,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    public void MatchesIndependentAdbPhoneFrames(ObservedParameter parameter,int value,string frame)
        =>Assert.Equal(frame,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.AmpGain,65535)][InlineData(ObservedParameter.AmpGain,101)]
    [InlineData(ObservedParameter.AmpMaster,65535)][InlineData(ObservedParameter.AmpMaster,101)]
    public void RejectsUnverifiedTargets(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
    [Theory]
    [InlineData(ObservedParameter.AmpGain)][InlineData(ObservedParameter.AmpMaster)]
    public void RejectsOtherLayoutsAndFixedWordChanges(ObservedParameter parameter)
    {
        foreach(var index in new[]{0,1,2,7,8}) {
            var values=Baseline.Values.ToArray();values[index]=index is >=2 and <=7?(ushort)101:(ushort)(values[index]+1);
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,ObservedParameterCommands.Definition(parameter).Minimum));
        }
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=0},parameter,40));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=1},parameter,40));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{1,31}},parameter,40));
    }
    [Fact]
    public void EveryBoundedSixControlCombinationPreservesOtherWords()
    {
        var parameters=new[]{ObservedParameter.AmpGain,ObservedParameter.AmpBass,ObservedParameter.AmpMid,ObservedParameter.AmpTreble,ObservedParameter.AmpPresence,ObservedParameter.AmpMaster};
        for(var mask=0;mask<64;mask++) {
            var values=Baseline.Values.ToArray();
            for(var i=0;i<6;i++)values[i+2]=(ushort)(Baseline.Values[i+2]-((mask>>i)&1));
            foreach(var parameter in parameters) {
                var definition=ObservedParameterCommands.Definition(parameter);
                var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,definition.Maximum);
                for(var i=0;i<9;i++)Assert.Equal(i==definition.ValueIndex?definition.Maximum:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
            }
        }
    }
}
