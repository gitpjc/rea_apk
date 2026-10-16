using System.Globalization;

namespace Iamp.Protocol;

public sealed record ModParameterSpec(ObservedParameter Parameter, byte Profile, int ValueIndex,
    string Name, string? Unit, ushort Minimum, ushort Maximum, decimal Scale, decimal Offset, int DecimalPlaces)
{
    public decimal DisplayValue(ushort wire) => wire * Scale + Offset;
    public string Format(ushort wire) => DisplayValue(wire).ToString(DecimalPlaces == 0 ? "0" : "0." + new string('0', DecimalPlaces), CultureInfo.InvariantCulture);
    public bool TryParse(string text, out ushort wire)
    {
        wire = 0;
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var display)) return false;
        var value = (display - Offset) / Scale;
        if (value < Minimum || value > Maximum || value != decimal.Truncate(value)) return false;
        wire = (ushort)value; return true;
    }
}

/// <summary>Mod layouts and settings verified from complete mobile views and independent command captures.</summary>
public static class ModParameters
{
    public static IReadOnlyList<ushort> Models { get; } = Array.AsReadOnly<ushort>([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]);
    public static IReadOnlyList<ModParameterSpec> All { get; } = Array.AsReadOnly<ModParameterSpec>([
        new(ObservedParameter.ModPhaserRate, 20, 2, "Rate", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPhaserLevel, 20, 3, "Level", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPhaserDepth, 20, 4, "Depth", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModFlangerRate, 21, 2, "Rate", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModFlangerMix, 21, 3, "Mix", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModFlangerFeedback, 21, 4, "Feedback", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModTremoloRate, 22, 2, "Rate", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModTremoloMix, 22, 3, "Mix", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModTremoloTone, 22, 4, "Tone", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPolyShiftMix, 23, 2, "Mix", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPolyShiftTone, 23, 3, "Tone", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPolyShiftPitch, 23, 4, "Pitch", null, 0, 240, 0.1m, -12m, 1),
        new(ObservedParameter.ModAnaChorusRate, 24, 2, "Rate", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModAnaChorusMix, 24, 3, "Mix", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModAnaChorusTone, 24, 4, "Tone", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModAnaChorusDepth, 24, 5, "Depth", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModLofiSample, 25, 2, "Sample", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModLofiMix, 25, 3, "Mix", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModLofiBit, 25, 4, "Bit", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModSlowGearRise, 26, 2, "Rise", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModSlowGearLevel, 26, 3, "Level", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPannerRate, 27, 2, "Rate", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPannerTone, 27, 3, "Tone", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPannerDepth, 27, 4, "Depth", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModPannerDuty, 27, 5, "Duty", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModDetunePitch, 28, 2, "Pitch", "Cent", 0, 200, 1m, -100m, 0),
        new(ObservedParameter.ModDetuneTone, 28, 3, "Tone", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModDetuneMix, 28, 4, "Mix", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModOctaveSub, 29, 2, "Sub", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModOctaveSubTone, 29, 3, "Sub Tone", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModOctaveUpper, 29, 4, "Upper", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModOctaveUpperTone, 29, 5, "Upper Tone", null, 0, 100, 1m, 0m, 0),
        new(ObservedParameter.ModOctaveDry, 29, 6, "Dry", null, 0, 100, 1m, 0m, 0)
    ]);
    public static byte Profile(PresetEffectBlock block) => block.TypeCode == 6 && block.Values.Count >= 2 ? (block.Values.Count, block.Values[1]) switch {
        (7, 0 or 1) => (byte)20,
        (7, 2 or 3) => (byte)21,
        (7, 4 or 5 or 6 or 7 or 8) => (byte)22,
        (7, 9) => (byte)23,
        (7, 10 or 11) => (byte)24,
        (7, 12) => (byte)25,
        (7, 13) => (byte)26,
        (7, 14) => (byte)27,
        (7, 15) => (byte)28,
        (7, 16) => (byte)29,
        _ => (byte)0
    } : (byte)0;
    public static IReadOnlyList<ModParameterSpec> For(PresetEffectBlock block) => All.Where(s => s.Profile == Profile(block)).ToArray();
    public static ModParameterSpec? Get(ObservedParameter parameter) => All.FirstOrDefault(s => s.Parameter == parameter);
    public static ModParameterSpec? Find(PresetEffectBlock block, int index) => For(block).FirstOrDefault(s => s.ValueIndex == index);
    public static bool IsKnownLayout(PresetEffectBlock block) => Profile(block) != 0;
    public static bool IsSupported(PresetEffectBlock block)
    {
        var specs = For(block);
        return specs.Count > 0 && block.Values[0] == 1 &&
            specs.All(s => block.Values[s.ValueIndex] >= s.Minimum && block.Values[s.ValueIndex] <= s.Maximum) &&
            block.Values.Select((value, index) => (value, index)).Where(v => v.index >= 2 && !specs.Any(s => s.ValueIndex == v.index)).All(v => v.value == 0);
    }
}
