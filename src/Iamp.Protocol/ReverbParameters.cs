namespace Iamp.Protocol;

public sealed record ReverbParameterSpec(ObservedParameter Parameter, byte Profile, int ValueIndex,
    string Name, string? Unit, ushort Minimum, ushort Maximum);

/// <summary>All nine Reverb models, verified against per-model mobile endpoints and complete scrolled layouts.</summary>
public static class ReverbParameters
{
    public static IReadOnlyList<ushort> Models { get; } = Array.AsReadOnly<ushort>([0, 1, 2, 3, 4, 5, 6, 7, 8]);
    public static IReadOnlyList<ReverbParameterSpec> All { get; } = Array.AsReadOnly<ReverbParameterSpec>([
        new(ObservedParameter.ReverbPreDelay, 5, 2, "Pre Delay", "ms", 0, 200),
        new(ObservedParameter.ReverbLevel, 5, 3, "Level", null, 0, 100),
        new(ObservedParameter.ReverbDecay, 5, 4, "Decay", null, 0, 100),
        new(ObservedParameter.ReverbTone, 5, 5, "Tone", null, 0, 100),
        new(ObservedParameter.ReverbFlReverbPreDelay, 18, 2, "Pre Delay", "ms", 0, 200),
        new(ObservedParameter.ReverbFlReverbLevel, 18, 3, "Level", null, 0, 100),
        new(ObservedParameter.ReverbFlReverbDecay, 18, 4, "Decay", null, 0, 100),
        new(ObservedParameter.ReverbFlReverbTone, 18, 5, "Tone", null, 0, 100),
        new(ObservedParameter.ReverbFlReverbRate, 18, 6, "Rate", null, 0, 100),
        new(ObservedParameter.ReverbFlReverbFeedback, 18, 7, "Feedback", null, 0, 100),
        new(ObservedParameter.ReverbFlReverbModDelay, 18, 8, "Mod Delay", null, 0, 100),
        new(ObservedParameter.ReverbFlReverbModLevel, 18, 9, "Mod Level", null, 0, 100),
        new(ObservedParameter.ReverbDistReverbPreDelay, 19, 2, "Pre Delay", "ms", 0, 200),
        new(ObservedParameter.ReverbDistReverbLevel, 19, 3, "Level", null, 0, 100),
        new(ObservedParameter.ReverbDistReverbDecay, 19, 4, "Decay", null, 0, 100),
        new(ObservedParameter.ReverbDistReverbTone, 19, 5, "Tone", null, 0, 100),
        new(ObservedParameter.ReverbDistReverbGain, 19, 6, "Gain", null, 0, 100),
        new(ObservedParameter.ReverbDistReverbDistLevel, 19, 7, "Dist Level", null, 0, 100)
    ]);
    public static byte Profile(PresetEffectBlock block) => block.TypeCode == 8 && block.Values.Count == 10 ? block.Values[1] switch {
        0 or 1 or 2 or 3 or 4 or 5 or 7 => (byte)5,
        6 => (byte)18,
        8 => (byte)19,
        _ => (byte)0
    } : (byte)0;
    public static IReadOnlyList<ReverbParameterSpec> For(PresetEffectBlock block) => All.Where(s => s.Profile == Profile(block)).ToArray();
    public static ReverbParameterSpec? Get(ObservedParameter parameter) => All.FirstOrDefault(s => s.Parameter == parameter);
    public static bool IsKnownLayout(PresetEffectBlock block) => Profile(block) != 0;
    public static bool IsSupported(PresetEffectBlock block)
    {
        var specs = For(block);
        return specs.Count > 0 && block.Values[0] == 1 &&
            specs.All(s => block.Values[s.ValueIndex] >= s.Minimum && block.Values[s.ValueIndex] <= s.Maximum) &&
            block.Values.Select((value, index) => (value, index)).Where(v => v.index >= 2 && !specs.Any(s => s.ValueIndex == v.index)).All(v => v.value == 0);
    }
}
