using Iamp.Protocol;

namespace Iamp.Desktop;

internal sealed record ReadOnlyParameter(string Name, ushort Value);
internal sealed record EffectPresentation(string Category, string Color, string Title,
    IReadOnlyList<ReadOnlyParameter> Parameters, PresetEffectBlock Source)
{
    // Model/parameter labels are restricted to retained phone screenshots matched to Windows reads.
    // Unresolved fields, including enabled-state and coincident zero/equal-value parameters, are omitted.
    internal static EffectPresentation From(PresetEffectBlock block)
    {
        var (category, color) = block.TypeCode switch {
            1 => ("OD", "#D6AE48"), 2 => ("Amp", "#D36964"), 4 => ("Dyna", "#FFA45C"), 9 => ("EQ", "#B1BDCD"),
            7 => ("Delay", "#57B8AE"), 8 => ("Reverb", "#879AE8"), _ => ("Effect", "#94A3B8")
        };
        if (block.TypeCode == 1 && block.Values.Count == 5 && block.Values[1] == 5)
            return new(category, color, "D-Drive", new[] {
                new ReadOnlyParameter("Gain",block.Values[2]),new ReadOnlyParameter("Tone",block.Values[3]),
                new ReadOnlyParameter("Vol",block.Values[4]) },block);
        if (block.TypeCode == 4 && block.Values.Count == 6 && block.Values[1] == 2)
            return new(category,color,"Noise Killer",new[] {
                new ReadOnlyParameter("Threshold",block.Values[2]),new ReadOnlyParameter("Level",block.Values[3]) },block);
        if (block.TypeCode == 1 && block.Values.Count == 5 && block.Values[1] == 3)
            return new(category,color,"OD808",new[] {
                new ReadOnlyParameter("Gain",block.Values[2]),new ReadOnlyParameter("Tone",block.Values[3]),
                new ReadOnlyParameter("Vol",block.Values[4]) },block);
        if (block.TypeCode == 2 && block.Values.Count == 9 && block.Values[1] == 31)
            return new(category,color,"PLX 100",new[] {
                new ReadOnlyParameter("Gain",block.Values[2]),new ReadOnlyParameter("Bass",block.Values[3]),
                new ReadOnlyParameter("Mid",block.Values[4]),new ReadOnlyParameter("Treble",block.Values[5]),
                new ReadOnlyParameter("Presence",block.Values[6]),new ReadOnlyParameter("Master",block.Values[7]) },block);
        if (block.TypeCode == 7 && block.Values.Count == 8 && block.Values[1] == 1)
            return new(category,color,"Analog Delay",new[] {
                new ReadOnlyParameter("Level",block.Values[2]),new ReadOnlyParameter("Feedback",block.Values[3]),
                new ReadOnlyParameter("Time (ms)",block.Values[4]) },block);
        if (block.TypeCode == 8 && block.Values.Count == 10 && block.Values[1] == 1)
            return new(category,color,"Hall Reverb",new[] {
                new ReadOnlyParameter("Pre-delay (ms)",block.Values[2]),new ReadOnlyParameter("Level",block.Values[3]),
                new ReadOnlyParameter("Decay",block.Values[4]) },block);
        return new(category,color,category,Array.Empty<ReadOnlyParameter>(),block);
    }
}
