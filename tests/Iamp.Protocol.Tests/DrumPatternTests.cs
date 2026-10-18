using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class DrumPatternTests
{
    [Fact]
    public void EveryMobilePatternLabelAndCommandMatchesCapturedEvidence()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "fixtures", "f15-drum-pattern-observations.json")));
        var root = fixture.RootElement;
        var baseline = Convert.FromHexString(root.GetProperty("baselineStateHex").GetString()!);
        var original = baseline.ToArray();
        var observed = root.GetProperty("patterns").EnumerateArray().ToArray();
        Assert.Equal(70, observed.Length);
        Assert.Equal(observed.Length, DrumPatternCatalog.All.Count);
        foreach (var row in observed)
        {
            var id = row.GetProperty("patternId").GetByte();
            var pattern = DrumPatternCatalog.Get(id);
            Assert.Equal(row.GetProperty("name").GetString(), pattern.Name);
            Assert.Equal($"{row.GetProperty("number").GetInt32():00} {pattern.Name}", pattern.DisplayName);
            var expected = Convert.FromHexString(row.GetProperty("frameHex").GetString()!);
            var payload = DrumCommands.CreatePattern(baseline, id);
            Assert.Equal(expected, FrameCodec.Encode(payload));
            Assert.Equal(payload, FrameCodec.Decode(expected).Payload);
            for (var offset = 0; offset < baseline.Length; offset++)
                if (offset != 0 && offset != 43) Assert.Equal(baseline[offset], payload[offset]);
        }
        Assert.Equal(original, baseline);
        Assert.Equal("10 POP010", DrumPatternCatalog.Get(9).DisplayName);
        Assert.Equal("70 METRONOME10", DrumPatternCatalog.Get(69).DisplayName);
    }

    [Fact]
    public void RejectsUnknownPatternsAndIncompleteState()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DrumPatternCatalog.Get(70));
        Assert.Throws<ArgumentOutOfRangeException>(() => DrumCommands.CreatePattern(new byte[92], 255));
        Assert.Throws<NotSupportedException>(() => DrumCommands.CreatePattern(new byte[91], 0));
        Assert.Throws<NotSupportedException>(() => DrumCommands.CreatePattern(new byte[92], 0));
    }
}
