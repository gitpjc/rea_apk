using System.Buffers.Binary;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class ReorderedParameterTests
{
    private static PresetEffectBlock Profile(ObservedParameter parameter)
        =>ObservedParameterCommands.Definition(parameter).Position switch {
            1=>new(1,4,new ushort[]{1,2,25,50,0,0}),
            2=>new(2,1,new ushort[]{1,3,39,100,100}),
            3=>new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1}),
            4=>new(4,7,new ushort[]{1,1,17,25,500,0,0,0}),
            5=>new(5,8,new ushort[]{1,1,20,24,35,0,0,0,0,0}),
            _=>throw new InvalidOperationException()
        };

    public static IEnumerable<object[]> Parameters()=>Enum.GetValues<ObservedParameter>().Select(parameter=>new object[]{parameter});

    [Theory]
    [MemberData(nameof(Parameters))]
    public void EachKnownParameterFollowsItsEffectAndPreservesOtherWordsAtEveryPosition(ObservedParameter parameter)
    {
        var definition=ObservedParameterCommands.Definition(parameter);
        var original=Profile(parameter);
        var before=original.Values.ToArray();
        for(byte position=1;position<=20;position++) {
            var block=original with {Position=position};
            var request=ObservedParameterCommands.Create(block,parameter,definition.Minimum);
            Assert.Equal(position,request[1]);
            Assert.Equal(block.TypeCode,request[2]);
            Assert.Same(block,ObservedParameterCommands.FindSupportedBlock(new[]{new PresetEffectBlock(20,0,Array.Empty<ushort>()),block},parameter));
            for(var i=0;i<before.Length;i++)
                Assert.Equal(i==definition.ValueIndex?definition.Minimum:before[i],BinaryPrimitives.ReadUInt16LittleEndian(request.AsSpan(4+i*2,2)));
            Assert.Equal(before,block.Values);
        }
    }

    [Fact]
    public void ResolutionRefusesMissingAmbiguousDisabledAndUnknownModels()
    {
        var noise=Profile(ObservedParameter.NoiseThreshold) with {Position=9};
        var disabled=noise with {Position=10,Values=new ushort[]{0,2,25,50,0,0}};
        Assert.Null(ObservedParameterCommands.FindSupportedBlock(Array.Empty<PresetEffectBlock>(),ObservedParameter.NoiseThreshold));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock(new[]{noise,disabled},ObservedParameter.NoiseThreshold));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock(new[]{disabled},ObservedParameter.NoiseThreshold));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock(new[]{noise with {Values=new ushort[]{1,0,25,50,0,0}}},ObservedParameter.NoiseThreshold));
    }
}
