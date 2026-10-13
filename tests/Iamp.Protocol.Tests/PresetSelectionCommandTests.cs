using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class PresetSelectionCommandTests
{
    [Fact]
    public void MatchesCapturedSelectionsAndPreservesAllOtherStateBytes()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures/f15-observed-selection.json")));
        var frames = fixture.RootElement.GetProperty("frames");
        var baseline = FrameCodec.Decode(Convert.FromHexString(frames[0].GetProperty("hex").GetString()!)).Payload;
        foreach (var index in new[] { 1, 3 })
        {
            var observedFrame = frames[index].GetProperty("hex").GetString()!;
            var target = FrameCodec.Decode(Convert.FromHexString(observedFrame)).Payload[1];
            var encoded = PresetSelectionCommand.Create(baseline, target);
            Assert.Equal(observedFrame.ToUpperInvariant(), Convert.ToHexString(FrameCodec.Encode(encoded)));
            Assert.Equal(baseline[2..], encoded[2..]);
        }
    }

    [Fact]
    public void RejectsUnknownStateAndPresetSlots()
    {
        Assert.Throws<NotSupportedException>(() => PresetSelectionCommand.Create(new byte[91], 5));
        var state = new byte[92]; state[0] = 0x12; state[1] = 4;
        Assert.Throws<ArgumentOutOfRangeException>(() => PresetSelectionCommand.Create(state, 6));
        state[1] = 6;
        Assert.Throws<NotSupportedException>(() => PresetSelectionCommand.Create(state, 4));
        Assert.Throws<NotSupportedException>(() => PresetSelectionCommand.RequireObservedPreset(4, new byte[160]));
    }
}
