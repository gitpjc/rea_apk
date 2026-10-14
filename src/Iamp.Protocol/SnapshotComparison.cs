using System.Globalization;

namespace Iamp.Protocol;

public sealed record SnapshotDifference(string Field, string FileValue, string DisplayedValue);
public sealed record SnapshotComparisonResult(bool RawPayloadsMatch, bool FirmwareMatches,
    IReadOnlyList<SnapshotDifference> Differences);

/// <summary>Read-only comparison of validated local archives, including every opaque payload byte.</summary>
public static class SnapshotComparison
{
    public static SnapshotComparisonResult Compare(string fileJson, string displayedJson)
    {
        var file = SnapshotArchiveCodec.Decode(fileJson);
        var displayed = SnapshotArchiveCodec.Decode(displayedJson);
        var differences = new List<SnapshotDifference>();
        void Add(string label, string left, string right)
        {
            if (left != right) differences.Add(new(label, left, right));
        }
        string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        Add("Firmware", file.Firmware, displayed.Firmware);
        Add("Preset index", Number(file.Preset.WireIndex), Number(displayed.Preset.WireIndex));
        Add("Preset name", file.Preset.Name, displayed.Preset.Name);
        // A name's zero padding can differ even when the decoded labels are identical.
        if (!file.DetailPayload.AsSpan(2, 20).SequenceEqual(displayed.DetailPayload.AsSpan(2, 20)))
            Add("Stored name bytes", Convert.ToHexString(file.DetailPayload.AsSpan(2, 20)), Convert.ToHexString(displayed.DetailPayload.AsSpan(2, 20)));
        for (var offset = 2; offset < file.StatePayload.Length; offset++)
            Add($"State byte {offset} (unmapped)", Number(file.StatePayload[offset]), Number(displayed.StatePayload[offset]));
        for (var index = 0; index < 20; index++)
        {
            var left = file.Preset.Blocks[index]; var right = displayed.Preset.Blocks[index];
            Add($"Effect {index + 1} · Type code", Number(left.TypeCode), Number(right.TypeCode));
            for (var value = 0; value < Math.Max(left.Values.Count, right.Values.Count); value++)
                Add($"Effect {index + 1} · Field {value + 1}", value < left.Values.Count ? Number(left.Values[value]) : "Absent",
                    value < right.Values.Count ? Number(right.Values[value]) : "Absent");
        }
        Add("Unmapped preset trailer", Convert.ToHexString(file.Preset.UnknownTrailer), Convert.ToHexString(displayed.Preset.UnknownTrailer));
        return new(file.StatePayload.AsSpan().SequenceEqual(displayed.StatePayload) && file.DetailPayload.AsSpan().SequenceEqual(displayed.DetailPayload),
            file.Firmware == displayed.Firmware, differences);
    }
}
