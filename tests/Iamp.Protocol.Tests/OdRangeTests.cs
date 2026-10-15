using System.Buffers.Binary;
using Iamp.Protocol;
namespace Iamp.Protocol.Tests;
public class OdRangeTests
{
    private static PresetEffectBlock Baseline=>new(2,1,new ushort[]{1,3,39,100,100});
    private static ObservedParameter[] Parameters=>new[]{ObservedParameter.OdGain,ObservedParameter.OdTone,ObservedParameter.OdVolume};
    [Theory]
    [InlineData(ObservedParameter.OdGain,0,"AA550E002502010A010003000000640064004DB9")]
    [InlineData(ObservedParameter.OdGain,100,"AA550E002502010A010003006400640064001400")]
    [InlineData(ObservedParameter.OdGain,39,"AA550E002502010A01000300270064006400B0F0")]
    [InlineData(ObservedParameter.OdTone,0,"AA550E002502010A0100030027000000640023D3")]
    [InlineData(ObservedParameter.OdTone,100,"AA550E002502010A01000300270064006400B0F0")]
    [InlineData(ObservedParameter.OdVolume,0,"AA550E002502010A01000300270064000000771E")]
    [InlineData(ObservedParameter.OdVolume,100,"AA550E002502010A01000300270064006400B0F0")]
    public void MatchesIndependentPhoneEndpointsAndRestoration(ObservedParameter parameter,int value,string expected)
        =>Assert.Equal(expected,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.OdGain,101)][InlineData(ObservedParameter.OdGain,65535)]
    [InlineData(ObservedParameter.OdTone,101)][InlineData(ObservedParameter.OdTone,65535)]
    [InlineData(ObservedParameter.OdVolume,101)][InlineData(ObservedParameter.OdVolume,65535)]
    public void RejectsTargetsBeyondPhoneRange(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
    [Theory]
    [InlineData(ObservedParameter.OdGain)][InlineData(ObservedParameter.OdTone)][InlineData(ObservedParameter.OdVolume)]
    public void RejectsUnknownProfilesAndSourceValues(ObservedParameter parameter)
    {
        foreach(var values in new[]{new ushort[]{0,3,39,100,100},new ushort[]{2,3,39,100,100},new ushort[]{1,4,39,100,100},new ushort[]{1,3,101,100,100},new ushort[]{1,3,39,101,100},new ushort[]{1,3,39,100,101},new ushort[]{1,3}})
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=0},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=2},parameter,0));
    }
    private static void Check(ushort[] values,ObservedParameter parameter,ushort target)
    {
        var original=values.ToArray();var d=ObservedParameterCommands.Definition(parameter);
        var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target);
        for(var i=0;i<5;i++)Assert.Equal(i==d.ValueIndex?target:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
        Assert.Equal(payload,FrameCodec.Decode(FrameCodec.Encode(payload)).Payload);Assert.Equal(original,values);
    }
    [Fact]
    public void EveryTargetAndRepresentativeCombinedSourcesPreserveUnrelatedWords()
    {
        for(ushort value=0;value<=100;value++)foreach(var parameter in Parameters)Check(Baseline.Values.ToArray(),parameter,value);
        var points=new ushort[]{0,1,39,50,99,100};
        foreach(var gain in points)foreach(var tone in points)foreach(var volume in points)foreach(var parameter in Parameters)
            foreach(var target in new ushort[]{0,50,100})Check(new ushort[]{1,3,gain,tone,volume},parameter,target);
    }
}
