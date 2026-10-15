using System.Buffers.Binary;
using Iamp.Protocol;
namespace Iamp.Protocol.Tests;
public class NoiseRangeTests
{
    private static PresetEffectBlock Baseline=>new(1,4,new ushort[]{1,2,25,50,0,0});
    [Theory]
    [InlineData(ObservedParameter.NoiseThreshold,0,"AA5510002501040C010002000000320000000000477F")]
    [InlineData(ObservedParameter.NoiseThreshold,100,"AA5510002501040C010002006400320000000000C5AA")]
    [InlineData(ObservedParameter.NoiseThreshold,25,"AA5510002501040C01000200190032000000000023C2")]
    [InlineData(ObservedParameter.NoiseLevel,0,"AA5510002501040C010002001900000000000000870E")]
    [InlineData(ObservedParameter.NoiseLevel,100,"AA5510002501040C010002001900640000000000DEB7")]
    [InlineData(ObservedParameter.NoiseLevel,50,"AA5510002501040C01000200190032000000000023C2")]
    public void MatchesIndependentPhoneEndpointsAndRestoration(ObservedParameter parameter,int value,string expected)
        =>Assert.Equal(expected,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.NoiseThreshold,101)][InlineData(ObservedParameter.NoiseThreshold,65535)]
    [InlineData(ObservedParameter.NoiseLevel,101)][InlineData(ObservedParameter.NoiseLevel,65535)]
    public void RejectsTargetsBeyondPhoneRange(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
    [Theory]
    [InlineData(ObservedParameter.NoiseThreshold)][InlineData(ObservedParameter.NoiseLevel)]
    public void RejectsOtherProfilesAndInvalidSourceValues(ObservedParameter parameter)
    {
        foreach(var values in new[]{new ushort[]{0,2,25,50,0,0},new ushort[]{2,2,25,50,0,0},new ushort[]{1,3,25,50,0,0},new ushort[]{1,2,101,50,0,0},new ushort[]{1,2,25,101,0,0},new ushort[]{1,2,25,50,1,0},new ushort[]{1,2,25,50,0,1},new ushort[]{1,2}})
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=21},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=1},parameter,0));
    }
    [Fact]
    public void EveryIntegerSourceCombinationAndTargetRoundTripsWithoutAlteringOtherWords()
    {
        var parameters=new[]{ObservedParameter.NoiseThreshold,ObservedParameter.NoiseLevel};
        for(ushort threshold=0;threshold<=100;threshold++)for(ushort level=0;level<=100;level++) {
            var values=new ushort[]{1,2,threshold,level,0,0};var original=values.ToArray();
            foreach(var parameter in parameters) {
                var d=ObservedParameterCommands.Definition(parameter);var target=(ushort)(100-values[d.ValueIndex]);
                var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target);
                for(var i=0;i<6;i++)Assert.Equal(i==d.ValueIndex?target:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
                var frame=FrameCodec.Encode(payload);Assert.Equal(payload,FrameCodec.Decode(frame).Payload);Assert.Equal(original,values);
            }
        }
    }
}
