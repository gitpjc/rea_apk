using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class ObservedPresetTests
{
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "fixtures/f15-observed-preset.json")));

    private static byte[] Payload()
    {
        using var fixture = Fixture();
        return FrameCodec.Decode(Convert.FromHexString(fixture.RootElement.GetProperty("detail").GetProperty("frameHex").GetString()!)).Payload;
    }

    [Fact]
    public void MatchesEveryParameterDecodedIndependentlyByThePhoneApp()
    {
        using var fixture = Fixture();
        var expected = fixture.RootElement.GetProperty("detail");
        var preset = PresetDataParser.Parse(Payload());
        Assert.Equal(expected.GetProperty("presetIndex").GetByte(), preset.WireIndex);
        Assert.Equal(expected.GetProperty("name").GetString(), preset.Name);
        Assert.Equal(20, preset.Blocks.Count);
        var blocks = expected.GetProperty("blocks").EnumerateArray().ToArray();
        for (var i = 0; i < blocks.Length; i++)
        {
            Assert.Equal(blocks[i].GetProperty("sequence").GetByte(), preset.Blocks[i].Position);
            Assert.Equal(blocks[i].GetProperty("type").GetByte(), preset.Blocks[i].TypeCode);
            Assert.Equal(blocks[i].GetProperty("parameters").EnumerateArray().Select(v => v.GetUInt16()), preset.Blocks[i].Values);
        }
        Assert.Equal(Convert.FromHexString(expected.GetProperty("trailingPayloadHex").GetString()!), preset.UnknownTrailer);
    }

    [Fact]
    public void RejectsInvalidRecordLengthsSequencesAndTruncation()
    {
        var odd = Payload(); odd[24] = 11;
        Assert.Throws<FormatException>(() => PresetDataParser.Parse(odd));
        var sequence = Payload(); sequence[22] = 2;
        Assert.Throws<FormatException>(() => PresetDataParser.Parse(sequence));
        var truncated = Payload()[..^1];
        Assert.Throws<FormatException>(() => PresetDataParser.Parse(truncated));
    }

    [Fact]
    public void ReplacesOneParameterWithoutChangingOtherRecordsOrTrailer()
    {
        var original = Payload();
        var replaced = PresetDataParser.WithParameterValue(original, 2, 3, 37);
        var before = PresetDataParser.Parse(original);
        var after = PresetDataParser.Parse(replaced);
        Assert.Equal((ushort)37, after.Blocks[1].Values[3]);
        for (var block = 0; block < before.Blocks.Count; block++)
            for (var parameter = 0; parameter < before.Blocks[block].Values.Count; parameter++)
                if (block != 1 || parameter != 3)
                    Assert.Equal(before.Blocks[block].Values[parameter], after.Blocks[block].Values[parameter]);
        Assert.Equal(before.UnknownTrailer, after.UnknownTrailer);
        Assert.Equal(before.Name, after.Name);
        Assert.Throws<ArgumentOutOfRangeException>(() => PresetDataParser.WithParameterValue(original, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PresetDataParser.WithParameterValue(original, 20, 0, 1));
    }

    [Fact]
    public void PreservesUnmappedTypeCodesAndTrailingFields()
    {
        var payload = Payload(); payload[23] = 240; payload[^1] = 93;
        var preset = PresetDataParser.Parse(payload);
        Assert.Equal((byte)240, preset.Blocks[0].TypeCode);
        Assert.Equal(new ushort[] { 1, 5, 0, 73, 99 }, preset.Blocks[0].Values);
        Assert.Equal(new byte[] { 0, 93 }, preset.UnknownTrailer);
    }
}
