using Iamp.Protocol;

namespace Iamp.Desktop;

internal sealed record ReadOnlyParameter(string Name, ushort Value);
internal sealed record EffectPresentation(string Category, string Color, string Title,
    IReadOnlyList<ReadOnlyParameter> Parameters)
{
    // Category associations are provisional evidence from one supported preset.
    // No enabled-state, units or unknown model interpretation belongs here.
    internal static EffectPresentation From(PresetEffectBlock block)
    {
        var (category, color) = block.TypeCode switch {
            1 => ("OD", "#D6AE48"), 2 => ("Amp", "#D36964"), 9 => ("EQ", "#B1BDCD"),
            7 => ("Delay", "#57B8AE"), 8 => ("Reverb", "#879AE8"), _ => ("Effect", "#94A3B8")
        };
        if (block.TypeCode == 1 && block.Values.Count == 5 && block.Values[1] == 5)
            return new(category, color, "D-Drive", new[] {
                new ReadOnlyParameter("Gain",block.Values[2]),new ReadOnlyParameter("Tone",block.Values[3]),
                new ReadOnlyParameter("Vol",block.Values[4]) });
        return new(category,color,category,Array.Empty<ReadOnlyParameter>());
    }
}
