using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class ObservedFrameTests
{
    private static IEnumerable<(byte[] Bytes, byte[] Payload)> Observations()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures/f15-observed-frames.json")));
        foreach (var group in new[] { "frames", "responses" })
            foreach (var item in document.RootElement.GetProperty(group).EnumerateArray())
                yield return (Convert.FromHexString(item.GetProperty("hex").GetString()!),
                    Convert.FromHexString(item.GetProperty("payloadHex").GetString()!));
    }

    [Fact]
    public void CodecMatchesIndependentlyCapturedBytesInBothDirections()
    {
        var count = 0;
        foreach (var (bytes, payload) in Observations())
        {
            Assert.Equal(payload, FrameCodec.Decode(bytes).Payload);
            Assert.Equal(bytes, FrameCodec.Encode(payload));
            count++;
        }
        Assert.True(count >= 18, "Fixture must retain observed outgoing and incoming frames.");
    }

    [Fact]
    public void EveryObservedFrameReassemblesAcrossEveryPossibleSplit()
    {
        foreach (var (bytes, payload) in Observations())
            for (var split = 1; split < bytes.Length; split++)
            {
                var decoder = new FrameStreamDecoder();
                Assert.Empty(decoder.Push(bytes.AsSpan(0, split)));
                Assert.Equal(payload, Assert.Single(decoder.Push(bytes.AsSpan(split))).Payload);
                Assert.Equal(0, decoder.BufferedBytes);
            }
    }

    [Fact]
    public void StreamRecoversAfterCorruptionAndHandlesCoalescedFrames()
    {
        var (good, payload) = Observations().First();
        var bad = (byte[])good.Clone();
        bad[^1] ^= 1;
        var decoder = new FrameStreamDecoder();
        var frames = decoder.Push(new byte[] { 0, 0xAA, 0xAA, 0x55, 0xFF, 0xFF }.Concat(bad).Concat(good).Concat(good).ToArray());
        Assert.Equal(2, frames.Count);
        Assert.All(frames, frame => Assert.Equal(payload, frame.Payload));
        Assert.True(decoder.RejectedCandidates >= 2);
        Assert.Equal(0, decoder.BufferedBytes);
    }

    [Fact]
    public void RejectsTruncationTrailingBytesAndChecksumCorruption()
    {
        var (bytes, _) = Observations().First();
        Assert.Throws<FormatException>(() => FrameCodec.Decode(bytes.AsSpan(0, bytes.Length - 1)));
        Assert.Throws<FormatException>(() => FrameCodec.Decode(bytes.Concat(new byte[] { 0 }).ToArray()));
        bytes[4] ^= 1;
        Assert.Throws<FormatException>(() => FrameCodec.Decode(bytes));
    }

    [Fact]
    public void UntrustedLengthsCannotGrowBufferBeyondBudgetAndResetDropsPendingData()
    {
        var decoder = new FrameStreamDecoder();
        for (var i = 0; i < 100000; i++) decoder.Push(new byte[] { 0xAA, 0x55, 0xFF, 0xFF });
        Assert.InRange(decoder.BufferedBytes, 0, FrameCodec.MaximumPayload + 6);
        decoder.Push(new byte[] { 0xAA, 0x55, 4, 0, 1 });
        decoder.Reset();
        Assert.Equal(0, decoder.BufferedBytes);
        Assert.Equal(0, decoder.RejectedCandidates);
    }
}
