using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class SnapshotArchiveCodecTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 10, 14, 10, 20, 30, TimeSpan.FromHours(11));

    private static (byte[] State, byte[] Detail) Payloads()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "fixtures/f15-observed-preset.json")));
        byte[] Read(string key) => FrameCodec.Decode(Convert.FromHexString(
            fixture.RootElement.GetProperty(key).GetProperty("frameHex").GetString()!)).Payload;
        return (Read("state"), Read("detail"));
    }

    private static JsonObject Archive()
    {
        var (state, detail) = Payloads();
        return JsonNode.Parse(SnapshotArchiveCodec.Encode("V1.1.2", Timestamp, state, detail))!.AsObject();
    }

    [Fact]
    public void ReadsIndependentlyGeneratedSanitizedArchiveExample()
    {
        // Example timestamp is deliberately synthetic, not the original capture time.
        // Node generated this file independently from the retained factory-preset fixture.
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures/f15-snapshot-v1.example.json"));
        var archive = SnapshotArchiveCodec.Decode(json);
        var (state, detail) = Payloads();
        Assert.Equal(state, archive.StatePayload);
        Assert.Equal(detail, archive.DetailPayload);
        Assert.Equal("Cali MK3 Lead", archive.Preset.Name);
        Assert.Equal("V1.1.2", archive.Firmware);
        Assert.Equal(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), archive.CapturedAt);
    }

    [Fact]
    public void RoundTripPreservesUnknownModelsRawStateAndTrailer()
    {
        var (state, detail) = Payloads();
        state[80] ^= 0x7F;
        detail[23] = 240;
        detail[^2] = 0x5A; detail[^1] = 0xA5;
        var json = SnapshotArchiveCodec.Encode("V1.1.2", Timestamp, state, detail);
        var decoded = SnapshotArchiveCodec.Decode(json);
        Assert.Equal("V1.1.2", decoded.Firmware);
        Assert.Equal(Timestamp, decoded.CapturedAt);
        Assert.Equal(state, decoded.StatePayload);
        Assert.Equal(detail, decoded.DetailPayload);
        Assert.Equal((byte)240, decoded.Preset.Blocks[0].TypeCode);
        Assert.Equal(new byte[] { 0x5A, 0xA5 }, decoded.Preset.UnknownTrailer);
        Assert.Equal(8, JsonNode.Parse(json)!.AsObject().Count);
        Assert.Contains("\n", json);
    }

    [Theory]
    [InlineData("statePayloadHex")]
    [InlineData("detailPayloadHex")]
    [InlineData("stateSha256")]
    [InlineData("detailSha256")]
    public void RejectsPayloadOrDigestTampering(string field)
    {
        var json = Archive();
        var original = json[field]!.GetValue<string>();
        json[field] = (original[0] == 'A' ? "B" : "A") + original[1..];
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(json.ToJsonString()));
    }

    [Theory]
    [InlineData("format", "vendor-backup")]
    [InlineData("firmware", "")]
    [InlineData("firmware", "V1.1.9\n")]
    [InlineData("firmware", "V１.１.９")]
    [InlineData("capturedAt", "2026-99-99")]
    [InlineData("statePayloadHex", "GG")]
    [InlineData("detailPayloadHex", "ABC")]
    public void RejectsInvalidRequiredText(string field, string value)
    {
        var json = Archive(); json[field] = value;
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(json.ToJsonString()));
    }

    [Fact]
    public void RejectsWrongVersionMissingFieldsUnexpectedMetadataAndDuplicates()
    {
        var wrong = Archive(); wrong["schemaVersion"] = 2;
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(wrong.ToJsonString()));
        var missing = Archive(); missing.Remove("capturedAt");
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(missing.ToJsonString()));
        var unexpected = Archive(); unexpected["bluetoothAddress"] = "not-exported";
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(unexpected.ToJsonString()));
        var duplicate = Archive().ToJsonString().Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1");
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(duplicate));
    }

    [Fact]
    public void RejectsStructuralMismatchEvenWhenDigestsWereRecomputed()
    {
        var (state, detail) = Payloads();
        var json = Archive();
        state[1] = 17;
        json["statePayloadHex"] = Convert.ToHexString(state);
        json["stateSha256"] = Convert.ToHexString(SHA256.HashData(state));
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(json.ToJsonString()));
        state[1] = 16; state[0] = 0x13;
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Encode("V1.1.2", Timestamp, state, detail));
        state[0] = 0x12; state[1] = 0;
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Encode("V1.1.2", Timestamp, state, detail));
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Encode("V1.1.2", Timestamp, state[..^1], detail));
        state[1] = 16; detail[24] = 11;
        json["statePayloadHex"] = Convert.ToHexString(state);
        json["stateSha256"] = Convert.ToHexString(SHA256.HashData(state));
        json["detailPayloadHex"] = Convert.ToHexString(detail);
        json["detailSha256"] = Convert.ToHexString(SHA256.HashData(detail));
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(json.ToJsonString()));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":\"1\"}")]
    public void NormalizesMalformedJsonToFormatException(string json) =>
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(json));

    [Fact]
    public void RejectsOversizedArchiveAndFirmware()
    {
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(new string(' ', SnapshotArchiveCodec.MaximumJsonCharacters + 1)));
        var (state, detail) = Payloads();
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Encode("V" + new string('1', 32), Timestamp, state, detail));
        var json = Archive(); json["detailPayloadHex"] = new string('0', 10330);
        Assert.Throws<FormatException>(() => SnapshotArchiveCodec.Decode(json.ToJsonString()));
    }
}
