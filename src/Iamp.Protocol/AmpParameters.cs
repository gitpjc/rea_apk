namespace Iamp.Protocol;

public sealed record AmpParameterSpec(ObservedParameter Parameter, int ValueIndex, string Name);

/// <summary>Amp layouts and controls verified against retained mobile observations.</summary>
public static class AmpParameters
{
    public static IReadOnlyList<ushort> Models { get; } = Array.AsReadOnly<ushort>([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54]);

    public static IReadOnlyList<AmpParameterSpec> Settings { get; } = Array.AsReadOnly<AmpParameterSpec>([
        new(ObservedParameter.AmpGain, 2, "Gain"),
        new(ObservedParameter.AmpBass, 3, "Bass"),
        new(ObservedParameter.AmpMid, 4, "Mid"),
        new(ObservedParameter.AmpTreble, 5, "Treble"),
        new(ObservedParameter.AmpPresence, 6, "Presence"),
        new(ObservedParameter.AmpMaster, 7, "Master")
    ]);

    public static bool IsKnownLayout(PresetEffectBlock block)
        => block.TypeCode == 2 && block.Values.Count == 9 && Models.Contains(block.Values[1]);

    public static bool IsSupported(PresetEffectBlock block)
        => IsKnownLayout(block) && block.Values[0] == 1 &&
           Settings.All(spec => block.Values[spec.ValueIndex] <= 100) && block.Values[8] <= 1;
}
