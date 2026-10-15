using System.Buffers.Binary;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class EqModelTests
{
    internal static ushort[] Defaults(int model)=>model==0?[1,0,32,32,32,32,32,50,0,0,0,0,0,0,0,0,0]:[1,1,32,100,8,32,500,8,32,1600,8,32,6000,8,0,0,50];
    public static IEnumerable<object[]> Captures()
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-eq-model-observations.json")));
        foreach(var model in doc.RootElement.GetProperty("models").EnumerateArray())foreach(var sample in model.GetProperty("cases").EnumerateArray())
            yield return new object[]{sample.GetProperty("kind").GetString()!,sample.GetProperty("index").GetInt32(),sample.GetProperty("target").GetUInt16(),
                sample.GetProperty("before").EnumerateArray().Select(v=>v.GetUInt16()).ToArray(),sample.GetProperty("frameHex").GetString()!};
    }
    [Theory][MemberData(nameof(Captures))]
    public void EncoderMatchesIndependentMobileEndpointsAndPower(string kind,int index,ushort target,ushort[] values,string frameHex)
    {
        var block=new PresetEffectBlock(1,9,values);
        var payload=kind=="power"?EffectBypassCommand.Create(block,target==1):ObservedParameterCommands.Create(block,EqParameters.Find(block,index)!.Parameter,target);
        Assert.Equal(frameHex,Convert.ToHexString(FrameCodec.Encode(payload)));
    }
    public static IEnumerable<object[]> Parameters()=>EqParameters.All.Select(s=>new object[]{s.Parameter});
    [Theory][MemberData(nameof(Parameters))]
    public void EveryChoiceAtEveryPositionRoundTripsPhysicalValuesAndPreservesOtherWords(ObservedParameter parameter)
    {
        var spec=EqParameters.Get(parameter)!;var values=Defaults(spec.Profile-12);
        foreach(var wire in spec.WireValues) {
            Assert.True(spec.TryParse(spec.Format(wire),out var parsed));Assert.Equal(wire,parsed);
            for(byte position=1;position<=20;position++) {
                var payload=ObservedParameterCommands.Create(new(position,9,values),parameter,wire);
                Assert.Equal(position,payload[1]);
                for(var i=0;i<17;i++)Assert.Equal(i==spec.ValueIndex?wire:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+i*2,2)));
            }
        }
        var block=new PresetEffectBlock(1,9,values);
        foreach(var invalid in new[]{block with {Position=0},block with {TypeCode=8},block with {Values=values.Take(16).ToArray()},block with {Values=new ushort[17]}})
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(invalid,parameter,spec.WireValues[0]));
        var disabled=values.ToArray();disabled[0]=0;
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {Values=disabled},parameter,spec.WireValues[0]));
        var bad=values.ToArray();bad[spec.ValueIndex]=ushort.MaxValue;
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {Values=bad},parameter,spec.WireValues[0]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(block,parameter,ushort.MaxValue));
        Assert.Same(block,ObservedParameterCommands.FindSupportedBlock(new[]{block},parameter));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock(new[]{block,block with {Position=2}},parameter));
    }
    [Fact]
    public void PhysicalMappingsAndInvalidStepsAreExplicit()
    {
        var gain=EqParameters.Get(ObservedParameter.Eq100Hz)!;Assert.Equal(-16m,gain.DisplayValue(0));Assert.Equal(.5m,gain.DisplayValue(33));
        Assert.False(gain.TryParse("0.25",out _));Assert.False(gain.TryParse("16.5",out _));
        var q=EqParameters.Get(ObservedParameter.CustomEqBand1Q)!;Assert.Equal(.2m,q.DisplayValue(0));Assert.Equal(10m,q.DisplayValue(98));Assert.False(q.TryParse("1.05",out _));
        var frequency=EqParameters.Get(ObservedParameter.CustomEqBand1Freq)!;
        Assert.True(frequency.TryParse("99",out _));Assert.True(frequency.TryParse("100",out _));Assert.True(frequency.TryParse("110",out _));Assert.False(frequency.TryParse("101",out _));
        var cut=EqParameters.Get(ObservedParameter.CustomEqHighCut)!;Assert.True(cut.TryParse("off",out var wire));Assert.Equal(0,wire);Assert.True(cut.IsOff(wire));
        Assert.False(cut.TryParse("1000",out _));Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(new(1,9,Defaults(1)),cut.Parameter,2001));
        var reserved=Defaults(0);reserved[8]=1;Assert.False(EqParameters.IsSupported(new(1,9,reserved)));
    }
}
