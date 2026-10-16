using System.Buffers.Binary;
using Iamp.Protocol;
namespace Iamp.Protocol.Tests;
public class DelayParameterTests
{
    private static PresetEffectBlock Baseline=>new(4,7,new ushort[]{1,1,17,25,500,0,0,0});
    [Theory]
    [InlineData(ObservedParameter.DelayLevel,16,"AA551400250407100100010010001900F401000000000000D9E6")]
    [InlineData(ObservedParameter.DelayLevel,17,"AA551400250407100100010011001900F401000000000000DA93")]
    [InlineData(ObservedParameter.DelayFeedback,24,"AA551400250407100100010011001800F401000000000000B5D6")]
    [InlineData(ObservedParameter.DelayFeedback,25,"AA551400250407100100010011001900F401000000000000DA93")]
    [InlineData(ObservedParameter.DelayTime,499,"AA551400250407100100010011001900F3010000000000001D8B")]
    [InlineData(ObservedParameter.DelayTime,500,"AA551400250407100100010011001900F401000000000000DA93")]
    public void MatchesIndependentPhoneFrames(ObservedParameter parameter,int value,string frame)
        =>Assert.Equal(frame,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline,parameter,(ushort)value))));
    [Theory]
    [InlineData(ObservedParameter.DelayLevel,65535)][InlineData(ObservedParameter.DelayLevel,101)]
    [InlineData(ObservedParameter.DelayFeedback,65535)][InlineData(ObservedParameter.DelayFeedback,101)]
    [InlineData(ObservedParameter.DelayTime,39)][InlineData(ObservedParameter.DelayTime,2501)]
    public void RejectsUnverifiedTargets(ObservedParameter parameter,int value)
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
    [Theory]
    [InlineData(ObservedParameter.DelayLevel)][InlineData(ObservedParameter.DelayFeedback)][InlineData(ObservedParameter.DelayTime)]
    public void RejectsUnknownSourceFieldsLayoutsAndDisabledEffects(ObservedParameter parameter)
    {
        var target=ObservedParameterCommands.Definition(parameter).Minimum;
        foreach(var index in new[]{0,1,5,6,7}) {
            var values=Baseline.Values.ToArray();values[index]=index==1?ushort.MaxValue:(ushort)(values[index]+1);
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target));
        }
        foreach(var index in new[]{2,3,4}) {
            var values=Baseline.Values.ToArray();values[index]=index==4?(ushort)2501:(ushort)101;
            Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target));
        }
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{0,1,17,25,500,0,0,0}},parameter,target));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Position=0},parameter,target));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {TypeCode=8},parameter,target));
        Assert.Throws<NotSupportedException>(()=>ObservedParameterCommands.Create(Baseline with {Values=new ushort[]{1,1}},parameter,target));
    }
    [Fact]
    public void AllEightBoundedCombinationsPreserveOtherWordsAndInput()
    {
        var parameters=new[]{ObservedParameter.DelayLevel,ObservedParameter.DelayFeedback,ObservedParameter.DelayTime};
        for(var mask=0;mask<8;mask++) {
            var values=Baseline.Values.ToArray();for(var i=0;i<3;i++)values[i+2]=(ushort)(values[i+2]-((mask>>i)&1));
            var original=values.ToArray();
            foreach(var parameter in parameters) {
                var definition=ObservedParameterCommands.Definition(parameter);
                var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,definition.Maximum);
                Assert.Equal(new byte[]{0x25,4,7,16},payload.Take(4));
                for(var i=0;i<8;i++)Assert.Equal(i==definition.ValueIndex?definition.Maximum:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
                Assert.Equal(original,values);
            }
        }
    }
}
