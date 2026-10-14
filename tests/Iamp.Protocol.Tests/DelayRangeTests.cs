using System.Buffers.Binary;
using Iamp.Protocol;
namespace Iamp.Protocol.Tests;
public class DelayRangeTests {
 private static PresetEffectBlock Baseline=>new(4,7,new ushort[]{1,1,17,25,500,0,0,0});
 private static ObservedParameter[] Parameters=>new[]{ObservedParameter.DelayLevel,ObservedParameter.DelayFeedback,ObservedParameter.DelayTime};
 [Theory]
 [InlineData(ObservedParameter.DelayLevel,0,17,"AA551400250407100100010000001900F401000000000000EEB6")]
 [InlineData(ObservedParameter.DelayLevel,100,17,"AA551400250407100100010064001900F4010000000000005082")]
 [InlineData(ObservedParameter.DelayLevel,17,17,"AA551400250407100100010011001900F401000000000000DA93")]
 [InlineData(ObservedParameter.DelayFeedback,0,0,"AA551400250407100100010000000000F4010000000000005F2E")]
 [InlineData(ObservedParameter.DelayFeedback,100,0,"AA551400250407100100010000006400F401000000000000B90C")]
 [InlineData(ObservedParameter.DelayFeedback,25,0,"AA551400250407100100010000001900F401000000000000EEB6")]
 [InlineData(ObservedParameter.DelayTime,40,17,"AA551400250407100100010011001900280000000000000045A0")]
 [InlineData(ObservedParameter.DelayTime,2500,17,"AA551400250407100100010011001900C4090000000000000FE2")]
 [InlineData(ObservedParameter.DelayTime,500,17,"AA551400250407100100010011001900F401000000000000DA93")]
 public void MatchesIndependentPhoneEndpointsAndRestoration(ObservedParameter parameter,int value,int sourceLevel,string frame) {
  var v=Baseline.Values.ToArray();v[2]=(ushort)sourceLevel;
  Assert.Equal(frame,Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(Baseline with {Values=v},parameter,(ushort)value))));
 }
 [Theory]
 [InlineData(ObservedParameter.DelayLevel,101)][InlineData(ObservedParameter.DelayLevel,65535)]
 [InlineData(ObservedParameter.DelayFeedback,101)][InlineData(ObservedParameter.DelayFeedback,65535)]
 [InlineData(ObservedParameter.DelayTime,39)][InlineData(ObservedParameter.DelayTime,2501)][InlineData(ObservedParameter.DelayTime,65535)]
 public void RejectsTargetsOutsideObservedRange(ObservedParameter parameter,int value)=>Assert.Throws<ArgumentOutOfRangeException>(()=>ObservedParameterCommands.Create(Baseline,parameter,(ushort)value));
 private static void Check(ushort[] values,ObservedParameter parameter,ushort target) {
  var original=values.ToArray();var d=ObservedParameterCommands.Definition(parameter);var payload=ObservedParameterCommands.Create(Baseline with {Values=values},parameter,target);
  for(var i=0;i<8;i++)Assert.Equal(i==d.ValueIndex?target:values[i],BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4+2*i,2)));
  Assert.Equal(payload,FrameCodec.Decode(FrameCodec.Encode(payload)).Payload);Assert.Equal(original,values);
 }
 [Fact] public void EveryIntegerTargetAndAllEightExtremeSourcesPreserveOtherWords() {
  foreach(var p in Parameters){var d=ObservedParameterCommands.Definition(p);for(var v=d.Minimum;v<=d.Maximum;v++)Check(Baseline.Values.ToArray(),p,v);}
  for(var mask=0;mask<8;mask++){var values=Baseline.Values.ToArray();for(var i=0;i<3;i++){var d=ObservedParameterCommands.Definition(Parameters[i]);values[i+2]=((mask>>i)&1)==0?d.Minimum:d.Maximum;}foreach(var p in Parameters){var d=ObservedParameterCommands.Definition(p);foreach(var v in new ushort[]{d.Minimum,(ushort)(d.Minimum+1),d.Maximum})Check(values,p,v);}}
 }
}
