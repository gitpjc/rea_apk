using System.Buffers.Binary;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class EffectBypassTests
{
    // Exact phone log frames, 14 October 2026; no names or device addresses.
    [Theory]
    [InlineData("AA5510002501040C00000200190032000000000020B7")]
    [InlineData("AA5510002501040C01000200190032000000000023C2")]
    [InlineData("AA550E002502010A00000300270064006400DFB5")]
    [InlineData("AA550E002502010A01000300270064006400B0F0")]
    [InlineData("AA5516002503021200001F00640023002B0028002D004B000100D1B5")]
    [InlineData("AA5516002503021201001F00640023002B0028002D004B0001002184")]
    [InlineData("AA551400250407100000010011001900F40100000000000072B7")]
    [InlineData("AA551400250407100100010011001900F401000000000000DA93")]
    [InlineData("AA5518002505081400000100140018002300000000000000000000008338")]
    [InlineData("AA551800250508140100010014001800230000000000000000000000B68B")]
    public void MatchesIndependentPhoneOnOffFrames(string hex)
    {
        var frame=Convert.FromHexString(hex);var count=frame[7]/2;
        var values=Enumerable.Range(0,count).Select(i=>BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(8+2*i,2))).ToArray();
        var desired=values[0]==1;values[0]=(ushort)(desired?0:1);
        var block=new PresetEffectBlock(frame[5],frame[6],values);
        Assert.Equal(hex,Convert.ToHexString(FrameCodec.Encode(EffectBypassCommand.Create(block,desired))));
        Assert.True(EffectBypassCommand.TryGetEnabled(block,out var enabled));Assert.Equal(!desired,enabled);
        Assert.Equal((ushort)(desired?0:1),values[0]); // Encoder leaves input untouched.
    }
    [Fact]
    public void RejectsUnknownStatesAndLayouts()
    {
        var valid=new PresetEffectBlock(1,4,new ushort[]{0,2,25,50,0,0});
        foreach(var block in new[]{valid with {Position=0},valid with {Position=21},valid with {TypeCode=9},
            valid with {Values=new ushort[]{2,2,25,50,0,0}},valid with {Values=new ushort[]{1,3,25,50,0,0}},valid with {Values=Array.Empty<ushort>()}}) {
            Assert.False(EffectBypassCommand.TryGetEnabled(block,out _));
            Assert.Throws<NotSupportedException>(()=>EffectBypassCommand.Create(block,true));
        }
    }
    [Fact]
    public void PreservesOpaqueAndParameterWords()
    {
        var block=new PresetEffectBlock(1,4,new ushort[]{0,2,ushort.MaxValue,0x1234,0xABCD,0xCAFE});
        var on=EffectBypassCommand.Create(block,true);var off=EffectBypassCommand.Create(block,false);
        Assert.Equal(on[..4],off[..4]);Assert.Equal(on[6..],off[6..]);Assert.Equal(1,on[4]);Assert.Equal(0,off[4]);
    }
    [Fact]
    public void AmpReadbackMirrorsOnlyTheObservedLinkedFlag()
    {
        var amp=new PresetEffectBlock(3,2,new ushort[]{1,31,100,35,43,40,45,75,1});
        var off=EffectBypassCommand.ExpectedValues(amp,false);
        Assert.Equal(new ushort[]{0,31,100,35,43,40,45,75,0},off);
        Assert.Equal(amp.Values,EffectBypassCommand.ExpectedValues(amp with {Values=off},true));
        Assert.False(EffectBypassCommand.TryGetEnabled(amp with {Values=new ushort[]{1,31,100,35,43,40,45,75,2}},out _));
        var noise=new PresetEffectBlock(1,4,new ushort[]{1,2,25,50,0,0});
        Assert.Equal(new ushort[]{0,2,25,50,0,0},EffectBypassCommand.ExpectedValues(noise,false));
    }
}
