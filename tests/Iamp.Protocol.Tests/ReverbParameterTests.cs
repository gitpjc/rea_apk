using System.Buffers.Binary;
using Iamp.Protocol;
namespace Iamp.Protocol.Tests;
public class ReverbParameterTests
{
    private static PresetEffectBlock Baseline=>new(5,8,new ushort[]{1,1,20,24,35,0,0,0,0,0});
    [Theory]
    [InlineData(ObservedParameter.ReverbPreDelay,18,"AA5518002505081401000100120018002300000000000000000000007630")]
    [InlineData(ObservedParameter.ReverbPreDelay,20,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbLevel,23,"AA5518002505081401000100140017002300000000000000000000002DE6")]
    [InlineData(ObservedParameter.ReverbLevel,24,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbDecay,34,"AA551800250508140100010014001800220000000000000000000000B5FE")]
    [InlineData(ObservedParameter.ReverbDecay,35,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbTone,0,"AA551800250508140100010014001800230000000000000000000000B68B")]
    [InlineData(ObservedParameter.ReverbTone,1,"AA551800250508140100010014001800230001000000000000000000D9CE")]
    public void MatchesIndependentPhoneFrames(ObservedParameter parameter,int value,string frame)
        =>Assert.Equal(frame,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.ReverbPreDelay,65535)][InlineData(ObservedParameter.ReverbPreDelay,201)]
    [InlineData(ObservedParameter.ReverbLevel,65535)][InlineData(ObservedParameter.ReverbLevel,101)]
    [InlineData(ObservedParameter.ReverbDecay,65535)][InlineData(ObservedParameter.ReverbDecay,101)]
    [InlineData(ObservedParameter.ReverbTone,-1)][InlineData(ObservedParameter.ReverbTone,101)]
    public void RejectsUnverifiedTargets(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,unchecked((ushort)value)));
    [Theory]
    [InlineData(ObservedParameter.ReverbPreDelay)][InlineData(ObservedParameter.ReverbLevel)]
    [InlineData(ObservedParameter.ReverbDecay)][InlineData(ObservedParameter.ReverbTone)]
    public void RejectsUnknownSourceFieldsLayoutsAndDisabledEffects(ObservedParameter parameter)
    {
        var target=ObservedParameterCommands.Definition(parameter).Minimum;
        foreach(var index in new[]{0,1,6,7,8,9}) {
            var values=Baseline.Values.ToArray();values[index]++;
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target));
        }
        foreach(var other in new[]{ObservedParameter.ReverbPreDelay,ObservedParameter.ReverbLevel,ObservedParameter.ReverbDecay,ObservedParameter.ReverbTone}) {
            var d=ObservedParameterCommands.Definition(other);
            foreach(var invalid in new[]{unchecked((ushort)(d.Minimum-1)),(ushort)(d.Maximum+1)}) {
                var values=Baseline.Values.ToArray();values[d.ValueIndex]=invalid;
                Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target));
            }
        }
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{0,1,20,24,35,0,0,0,0,0}},parameter,target));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=0},parameter,target));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=7},parameter,target));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{1,1}},parameter,target));
    }
    [Fact]
    public void All24BoundedCombinationsPreserveOtherWordsAndInput()
    {
        var parameters=new[]{ObservedParameter.ReverbPreDelay,ObservedParameter.ReverbLevel,ObservedParameter.ReverbDecay,ObservedParameter.ReverbTone};
        for(ushort pre=18;pre<=20;pre++)for(var mask=0;mask<8;mask++) {
            var values=new ushort[]{1,1,pre,(ushort)(24-(mask&1)),(ushort)(35-((mask>>1)&1)),(ushort)((mask>>2)&1),0,0,0,0};
            var original=values.ToArray();
            foreach(var parameter in parameters) {
                var d=ObservedParameterCommands.Definition(parameter);
                var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,d.Maximum);
                Assert.Equal(new byte[]{0x25,5,8,20},payload.Take(4));
                for(var i=0;i<10;i++)Assert.Equal(i==d.ValueIndex?d.Maximum:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
                Assert.Equal(original,values);
            }
        }
    }
}
