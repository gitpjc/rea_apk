using System.Buffers.Binary;

namespace Iamp.Protocol;

public enum ObservedParameter { NoiseThreshold, NoiseLevel, OdGain, OdTone, OdVolume }
public sealed record ObservedParameterDefinition(byte Position, int ValueIndex, ushort Minimum, ushort Maximum);

/// <summary>Only captured F15i V1.1.9 models and nearby experiment intervals; unknown fields are preserved.</summary>
public static class ObservedParameterCommands
{
    public static ObservedParameterDefinition Definition(ObservedParameter parameter) => parameter switch
    {
        ObservedParameter.NoiseThreshold => new(1, 2, 25, 28),
        ObservedParameter.NoiseLevel => new(1, 3, 47, 50),
        ObservedParameter.OdGain => new(2, 2, 36, 39),
        ObservedParameter.OdTone => new(2, 3, 97, 100),
        ObservedParameter.OdVolume => new(2, 4, 97, 100),
        _ => throw new ArgumentOutOfRangeException(nameof(parameter))
    };

    public static byte[] Create(PresetEffectBlock block, ObservedParameter parameter, ushort value)
    {
        var definition = Definition(parameter);
        var v = block.Values;
        var supported = definition.Position == 1
            ? block.Position == 1 && block.TypeCode == 4 && v.Count == 6 && v[0] == 1 && v[1] == 2 &&
                v[2] is >= 25 and <= 28 && v[3] is >= 47 and <= 50 && v[4] == 0 && v[5] == 0
            : block.Position == 2 && block.TypeCode == 1 && v.Count == 5 && v[0] == 1 && v[1] == 3 &&
                v[2] is >= 36 and <= 39 && v[3] is >= 97 and <= 100 && v[4] is >= 97 and <= 100;
        if (!supported) throw new NotSupportedException("Effect differs from the captured configuration or supported experiment intervals.");
        if (value < definition.Minimum || value > definition.Maximum)
            throw new ArgumentOutOfRangeException(nameof(value), $"Supported experimental interval is {definition.Minimum}–{definition.Maximum}.");
        var payload = new byte[4 + v.Count * 2];
        payload[0] = 0x25; payload[1] = block.Position; payload[2] = block.TypeCode; payload[3] = checked((byte)(v.Count * 2));
        for (var i = 0; i < v.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2), i == definition.ValueIndex ? value : v[i]);
        return payload;
    }
}
