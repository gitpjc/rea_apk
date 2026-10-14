using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Iamp.Protocol;

public sealed record SnapshotArchive(string Firmware, DateTimeOffset CapturedAt,
    byte[] StatePayload, byte[] DetailPayload, DecodedPreset Preset);

/// <summary>Versioned local snapshots only; not a vendor backup, command or device-apply format.</summary>
public static class SnapshotArchiveCodec
{
    public const int MaximumJsonCharacters = 65536;
    private const int MaximumDetailBytes = 22 + 20 * (3 + 254) + 2;
    private const string Format = "iamp-desktop-snapshot";
    private static readonly string[] Fields = ["format", "schemaVersion", "firmware", "capturedAt",
        "statePayloadHex", "detailPayloadHex", "stateSha256", "detailSha256"];

    public static string Encode(string firmware, DateTimeOffset capturedAt,
        ReadOnlySpan<byte> state, ReadOnlySpan<byte> detail)
    {
        ValidateFirmware(firmware);
        _ = ValidatePayloads(state, detail);
        return JsonSerializer.Serialize(new
        {
            format = Format,
            schemaVersion = 1,
            firmware,
            capturedAt = capturedAt.ToString("O", CultureInfo.InvariantCulture),
            statePayloadHex = Convert.ToHexString(state),
            detailPayloadHex = Convert.ToHexString(detail),
            stateSha256 = Convert.ToHexString(SHA256.HashData(state)),
            detailSha256 = Convert.ToHexString(SHA256.HashData(detail))
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    public static SnapshotArchive Decode(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumJsonCharacters)
            throw new FormatException("Snapshot JSON is empty or exceeds 65536 characters.");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new FormatException("Snapshot root must be an object.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
                if (!Fields.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name))
                    throw new FormatException("Snapshot contains an unknown or duplicate field.");
            if (seen.Count != Fields.Length || root.GetProperty("format").GetString() != Format ||
                root.GetProperty("schemaVersion").GetInt32() != 1)
                throw new FormatException("Unsupported or incomplete snapshot format/version.");
            var firmware = root.GetProperty("firmware").GetString();
            ValidateFirmware(firmware);
            var timestamp = root.GetProperty("capturedAt").GetString();
            if (!DateTimeOffset.TryParseExact(timestamp, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var capturedAt))
                throw new FormatException("Snapshot timestamp must be a valid round-trip ISO timestamp with offset.");
            var state = ReadHex(root, "statePayloadHex", 92, 92);
            var detail = ReadHex(root, "detailPayloadHex", 84, MaximumDetailBytes);
            RequireDigest(root, "stateSha256", state);
            RequireDigest(root, "detailSha256", detail);
            var preset = ValidatePayloads(state, detail);
            return new SnapshotArchive(firmware!, capturedAt, state, detail, preset);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or OverflowException)
        {
            throw new FormatException("Snapshot JSON has malformed or invalid fields.", ex);
        }
    }

    private static byte[] ReadHex(JsonElement root, string field, int minimumBytes, int maximumBytes)
    {
        var text = root.GetProperty(field).GetString();
        if (text is null || (text.Length & 1) != 0 || text.Length < minimumBytes * 2 || text.Length > maximumBytes * 2)
            throw new FormatException($"Snapshot {field} has an invalid length.");
        return Convert.FromHexString(text);
    }

    private static void RequireDigest(JsonElement root, string field, ReadOnlySpan<byte> payload)
    {
        var expected = ReadHex(root, field, 32, 32);
        if (!CryptographicOperations.FixedTimeEquals(expected, SHA256.HashData(payload)))
            throw new FormatException($"Snapshot {field} integrity check failed.");
    }

    private static void ValidateFirmware(string? firmware)
    {
        if (string.IsNullOrEmpty(firmware) || firmware.Length > 32 || !firmware.Any(char.IsAsciiDigit) ||
            !char.IsAsciiLetterOrDigit(firmware[0]) ||
            firmware.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or '-' or '+')))
            throw new FormatException("Snapshot firmware must be bounded ASCII version text.");
    }

    private static DecodedPreset ValidatePayloads(ReadOnlySpan<byte> state, ReadOnlySpan<byte> detail)
    {
        if (state.Length != 92 || state[0] != 0x12 || state[1] is < 1 or > 80)
            throw new FormatException("Snapshot state must be a 92-byte 12 response with preset index 1-80.");
        if (detail.Length > MaximumDetailBytes) throw new FormatException("Snapshot detail exceeds structural bounds.");
        var preset = PresetDataParser.Parse(detail);
        if (preset.WireIndex != state[1]) throw new FormatException("Snapshot state and detail preset indices differ.");
        return preset;
    }
}
