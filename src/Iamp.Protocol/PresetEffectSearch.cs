using System.Text.Json;

namespace Iamp.Protocol;

public sealed record EffectDefinition(byte TypeCode,ushort Model,string Category,string Name)
{
    public string DisplayName=>$"{Category} · {Name}";
}

/// <summary>Read-only names for searching; catalog membership never grants write support.</summary>
public static class PresetEffectSearch
{
    public static IReadOnlyList<EffectDefinition> Catalog {get;}=LoadCatalog();

    private static IReadOnlyList<EffectDefinition> LoadCatalog()
    {
        using var stream=typeof(PresetEffectSearch).Assembly.GetManifestResourceStream("Iamp.Protocol.effect_catalog.json")
            ??throw new InvalidOperationException("Effect search catalog is missing.");
        var entries=JsonSerializer.Deserialize<EffectDefinition[]>(stream)??throw new FormatException("Invalid effect catalog.");
        if(entries.Any(e=>e.TypeCode==0||string.IsNullOrWhiteSpace(e.Category)||string.IsNullOrWhiteSpace(e.Name)) ||
            entries.Select(e=>(e.TypeCode,e.Model)).Distinct().Count()!=entries.Length)
            throw new FormatException("Effect search catalog contains invalid or duplicate keys.");
        return Array.AsReadOnly(entries);
    }

    public static bool Contains(IEnumerable<PresetEffectBlock> blocks,byte typeCode,ushort model)
        =>typeCode!=0 && blocks.Any(b=>b.TypeCode==typeCode && b.Values.Count>=2 && b.Values[1]==model);

    public static bool MatchesText(int wireIndex,string name,string term)
    {
        term=term.Trim();
        return name.Contains(term,StringComparison.OrdinalIgnoreCase) ||
            PresetAddress.FromWireIndex(wireIndex).Label.Contains(term,StringComparison.OrdinalIgnoreCase) ||
            wireIndex.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(term,StringComparison.Ordinal);
    }

    public static IReadOnlyDictionary<int,IReadOnlyList<PresetEffectBlock>> CreateIndex(IEnumerable<DecodedPreset> presets)
    {
        var index=new Dictionary<int,IReadOnlyList<PresetEffectBlock>>();
        foreach(var preset in presets) {
            if(preset.WireIndex is <1 or >80 || !index.TryAdd(preset.WireIndex,preset.Blocks))
                throw new FormatException("Preset effect index has an invalid or duplicate slot.");
        }
        return index;
    }
}
