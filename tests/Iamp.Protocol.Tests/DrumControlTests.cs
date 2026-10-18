using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class DrumControlTests
{
    [Fact]
    public void StateEditsMatchCapturedMobileFramesAndPreserveUnrelatedBytes()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "fixtures", "f15-drum-control-observations.json")));
        var root = fixture.RootElement;
        var baseline = Convert.FromHexString(root.GetProperty("baselineStateHex").GetString()!);
        var original = baseline.ToArray();
        Assert.Equal(100, DrumSettings.Parse(baseline).Tempo);
        Assert.Equal(60, DrumSettings.Parse(baseline).Volume);
        foreach (var row in root.GetProperty("samples").EnumerateArray())
        {
            var volume = row.GetProperty("kind").GetString() == "volume";
            var value = row.GetProperty("value").GetInt32();
            var payload = volume ? DrumCommands.CreateVolume(baseline, value) : DrumCommands.CreateTempo(baseline, value);
            Assert.Equal(Convert.FromHexString(row.GetProperty("frameHex").GetString()!), FrameCodec.Encode(payload));
            for (var i = 1; i < baseline.Length; i++)
                if (!(volume ? i == 40 : i is 41 or 42)) Assert.Equal(baseline[i], payload[i]);
            payload[0] = 0x12;
            var parsed = DrumSettings.Parse(payload);
            Assert.Equal(value, volume ? parsed.Volume : parsed.Tempo);
        }
        Assert.Equal(original, baseline);
    }

    [Fact]
    public void PlaybackAndTapMatchTheSeparateMobileCommands()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "fixtures", "f15-drum-control-observations.json")));
        foreach (var row in fixture.RootElement.GetProperty("playback").EnumerateArray())
        {
            var playing = row.GetProperty("playing").GetBoolean();
            var interval = row.GetProperty("intervalMilliseconds").GetUInt16();
            var payload = interval == 0 ? DrumCommands.CreatePlayback(playing) : DrumCommands.CreateTap(playing, interval);
            Assert.Equal(Convert.FromHexString(row.GetProperty("frameHex").GetString()!), FrameCodec.Encode(payload));
            var response = FrameCodec.Decode(Convert.FromHexString(row.GetProperty("replyHex").GetString()!)).Payload;
            Assert.Equal(2, response.Length);
            Assert.Equal(0x48, response[0]);
            // Long idle intervals are sent by mobile but return 48 00 without changing tempo.
            Assert.Equal((byte)(interval is 0 or 38534 or 33266 ? 0 : 1), response[1]);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ToolsEntryReadsPlaybackBeforeReplyRegardlessOfPacketOrder(bool playing, bool statusFirst)
    {
        List<string> writes = [];
        CommandSession? session = null;
        using var created = session = new CommandSession((p, _) =>
        {
            writes.Add(Convert.ToHexString(p));
            if (p[0] == 0x41)
            {
                var ack = FrameCodec.Encode([0x42, 0x01]);
                var status = FrameCodec.Encode([0x49, (byte)(playing ? 1 : 0)]);
                session!.Receive(statusFirst ? status.Concat(ack).ToArray() : ack.Concat(status).ToArray());
            }
            return Task.CompletedTask;
        });
        Assert.Equal(playing, await DrumEntrySequence.EnterAsync(session, default).WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.Equal(new[] { "4101", "4A01" }, writes);
    }
}
