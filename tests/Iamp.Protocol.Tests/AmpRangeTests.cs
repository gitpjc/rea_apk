using System.Buffers.Binary;
using Iamp.Protocol;
namespace Iamp.Protocol.Tests;
public class AmpRangeTests
{
    private static PresetEffectBlock Baseline=>new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1});
    private static ObservedParameter[] Parameters=>new[]{ObservedParameter.AmpGain,ObservedParameter.AmpBass,ObservedParameter.AmpMid,ObservedParameter.AmpTreble,ObservedParameter.AmpPresence,ObservedParameter.AmpMaster};
    [Theory]
    [InlineData(ObservedParameter.AmpGain,0,"AA5516002503021201001F00000023002B0028002D004B0001006A51")]
    [InlineData(ObservedParameter.AmpGain,100,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData(ObservedParameter.AmpBass,0,"AA5516002503021201001F00640000002B0028002D004B0001004ABB")]
    [InlineData(ObservedParameter.AmpBass,100,"AA5516002503021201001F00640064002B0028002D004B000100F48F")]
    [InlineData(ObservedParameter.AmpBass,35,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData(ObservedParameter.AmpMid,0,"AA5516002503021201001F0064002300000028002D004B000100E30D")]
    [InlineData(ObservedParameter.AmpMid,100,"AA5516002503021201001F0064002300640028002D004B000100052F")]
    [InlineData(ObservedParameter.AmpMid,43,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData(ObservedParameter.AmpTreble,0,"AA5516002503021201001F00640023002B0000002D004B0001004436")]
    [InlineData(ObservedParameter.AmpTreble,100,"AA5516002503021201001F00640023002B0064002D004B000100C6E3")]
    [InlineData(ObservedParameter.AmpTreble,40,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData(ObservedParameter.AmpPresence,0,"AA5516002503021201001F00640023002B00280000004B0001005ACF")]
    [InlineData(ObservedParameter.AmpPresence,100,"AA5516002503021201001F00640023002B00280064004B0001000376")]
    [InlineData(ObservedParameter.AmpPresence,45,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData(ObservedParameter.AmpMaster,0,"AA5516002503021201001F00640023002B0028002D00000001005107")]
    [InlineData(ObservedParameter.AmpMaster,100,"AA5516002503021201001F00640023002B0028002D0064000100C224")]
    [InlineData(ObservedParameter.AmpMaster,75,"AA5516002503021201001F00640023002B0028002D004B0001002184")]
    public void MatchesIndependentPhoneEndpointsAndRestoration(ObservedParameter parameter,int value,string expected)
        =>Assert.Equal(expected,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.AmpGain,101)][InlineData(ObservedParameter.AmpGain,65535)]
    [InlineData(ObservedParameter.AmpBass,101)][InlineData(ObservedParameter.AmpBass,65535)]
    [InlineData(ObservedParameter.AmpMid,101)][InlineData(ObservedParameter.AmpMid,65535)]
    [InlineData(ObservedParameter.AmpTreble,101)][InlineData(ObservedParameter.AmpTreble,65535)]
    [InlineData(ObservedParameter.AmpPresence,101)][InlineData(ObservedParameter.AmpPresence,65535)]
    [InlineData(ObservedParameter.AmpMaster,101)][InlineData(ObservedParameter.AmpMaster,65535)]
    public void RejectsTargetsBeyondPhoneRange(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
    [Theory]
    [InlineData(ObservedParameter.AmpGain)]
    [InlineData(ObservedParameter.AmpBass)]
    [InlineData(ObservedParameter.AmpMid)]
    [InlineData(ObservedParameter.AmpTreble)]
    [InlineData(ObservedParameter.AmpPresence)]
    [InlineData(ObservedParameter.AmpMaster)]
    public void RejectsUnknownProfilesFlagsAndSourceValues(ObservedParameter parameter)
    {
        foreach(var index in Enumerable.Range(0,9)) {
            var values=Baseline.Values.ToArray();values[index]=index is >=2 and <=7?(ushort)101:(ushort)(values[index]+1);
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,0));
        }
        var disabled=Baseline.Values.ToArray();disabled[0]=0;
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=disabled},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=21},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=1},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{1,31}},parameter,0));
    }
    private static void Check(ushort[] values,ObservedParameter parameter,ushort target)
    {
        var original=values.ToArray();var d=ObservedParameterCommands.Definition(parameter);
        var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target);
        for(var i=0;i<9;i++)Assert.Equal(i==d.ValueIndex?target:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
        Assert.Equal(payload,FrameCodec.Decode(FrameCodec.Encode(payload)).Payload);Assert.Equal(original,values);
    }
    [Fact]
    public void EveryTargetAndAll64ExtremeSourceCombinationsPreserveOtherWords()
    {
        for(ushort value=0;value<=100;value++)foreach(var parameter in Parameters)Check(Baseline.Values.ToArray(),parameter,value);
        for(var mask=0;mask<64;mask++) {
            var values=Baseline.Values.ToArray();for(var i=0;i<6;i++)values[i+2]=(ushort)(((mask>>i)&1)*100);
            foreach(var parameter in Parameters)foreach(var target in new ushort[]{0,1,50,99,100})Check(values,parameter,target);
        }
    }
}
