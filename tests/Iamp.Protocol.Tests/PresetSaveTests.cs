using System.Text;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class PresetSaveTests
{
    private static JsonDocument Fixture()=>JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-preset-save-observations.json")));
    [Fact]
    public void PowerCycleReadbackRetainsBroaderEditsDisabledEffectAndDuplicateCategory()
    {
        using var doc=Fixture();var root=doc.RootElement;var power=root.GetProperty("powerCycle");
        Assert.True(power.GetProperty("verified").GetBoolean());Assert.True(power.GetProperty("allOriginalSlotsRestored").GetBoolean());
        Assert.Equal(power.GetProperty("expectedDetailHex").GetString(),power.GetProperty("readbackDetailHex").GetString());
        var state=Convert.FromHexString(power.GetProperty("readbackStateHex").GetString()!);var detail=Convert.FromHexString(power.GetProperty("readbackDetailHex").GetString()!);
        var decoded=CurrentPresetReadGuard.Validate(state,detail,state);Assert.Equal(80,decoded.WireIndex);Assert.Equal("Windows Save 80",decoded.Name);
        Assert.Equal(27,decoded.Blocks[0].Values[2]);Assert.Equal(99,decoded.Blocks[1].Values[3]);Assert.Equal(0,decoded.Blocks[1].Values[0]);
        Assert.Equal(2,decoded.Blocks.Count(b=>b.TypeCode==4));Assert.Equal(0,decoded.Blocks[5].Values[1]);
        Assert.Equal(PresetSaveCommand.ExpectedState(Convert.FromHexString(root.GetProperty("baselineStateHex").GetString()!),80),state);
    }
    [Fact]
    public void NameAndDestinationMatchIndependentMobileFramesAndCompleteReadbacks()
    {
        using var doc=Fixture();var root=doc.RootElement;var state=Convert.FromHexString(root.GetProperty("baselineStateHex").GetString()!);var detail=Convert.FromHexString(root.GetProperty("baselineDetailHex").GetString()!);
        foreach(var sample in root.GetProperty("samples").EnumerateArray()) {
            var slot=sample.GetProperty("destination").GetByte();var name=sample.GetProperty("name").GetString()!;
            Assert.Equal(sample.GetProperty("frameHex").GetString(),Convert.ToHexString(FrameCodec.Encode(PresetSaveCommand.Create("V1.1.9",state,detail,slot,name))));
            Assert.Equal(sample.GetProperty("expectedDetailHex").GetString(),Convert.ToHexString(PresetSaveCommand.ExpectedDetail(detail,slot,name)));
            var recall=PresetSaveCommand.ExpectedState(state,slot);recall[0]=0x13;
            Assert.Equal(sample.GetProperty("recallFrameHex").GetString(),Convert.ToHexString(FrameCodec.Encode(recall)));
        }
    }

    [Fact]
    public void AllEightyDestinationsPreserveEffectsGlobalFieldsAndOpaqueBytes()
    {
        using var doc=Fixture();var root=doc.RootElement;var state=Convert.FromHexString(root.GetProperty("baselineStateHex").GetString()!);var detail=Convert.FromHexString(root.GetProperty("baselineDetailHex").GetString()!);
        var sourceState=state.ToArray();var sourceDetail=detail.ToArray();
        // Enabled flags, reordering and unrelated words are copied rather than reconstructed.
        detail=PresetDataParser.WithParameterValue(detail,2,0,0);
        detail=EffectChainCommands.ExpectedMove(detail,5,1);detail[^1]=0xA5;
        var editedSource=detail.ToArray();
        for(byte slot=1;slot<=80;slot++) {
            var request=PresetSaveCommand.Create("V1.1.9",state,detail,slot,"A renamed preset");
            Assert.Equal(22,request.Length);Assert.Equal(0x2F,request[0]);Assert.Equal(slot,request[1]);
            var expected=PresetSaveCommand.ExpectedDetail(detail,slot,"A renamed preset");
            Assert.Equal(detail.AsSpan(22).ToArray(),expected.AsSpan(22).ToArray());Assert.Equal(0x22,expected[0]);Assert.Equal(slot,expected[1]);
            var expectedState=PresetSaveCommand.ExpectedState(state,slot);
            Assert.Equal(state.AsSpan(2).ToArray(),expectedState.AsSpan(2).ToArray());Assert.Equal(0x12,expectedState[0]);Assert.Equal(slot,expectedState[1]);
        }
        Assert.Equal(sourceState,state);Assert.Equal(editedSource,detail);Assert.Equal(root.GetProperty("baselineDetailHex").GetString(),Convert.ToHexString(sourceDetail));
    }

    [Theory]
    [InlineData("")][InlineData(" ")][InlineData("123456789012345678901")][InlineData("bad\0name")][InlineData("line\nname")][InlineData("café")][InlineData("音色")][InlineData("🎸")]
    public void InvalidOrUnverifiedNamesNeverTruncateOrSubstitute(string name)=>Assert.Throws<ArgumentException>(()=>PresetSaveCommand.EncodeName(name));

    [Fact]
    public void NamesUseExactlyTwentyBytesAndPreserveSpacesAndPunctuation()
    {
        foreach(var name in new[]{"A","12345678901234567890"," My tone-1.0_ ","Guitar & Bass!"}) {
            var encoded=PresetSaveCommand.EncodeName(name);Assert.Equal(20,encoded.Length);
            Assert.Equal(Encoding.ASCII.GetBytes(name),encoded.AsSpan(0,name.Length).ToArray());Assert.All(encoded.Skip(name.Length),b=>Assert.Equal(0,b));
        }
        Assert.Throws<ArgumentNullException>(()=>PresetSaveCommand.EncodeName(null!));
    }

    [Fact]
    public void UnsupportedSourcesAndDestinationsAreRejected()
    {
        using var doc=Fixture();var root=doc.RootElement;var state=Convert.FromHexString(root.GetProperty("baselineStateHex").GetString()!);var detail=Convert.FromHexString(root.GetProperty("baselineDetailHex").GetString()!);
        foreach(byte slot in new byte[]{0,81,255})Assert.Throws<ArgumentOutOfRangeException>(()=>PresetSaveCommand.Create("V1.1.9",state,detail,slot,"Test"));
        Assert.Throws<NotSupportedException>(()=>PresetSaveCommand.Create("V1.1.2",state,detail,4,"Test"));
        Assert.Throws<FormatException>(()=>PresetSaveCommand.Create("V1.1.9",state[..^1],detail,4,"Test"));
        var wrong=detail.ToArray();wrong[1]=5;Assert.Throws<CurrentPresetChangedException>(()=>PresetSaveCommand.Create("V1.1.9",state,wrong,4,"Test"));
        var unknown=detail.ToArray();unknown[23]=0xFE;Assert.Throws<NotSupportedException>(()=>PresetSaveCommand.Create("V1.1.9",state,unknown,4,"Test"));
    }
}
