namespace Iamp.Protocol;

public sealed record OdParameterSpec(ObservedParameter Parameter, int ValueIndex, string Name);

/// <summary>OD controls verified against retained mobile observations.</summary>
public static class OdParameters
{
    public static IReadOnlyList<ushort> Models { get; } = Array.AsReadOnly<ushort>([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21]);
    public static IReadOnlyList<OdParameterSpec> Settings { get; } = Array.AsReadOnly<OdParameterSpec>([
        new(ObservedParameter.OdGain, 2, "Gain"),
        new(ObservedParameter.OdTone, 3, "Tone"),
        new(ObservedParameter.OdVolume, 4, "Vol")
    ]);

    public static bool IsKnownLayout(PresetEffectBlock block)
        => block.TypeCode == 1 && block.Values.Count == 5 && Models.Contains(block.Values[1]);

    public static bool IsSupported(PresetEffectBlock block)
        => IsKnownLayout(block) && block.Values[0] == 1 && block.Values.Skip(2).All(value => value <= 100);
}
