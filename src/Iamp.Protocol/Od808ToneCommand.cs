using System.Buffers.Binary;

namespace Iamp.Protocol;

/// <summary>Complete second-position 808 record observed in the V1.1.9 phone tone round trip.</summary>
public static class Od808ToneCommand
{
    public static byte[] Create(PresetEffectBlock block, ushort tone)
    {
        if (block.Position != 2 || block.TypeCode != 1 || block.Values.Count != 5 ||
            block.Values[0] != 1 || block.Values[1] != 3 || block.Values[2] != 39 ||
            block.Values[3] is < 97 or > 100 || block.Values[4] != 100)
            throw new NotSupportedException("Only the captured second-position 808 configuration is supported.");
        if (tone is < 97 or > 100)
            throw new ArgumentOutOfRangeException(nameof(tone), "The experimental tone range is 97–100.");
        var payload = new byte[14];
        payload[0] = 0x25; payload[1] = block.Position; payload[2] = block.TypeCode; payload[3] = 10;
        for (var i = 0; i < block.Values.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2), i == 3 ? tone : block.Values[i]);
        return payload;
    }
}
