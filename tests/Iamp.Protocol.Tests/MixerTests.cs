using System.Globalization;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class MixerTests
{
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "fixtures", "f15-mixer-observations.json")));
    private static byte[] Baseline()
    { using var fixture = Fixture(); return Convert.FromHexString(fixture.RootElement.GetProperty("baselineStateHex").GetString()!); }
    private static MixerChannel Channel(JsonElement channel) => channel.ValueKind == JsonValueKind.Number
        ? (MixerChannel)channel.GetInt32() : Enum.Parse<MixerChannel>(channel.GetString()!, ignoreCase: true);

    public static IEnumerable<object[]> MobileFrames()
    {
        using var fixture = Fixture();
        var seen = new HashSet<string>();
        foreach (var channel in fixture.RootElement.GetProperty("channels").EnumerateArray())
            foreach (var sample in channel.GetProperty("samples").EnumerateArray())
                if (seen.Add(channel.GetProperty("channel").ToString() + sample.GetProperty("kind").GetString() +
                    sample.GetProperty("beforeStateHex").GetString() + sample.GetProperty("frameHex").GetString()))
                yield return [Channel(channel.GetProperty("channel")), sample.GetProperty("kind").GetString()!,
                    sample.GetProperty("target").GetByte(), sample.GetProperty("beforeStateHex").GetString()!,
                    sample.GetProperty("frameHex").GetString()!];
    }

    [Theory]
    [MemberData(nameof(MobileFrames))]
    public void FullStateCommandsMatchIndependentMobileFrames(MixerChannel channel, string kind, byte target,
        string beforeStateHex, string frameHex)
    {
        var before = Convert.FromHexString(beforeStateHex); var original = before.ToArray();
        var payload = kind switch {
            "level" => MixerCommands.CreateLevel(before, channel, target),
            "mute" => MixerCommands.CreateMute(before, channel, target == 1),
            _ => throw new InvalidOperationException("Unknown retained mixer command.")
        };
        Assert.Equal(frameHex, Convert.ToHexString(FrameCodec.Encode(payload)));
        Assert.Equal(original, before);
    }

    [Fact]
    public void EveryLevelAndMuteAtAllEightySlotsPreservesEveryUnrelatedByte()
    {
        var baseline = Baseline();
        foreach (var spec in MixerState.Specs)
            for (byte slot = 1; slot <= 80; slot++)
            {
                var source = baseline.ToArray(); source[1] = slot; source[36] = 0xA5;
                for (byte level = 0; level <= 72; level++)
                {
                    var expected = source.ToArray(); expected[0] = 0x13;
                    foreach (var offset in spec.LevelOffsets) expected[offset] = level;
                    Assert.Equal(expected, MixerCommands.CreateLevel(source, spec.Channel, level));
                }
                foreach (var muted in new[] { false, true })
                {
                    var expected = source.ToArray(); expected[0] = 0x13; expected[spec.MuteOffset] = (byte)(muted ? 1 : 0);
                    Assert.Equal(expected, MixerCommands.CreateMute(source, spec.Channel, muted));
                }
                Assert.Equal(slot, source[1]); Assert.Equal(0xA5, source[36]); Assert.Equal(0x12, source[0]);
            }
    }

    [Fact]
    public void ParsedStateUsesCapturedFieldsAndIndependentPhysicalMappings()
    {
        using var fixture = Fixture(); var source = Baseline(); var state = MixerState.Parse(source);
        Assert.Equal(source[1], state.WireIndex);
        Assert.Equal(Convert.ToHexString(source), state.RawStatePayloadHex);
        var channels = fixture.RootElement.GetProperty("channels").EnumerateArray().ToArray();
        Assert.Equal(Enum.GetValues<MixerChannel>().Order(), channels.Select(c => Channel(c.GetProperty("channel"))).Order());
        foreach (var captured in channels)
        {
            var channel = Channel(captured.GetProperty("channel")); var spec = MixerState.GetSpec(channel); var shown = state.Get(channel);
            Assert.Equal(captured.GetProperty("name").GetString(), spec.Name);
            Assert.Equal(captured.GetProperty("muteOffset").GetInt32(), spec.MuteOffset);
            Assert.Equal(captured.GetProperty("levelOffsets").EnumerateArray().Select(v => v.GetInt32()), spec.LevelOffsets);
            Assert.Equal(captured.GetProperty("minimum").GetByte(), spec.Minimum);
            Assert.Equal(captured.GetProperty("maximum").GetByte(), spec.Maximum);
            var scale = captured.GetProperty("scale").GetDecimal(); var offset = captured.GetProperty("offset").GetDecimal();
            var places = captured.GetProperty("decimalPlaces").GetInt32();
            Assert.Equal(scale, spec.Scale); Assert.Equal(offset, spec.Offset); Assert.Equal("dB", spec.Unit);
            foreach (var observation in captured.GetProperty("physicalObservations").EnumerateArray())
            {
                Assert.Equal(observation.GetProperty("value").GetDecimal(), spec.DisplayValue(observation.GetProperty("raw").GetByte()));
                Assert.Equal(64, observation.GetProperty("imageSha256").GetString()!.Length);
            }
            Assert.True(captured.GetProperty("mutedFaderInteractive").GetBoolean());
            Assert.Equal(source[spec.MuteOffset] == 1, shown.Muted);
            Assert.Equal(spec.LevelOffsets.Select(i => source[i]), shown.RawLevels);
            Assert.True(shown.HasMatchingLevels);
            for (byte raw = spec.Minimum; raw <= spec.Maximum; raw++)
            {
                var value = raw * scale + offset; var text = value.ToString($"F{places}", CultureInfo.InvariantCulture);
                Assert.Equal(value, spec.DisplayValue(raw)); Assert.Equal(text, spec.Format(raw));
                Assert.True(spec.TryParse(text, out var parsed)); Assert.Equal(raw, parsed);
            }
            foreach (var invalid in new[] { "", "bad", "NaN", "1e1", "-99999999999999999999999999999", "99999999999999999999999999999",
                (offset - scale).ToString(CultureInfo.InvariantCulture),
                (spec.Maximum * scale + offset + scale).ToString(CultureInfo.InvariantCulture),
                (offset + scale / 2m).ToString(CultureInfo.InvariantCulture) })
                Assert.False(spec.TryParse(invalid, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => spec.Format(73));
            Assert.Throws<ArgumentOutOfRangeException>(() => MixerCommands.CreateLevel(source, channel, 73));
            var samples = captured.GetProperty("samples").EnumerateArray().ToArray();
            Assert.Contains(samples, sample => sample.GetProperty("kind").GetString() == "level");
            foreach (var target in new byte[] { 0, 1 })
                Assert.Contains(samples, sample => sample.GetProperty("kind").GetString() == "mute" && sample.GetProperty("target").GetByte() == target);
        }
        source[30] = 1;
        Assert.Equal(Baseline(), state.ToArray());
        var exported = state.ToArray(); exported[30] = 2;
        Assert.Equal(Baseline(), state.ToArray());
    }

    [Fact]
    public void MalformedReadStatesAndUnknownChannelsAreRejectedBeforeCommands()
    {
        var source = Baseline();
        var invalid = new List<byte[]> { Array.Empty<byte>(), source[..^1], source.Concat(new byte[] { 0 }).ToArray() };
        foreach (var (index, value) in new (int, byte)[] { (0, 0x13), (0, 0), (1, 0), (1, 81) })
        { var copy = source.ToArray(); copy[index] = value; invalid.Add(copy); }
        foreach (var spec in MixerState.Specs)
        {
            var badMute = source.ToArray(); badMute[spec.MuteOffset] = 2; invalid.Add(badMute);
            foreach (var offset in spec.LevelOffsets)
            { var badLevel = source.ToArray(); badLevel[offset] = 73; invalid.Add(badLevel); }
        }
        foreach (var state in invalid)
        {
            Assert.Throws<NotSupportedException>(() => MixerState.Parse(state));
            foreach (var channel in Enum.GetValues<MixerChannel>())
            {
                Assert.Throws<NotSupportedException>(() => MixerCommands.CreateLevel(state, channel, 0));
                Assert.Throws<NotSupportedException>(() => MixerCommands.CreateMute(state, channel, false));
            }
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => MixerState.GetSpec((MixerChannel)255));
        Assert.Throws<ArgumentOutOfRangeException>(() => MixerState.Parse(source).Get((MixerChannel)255));
        Assert.Throws<ArgumentOutOfRangeException>(() => MixerCommands.CreateLevel(source, (MixerChannel)255, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MixerCommands.CreateMute(source, (MixerChannel)255, false));
    }

    [Fact]
    public void AsymmetricMasterCanBeReadAndPreservedButNeverEdited()
    {
        var source = Baseline(); source[37] = 20; source[38] = 21;
        Assert.False(MixerState.Parse(source).Get(MixerChannel.Master).HasMatchingLevels);
        Assert.Throws<NotSupportedException>(() => MixerCommands.CreateLevel(source, MixerChannel.Master, 0));
        Assert.Throws<NotSupportedException>(() => MixerCommands.CreateMute(source, MixerChannel.Master, true));
        foreach (var channel in new[] { MixerChannel.Bluetooth, MixerChannel.UsbRecording, MixerChannel.Instrument })
            foreach (var payload in new[] { MixerCommands.CreateLevel(source, channel, 0), MixerCommands.CreateMute(source, channel, true) })
            { Assert.Equal(20, payload[37]); Assert.Equal(21, payload[38]); }
    }
}
