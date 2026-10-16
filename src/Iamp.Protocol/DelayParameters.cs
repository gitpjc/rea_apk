namespace Iamp.Protocol;

public sealed record DelayParameterSpec(ObservedParameter Parameter, byte Profile, int ValueIndex,
    string Name, string? Unit, ushort Minimum, ushort Maximum);

/// <summary>All twelve Delay models, verified against per-model mobile endpoint and full scrolled layout evidence.</summary>
public static class DelayParameters
{
    public static IReadOnlyList<ushort> Models { get; } = Array.AsReadOnly<ushort>([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);
    public static IReadOnlyList<DelayParameterSpec> All { get; } = Array.AsReadOnly<DelayParameterSpec>([
        new(ObservedParameter.DelayLevel, 4, 2, "Level", null, 0, 100),
        new(ObservedParameter.DelayFeedback, 4, 3, "Feedback", null, 0, 100),
        new(ObservedParameter.DelayTime, 4, 4, "Time", "ms", 40, 2500),
        new(ObservedParameter.DelayCrystalLevel, 14, 2, "Level", null, 0, 100),
        new(ObservedParameter.DelayCrystalFeedback, 14, 3, "Feedback", null, 0, 100),
        new(ObservedParameter.DelayCrystalTime, 14, 4, "Time", "ms", 40, 2500),
        new(ObservedParameter.DelayCrystalDepth, 14, 5, "Depth", null, 0, 100),
        new(ObservedParameter.DelayCrystalRate, 14, 6, "Rate", null, 0, 100),
        new(ObservedParameter.DelayVintageDelayLevel, 15, 2, "Level", null, 0, 100),
        new(ObservedParameter.DelayVintageDelayFeedback, 15, 3, "Feedback", null, 0, 100),
        new(ObservedParameter.DelayVintageDelayTime, 15, 4, "Time", "ms", 40, 2500),
        new(ObservedParameter.DelayVintageDelayBit, 15, 5, "Bit", null, 0, 100),
        new(ObservedParameter.DelayVintageDelaySRate, 15, 6, "S-Rate", null, 0, 100),
        new(ObservedParameter.DelayGalaxyDelayLevel, 16, 2, "Level", null, 0, 100),
        new(ObservedParameter.DelayGalaxyDelayFeedback, 16, 3, "Feedback", null, 0, 100),
        new(ObservedParameter.DelayGalaxyDelayTime, 16, 4, "Time", "ms", 40, 2500),
        new(ObservedParameter.DelayGalaxyDelayModRate, 16, 5, "Mod Rate", null, 0, 100),
        new(ObservedParameter.DelayGalaxyDelayModDepth, 16, 6, "Mod Depth", null, 0, 100),
        new(ObservedParameter.DelaySweepLevel, 17, 2, "Level", null, 0, 100),
        new(ObservedParameter.DelaySweepFeedback, 17, 3, "Feedback", null, 0, 100),
        new(ObservedParameter.DelaySweepTime, 17, 4, "Time", "ms", 40, 2500),
        new(ObservedParameter.DelaySweepRate, 17, 5, "Rate", null, 0, 100),
        new(ObservedParameter.DelaySweepRange, 17, 6, "Range", null, 300, 4000)
    ]);
    public static IReadOnlyList<DelayParameterSpec> Settings => All.Where(s => s.Profile == 4).ToArray();
    public static byte Profile(PresetEffectBlock block) => block.TypeCode == 7 && block.Values.Count == 8 ? block.Values[1] switch {
        0 or 1 or 2 or 3 or 4 or 5 or 6 => (byte)4,
        7 or 8 => (byte)14,
        9 => (byte)15,
        10 => (byte)16,
        11 => (byte)17,
        _ => (byte)0
    } : (byte)0;
    public static IReadOnlyList<DelayParameterSpec> For(PresetEffectBlock block) => All.Where(s => s.Profile == Profile(block)).ToArray();
    public static DelayParameterSpec? Get(ObservedParameter parameter) => All.FirstOrDefault(s => s.Parameter == parameter);
    public static bool IsKnownLayout(PresetEffectBlock block) => Profile(block) != 0;
    public static bool IsSupported(PresetEffectBlock block)
    {
        var specs = For(block);
        return specs.Count > 0 && block.Values[0] == 1 &&
            specs.All(s => block.Values[s.ValueIndex] >= s.Minimum && block.Values[s.ValueIndex] <= s.Maximum) &&
            block.Values.Select((value, index) => (value, index)).Where(v => v.index >= 2 && !specs.Any(s => s.ValueIndex == v.index)).All(v => v.value == 0);
    }
}
