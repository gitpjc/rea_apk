using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class SnapshotComparisonTests
{
    private static SnapshotArchive Baseline() => SnapshotArchiveCodec.Decode(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "fixtures/f15-snapshot-v1.example.json")));
    private static string Encode(SnapshotArchive snapshot) => SnapshotArchiveCodec.Encode(snapshot.Firmware, snapshot.CapturedAt,
        snapshot.StatePayload, snapshot.DetailPayload);

    [Fact]
    public void IdenticalValuesAtDifferentTimesMatchWithoutChangingEitherArchive()
    {
        var file = Baseline(); var json = Encode(file);
        var result = SnapshotComparison.Compare(json, Encode(file with { CapturedAt = file.CapturedAt.AddDays(1) }));
        Assert.True(result.RawPayloadsMatch); Assert.True(result.FirmwareMatches); Assert.Empty(result.Differences);
        Assert.Equal(json, Encode(file));
    }

    [Fact]
    public void DetectsEveryOpaqueStateByteAndPreservesComparisonDirection()
    {
        var file = Baseline(); var json = Encode(file);
        for (var offset = 2; offset < file.StatePayload.Length; offset++)
        {
            var state = file.StatePayload.ToArray(); state[offset] ^= 1;
            var result = SnapshotComparison.Compare(json, Encode(file with { StatePayload = state }));
            Assert.False(result.RawPayloadsMatch);
            var difference = Assert.Single(result.Differences);
            Assert.Equal($"State byte {offset} (unmapped)", difference.Field);
            Assert.Equal(file.StatePayload[offset].ToString(), difference.FileValue);
            Assert.Equal(state[offset].ToString(), difference.DisplayedValue);
        }
    }

    [Fact]
    public void DetectsUnknownTypeParameterAndTrailerChanges()
    {
        var file = Baseline(); var detail = file.DetailPayload.ToArray(); detail[23] = 240; detail[^1] ^= 1;
        detail = PresetDataParser.WithParameterValue(detail, 1, 2, 1234);
        var result = SnapshotComparison.Compare(Encode(file), Encode(file with { DetailPayload = detail }));
        Assert.False(result.RawPayloadsMatch); Assert.Equal(3, result.Differences.Count);
        Assert.Contains(result.Differences, d => d.Field == "Effect 1 · Type code" && d.DisplayedValue == "240");
        Assert.Contains(result.Differences, d => d.Field == "Effect 1 · Field 3" && d.DisplayedValue == "1234");
        Assert.Contains(result.Differences, d => d.Field == "Unmapped preset trailer");
    }

    [Fact]
    public void DetectsStoredNamePaddingEvenWhenDecodedNamesMatch()
    {
        var file = Baseline(); var detail = file.DetailPayload.ToArray(); detail[21] = 0x7F;
        var result = SnapshotComparison.Compare(Encode(file), Encode(file with { DetailPayload = detail }));
        Assert.False(result.RawPayloadsMatch);
        Assert.Equal("Stored name bytes", Assert.Single(result.Differences).Field);
    }

    [Fact]
    public void HandlesMissingFieldsWithoutAssumingCompatibleLayouts()
    {
        var file = Baseline();
        // Independently captured first record starts at 22 and has five ushort values.
        var detail = file.DetailPayload[..33].Concat(file.DetailPayload[35..]).ToArray(); detail[24] -= 2;
        var result = SnapshotComparison.Compare(Encode(file), Encode(file with { DetailPayload = detail }));
        var difference = Assert.Single(result.Differences);
        Assert.Equal("Effect 1 · Field 5", difference.Field); Assert.Equal("Absent", difference.DisplayedValue);
    }

    [Fact]
    public void FirmwareDifferenceDoesNotImplyDifferentRawValues()
    {
        var file = Baseline(); var result = SnapshotComparison.Compare(Encode(file), Encode(file with { Firmware = "V1.1.9" }));
        Assert.True(result.RawPayloadsMatch); Assert.False(result.FirmwareMatches);
        Assert.Equal("Firmware", Assert.Single(result.Differences).Field);
    }

    [Fact]
    public void RejectsTamperedAndMalformedInputsOnEitherSide()
    {
        var json = Encode(Baseline());
        // Change a hex digit without updating its stored digest.
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var hex = document.RootElement.GetProperty("detailPayloadHex").GetString()!;
        var tampered = json.Replace(hex, hex[..^1] + (hex[^1] == '0' ? '1' : '0'));
        Assert.Throws<FormatException>(() => SnapshotComparison.Compare(tampered, json));
        Assert.Throws<FormatException>(() => SnapshotComparison.Compare(json, "{}"));
    }
}
