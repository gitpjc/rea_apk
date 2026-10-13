using System.Buffers.Binary;

namespace Iamp.Protocol;

/// <summary>Restricted to the position/model/values observed in the V1.1.9 phone round trip.</summary>
public static class NoiseKillerCommand
{
    public static byte[] Create(PresetEffectBlock block, ushort threshold)
    {
        if (block.Position != 1 || block.TypeCode != 4 || block.Values.Count != 6 ||
            block.Values[0] != 1 || block.Values[1] != 2 || block.Values[2] is < 25 or > 28 ||
            block.Values[3] != 50 || block.Values[4] != 0 || block.Values[5] != 0)
            throw new NotSupportedException("Only the captured first-position Noise Killer configuration is supported.");
        if (threshold is < 25 or > 28)
            throw new ArgumentOutOfRangeException(nameof(threshold), "The experimental verified range is 25–28.");
        var payload = new byte[16];
        payload[0] = 0x25; payload[1] = block.Position; payload[2] = block.TypeCode; payload[3] = 12;
        for (var i = 0; i < block.Values.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2), i == 2 ? threshold : block.Values[i]);
        return payload;
    }
}
