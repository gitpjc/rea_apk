using System.Buffers.Binary;
using System.Text;

namespace Iamp.Protocol;

public sealed record PresetEffectBlock(byte Position, byte TypeCode, IReadOnlyList<ushort> Values);
public sealed record DecodedPreset(byte WireIndex, string Name, IReadOnlyList<PresetEffectBlock> Blocks, byte[] UnknownTrailer);

/// <summary>Structural decoding verified against F15i V1.1.2 and V1.1.9 preset responses.</summary>
public static class PresetDataParser
{
    /// <summary>Offline byte-preserving transformation of one already decoded parameter; sends nothing.</summary>
    public static byte[] WithParameterValue(ReadOnlySpan<byte> payload, byte position, int parameterIndex, ushort value)
    {
        var decoded = Parse(payload);
        if (position is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(position));
        var block = decoded.Blocks[position - 1];
        if (parameterIndex < 0 || parameterIndex >= block.Values.Count) throw new ArgumentOutOfRangeException(nameof(parameterIndex));
        var offset = 22;
        foreach (var previous in decoded.Blocks.Take(position - 1)) offset += 3 + previous.Values.Count * 2;
        var result = payload.ToArray();
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset + 3 + parameterIndex * 2, 2), value);
        return result;
    }

    public static DecodedPreset Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 24 || payload[0] != 0x22 || payload[1] is < 1 or > 80)
            throw new FormatException("Not a supported F15 preset response.");
        var name = payload.Slice(2, 20);
        var zero = name.IndexOf((byte)0);
        var text = Encoding.UTF8.GetString(zero < 0 ? name : name[..zero]);
        var blocks = new List<PresetEffectBlock>();
        var offset = 22;
        for (var position = 1; position <= 20; position++)
        {
            if (offset + 3 > payload.Length) throw new FormatException("Preset record header is truncated.");
            var sequence = payload[offset++];
            var type = payload[offset++];
            var byteCount = payload[offset++];
            if (sequence != position || (byteCount & 1) != 0 || offset + byteCount > payload.Length)
                throw new FormatException("Preset record sequence or parameter length is invalid.");
            var values = new ushort[byteCount / 2];
            for (var i = 0; i < values.Length; i++)
                values[i] = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset + i * 2, 2));
            offset += byteCount;
            blocks.Add(new PresetEffectBlock(sequence, type, values));
        }
        if (payload.Length - offset != 2) throw new FormatException("Preset trailer differs from the observed schema.");
        return new DecodedPreset(payload[1], text, blocks, payload[offset..].ToArray());
    }
}
