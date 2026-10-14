using System.Buffers.Binary;
using Iamp.Protocol;
namespace Iamp.Protocol.Tests;
public class ReverbRangeTests
{
    private static PresetEffectBlock Baseline=>new(5,8,new ushort[]{1,1,20,24,35,0,0,0,0,0});
    private static ObservedParameter[] Parameters=>new[]{ObservedParameter.ReverbPreDelay,ObservedParameter.ReverbLevel,ObservedParameter.ReverbDecay,ObservedParameter.ReverbTone};
    [Theory]
    [InlineData(ObservedParameter.ReverbPreDelay,0,"AA5518002505081401000100000018002300000000000000000000001553")]
    [InlineData(ObservedParameter.ReverbPreDelay,200,"AA5518002505081401000100C8001800230000000000000000000000808F")]
    [InlineData(ObservedParameter.ReverbPreDelay,20,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbLevel,0,"AA5518002505081401000100140000002300000000000000000000009B17")]
    [InlineData(ObservedParameter.ReverbLevel,100,"AA551800250508140100010014006400230000000000000000000000D0C2")]
    [InlineData(ObservedParameter.ReverbLevel,24,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbDecay,0,"AA551800250508140100010014001800000000000000000000000000DDB4")]
    [InlineData(ObservedParameter.ReverbDecay,100,"AA5518002505081401000100140018006400000000000000000000006380")]
    [InlineData(ObservedParameter.ReverbDecay,35,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbTone,0,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbTone,100,"AA55180025050814010001001400180023006400000000000000000050A9")]
    public void MatchesIndependentPhoneEndpointsAndRestoration(ObservedParameter parameter,int value,string expected)
        =>Assert.Equal(expected,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.ReverbPreDelay,201)][InlineData(ObservedParameter.ReverbPreDelay,65535)]
    [InlineData(ObservedParameter.ReverbLevel,101)][InlineData(ObservedParameter.ReverbLevel,65535)]
    [InlineData(ObservedParameter.ReverbDecay,101)][InlineData(ObservedParameter.ReverbDecay,65535)]
    [InlineData(ObservedParameter.ReverbTone,101)][InlineData(ObservedParameter.ReverbTone,65535)]
    public void RejectsTargetsBeyondPhoneRange(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
    [Theory]
    [InlineData(ObservedParameter.ReverbPreDelay)]
    [InlineData(ObservedParameter.ReverbLevel)]
    [InlineData(ObservedParameter.ReverbDecay)]
    [InlineData(ObservedParameter.ReverbTone)]
    public void RejectsUnknownProfilesAndSourceValues(ObservedParameter parameter)
    {
        foreach(var index in Enumerable.Range(0,10)) {
            var values=Baseline.Values.ToArray();values[index]=index==2?(ushort)201:index is >=3 and <=5?(ushort)101:(ushort)(values[index]+1);
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,0));
        }
        var disabled=Baseline.Values.ToArray();disabled[0]=0;Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=disabled},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=4},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=7},parameter,0));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{1,1}},parameter,0));
    }
    private static void Check(ushort[] values,ObservedParameter parameter,ushort target)
    {
        var original=values.ToArray();var d=ObservedParameterCommands.Definition(parameter);
        var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target);
        for(var i=0;i<10;i++)Assert.Equal(i==d.ValueIndex?target:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
        Assert.Equal(payload,FrameCodec.Decode(FrameCodec.Encode(payload)).Payload);Assert.Equal(original,values);
    }
    [Fact]
    public void EveryTargetAndAll16ExtremeSourcesPreserveOtherWords()
    {
        foreach(var parameter in Parameters)for(ushort value=0;value<=ObservedParameterCommands.Definition(parameter).Maximum;value++)Check(Baseline.Values.ToArray(),parameter,value);
        for(var mask=0;mask<16;mask++) {
            var values=Baseline.Values.ToArray();for(var i=0;i<4;i++)values[i+2]=((mask>>i)&1)==0?(ushort)0:ObservedParameterCommands.Definition(Parameters[i]).Maximum;
            foreach(var parameter in Parameters)foreach(var target in new ushort[]{0,1,50,100})Check(values,parameter,target);
            Check(values,ObservedParameter.ReverbPreDelay,200);
        }
    }
}
