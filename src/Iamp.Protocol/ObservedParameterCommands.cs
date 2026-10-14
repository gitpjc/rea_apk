using System.Buffers.Binary;

namespace Iamp.Protocol;

public enum ObservedParameter { NoiseThreshold, NoiseLevel, OdGain, OdTone, OdVolume, AmpBass, AmpMid, AmpTreble, AmpPresence, AmpGain, AmpMaster, DelayLevel, DelayFeedback, DelayTime, ReverbPreDelay, ReverbLevel, ReverbDecay, ReverbTone }
public sealed record ObservedParameterDefinition(byte Position, int ValueIndex, ushort Minimum, ushort Maximum);

/// <summary>Captured F15i V1.1.9 model profiles and verified ranges only. Unknown fields are preserved.</summary>
public static class ObservedParameterCommands
{
    public static ObservedParameterDefinition Definition(ObservedParameter parameter) => parameter switch
    {
        ObservedParameter.NoiseThreshold => new(1, 2, 0, 100),
        ObservedParameter.NoiseLevel => new(1, 3, 0, 100),
        ObservedParameter.OdGain => new(2, 2, 0, 100),
        ObservedParameter.OdTone => new(2, 3, 0, 100),
        ObservedParameter.OdVolume => new(2, 4, 0, 100),
        ObservedParameter.AmpBass => new(3, 3, 0, 100),
        ObservedParameter.AmpMid => new(3, 4, 0, 100),
        ObservedParameter.AmpTreble => new(3, 5, 0, 100),
        ObservedParameter.AmpPresence => new(3, 6, 0, 100),
        ObservedParameter.AmpGain => new(3, 2, 0, 100),
        ObservedParameter.AmpMaster => new(3, 7, 0, 100),
        ObservedParameter.DelayLevel => new(4, 2, 0, 100),
        ObservedParameter.DelayFeedback => new(4, 3, 0, 100),
        ObservedParameter.DelayTime => new(4, 4, 40, 2500),
        ObservedParameter.ReverbPreDelay => new(5, 2, 0, 200),
        ObservedParameter.ReverbLevel => new(5, 3, 0, 100),
        ObservedParameter.ReverbDecay => new(5, 4, 0, 100),
        ObservedParameter.ReverbTone => new(5, 5, 0, 100),
        _ => throw new ArgumentOutOfRangeException(nameof(parameter))
    };

    public static byte[] Create(PresetEffectBlock block, ObservedParameter parameter, ushort value)
    {
        var definition = Definition(parameter);
        var v = block.Values;
        var supported = definition.Position switch {
            1 => block.Position == 1 && block.TypeCode == 4 && v.Count == 6 && v[0] == 1 && v[1] == 2 &&
                v[2] <= 100 && v[3] <= 100 && v[4] == 0 && v[5] == 0,
            2 => block.Position == 2 && block.TypeCode == 1 && v.Count == 5 && v[0] == 1 && v[1] == 3 &&
                v[2] <= 100 && v[3] <= 100 && v[4] <= 100,
            3 => block.Position == 3 && block.TypeCode == 2 && v.Count == 9 && v[0] == 1 && v[1] == 31 &&
                v[2] <= 100 && v[3] <= 100 && v[4] <= 100 && v[5] <= 100 && v[6] <= 100 && v[7] <= 100 && v[8] == 1,
            4 => block.Position == 4 && block.TypeCode == 7 && v.Count == 8 && v[0] == 1 && v[1] == 1 &&
                v[2] <= 100 && v[3] <= 100 && v[4] is >=40 and <=2500 && v[5] == 0 && v[6] == 0 && v[7] == 0,
            5 => block.Position == 5 && block.TypeCode == 8 && v.Count == 10 && v[0] == 1 && v[1] == 1 &&
                v[2] <= 200 && v[3] <= 100 && v[4] <= 100 && v[5] <= 100 &&
                v[6] == 0 && v[7] == 0 && v[8] == 0 && v[9] == 0,
            _ => false
        };
        if (!supported) throw new NotSupportedException("Effect differs from the captured configuration or supported ranges.");
        if (value < definition.Minimum || value > definition.Maximum)
            throw new ArgumentOutOfRangeException(nameof(value), $"Supported range is {definition.Minimum}–{definition.Maximum}.");
        var payload = new byte[4 + v.Count * 2];
        payload[0] = 0x25; payload[1] = block.Position; payload[2] = block.TypeCode; payload[3] = checked((byte)(v.Count * 2));
        for (var i = 0; i < v.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2), i == definition.ValueIndex ? value : v[i]);
        return payload;
    }
}
