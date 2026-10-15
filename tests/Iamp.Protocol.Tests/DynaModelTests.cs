using System.Buffers.Binary;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class DynaModelTests
{
    public static IEnumerable<object[]> Captures()
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-dyna-model-observations.json")));
        foreach(var model in doc.RootElement.GetProperty("models").EnumerateArray())
            foreach(var sample in model.GetProperty("cases").EnumerateArray())
                yield return new object[]{model.GetProperty("model").GetUInt16(),sample.GetProperty("kind").GetString()!,sample.GetProperty("index").GetInt32(),
                    sample.GetProperty("target").GetUInt16(),sample.GetProperty("before").EnumerateArray().Select(v=>v.GetUInt16()).ToArray(),sample.GetProperty("frameHex").GetString()!};
    }
    internal static ObservedParameter Parameter(ushort model,int index)=>(model,index) switch {
        (0,2)=>ObservedParameter.RedCompSensitivity,(0,3)=>ObservedParameter.RedCompLevel,
        (1,2)=>ObservedParameter.YellowCompAttack,(1,3)=>ObservedParameter.YellowCompRatio,(1,4)=>ObservedParameter.YellowCompThreshold,(1,5)=>ObservedParameter.YellowCompLevel,
        (3,2)=>ObservedParameter.IntelReducerThreshold,(3,3)=>ObservedParameter.IntelReducerDepth,(3,4)=>ObservedParameter.IntelReducerLevel,
        _=>throw new ArgumentOutOfRangeException(nameof(index))
    };
    [Theory]
    [MemberData(nameof(Captures))]
    public void EncoderMatchesIndependentMobileEndpointAndPowerFrames(ushort model,string kind,int index,ushort target,ushort[] values,string frameHex)
    {
        Assert.Equal(model,values[1]);var block=new PresetEffectBlock(1,4,values);
        var encoded=kind=="power"?EffectBypassCommand.Create(block,target==1):ObservedParameterCommands.Create(block,Parameter(model,index),target);
        Assert.Equal(frameHex,Convert.ToHexString(FrameCodec.Encode(encoded)));
        Assert.Equal(values,block.Values);
    }
    [Theory]
    [InlineData(0,2)][InlineData(0,3)]
    [InlineData(1,2)][InlineData(1,3)][InlineData(1,4)][InlineData(1,5)]
    [InlineData(3,2)][InlineData(3,3)][InlineData(3,4)]
    public void EveryIntegerAndPositionPreservesAllOtherWordsAndRejectsInvalidLayouts(int model,int index)
    {
        var parameter=Parameter((ushort)model,index);
        ushort[] values=model switch {0=>[1,0,20,70,0,0],1=>[1,1,30,15,45,75],3=>[1,3,10,15,50,0],_=>throw new InvalidOperationException()};
        for(byte position=1;position<=20;position++)for(ushort target=0;target<=100;target++) {
            var payload=ObservedParameterCommands.Create(new(position,4,values),parameter,target);
            Assert.Equal(position,payload[1]);
            for(var i=0;i<values.Length;i++)Assert.Equal(i==index?target:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+i*2,2)));
        }
        var block=new PresetEffectBlock(1,4,values);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(block,parameter,101));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {Position=0},parameter,50));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {TypeCode=5},parameter,50));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {Values=values.Take(5).ToArray()},parameter,50));
        var invalid=values.ToArray();invalid[0]=0;
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {Values=invalid},parameter,50));
        invalid=values.ToArray();invalid[index]=101;
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {Values=invalid},parameter,50));
        if(model!=1) {
            invalid=values.ToArray();invalid[^1]=1;
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(block with {Values=invalid},parameter,50));
        }
    }
    [Fact]
    public void ImplicitResolutionDistinguishesDynaModelsButRefusesIdenticalDuplicates()
    {
        var red=new PresetEffectBlock(6,4,new ushort[]{1,0,20,70,0,0});var yellow=new PresetEffectBlock(9,4,new ushort[]{1,1,30,15,45,75});
        Assert.Same(red,ObservedParameterCommands.FindSupportedBlock(new[]{yellow,red},ObservedParameter.RedCompSensitivity));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock(new[]{red,red with {Position=12}},ObservedParameter.RedCompSensitivity));
    }
}
