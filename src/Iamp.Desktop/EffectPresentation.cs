using Iamp.Protocol;

namespace Iamp.Desktop;

internal sealed record ReadOnlyParameter(string Name, decimal Value,int ValueIndex,
    string? Unit=null,string? Group=null,bool IsOff=false);
internal sealed record EffectPresentation(string Category, string Color, string Title,
    IReadOnlyList<ReadOnlyParameter> Parameters, PresetEffectBlock Source)
{
    // Model/parameter labels are restricted to retained phone screenshots matched to Windows reads.
    internal bool? Enabled=>EffectBypassCommand.TryGetEnabled(Source,out var enabled)?enabled:null;
    // Unresolved model fields and coincident zero/equal-value parameters are omitted.
    internal static EffectPresentation From(PresetEffectBlock block)
    {
        var (category, color) = block.TypeCode switch {
            1 => ("OD", "#D6AE48"), 2 => ("Amp", "#D36964"), 4 => ("Dyna", "#FFA45C"), 9 => ("EQ", "#20D5D2"),
            5 => ("Filter", "#F58CEB"), 6 => ("Mod", "#52B7FF"),
            7 => ("Delay", "#52DC38"), 8 => ("Reverb", "#879AE8"), _ => ("Effect", "#94A3B8")
        };
        if(OdParameters.IsKnownLayout(block))
            return new(category,color,block.Values[1]==3?"OD808":PresetEffectSearch.Catalog.Single(e=>e.TypeCode==1&&e.Model==block.Values[1]).Name,
                OdParameters.Settings.Select(s=>new ReadOnlyParameter(s.Name,block.Values[s.ValueIndex],s.ValueIndex)).ToArray(),block);
        var eqParameters=EqParameters.For(block);
        if(eqParameters.Count>0)
            return new(category,color,block.Values[1]==0?"5 Bands EQ":"Custom EQ",eqParameters.Select(spec=> {
                var raw=block.Values[spec.ValueIndex];
                return new ReadOnlyParameter(spec.Name,spec.DisplayValue(raw),spec.ValueIndex,spec.Unit,spec.Group,spec.IsOff(raw));
            }).ToArray(),block);
        if (block.TypeCode == 1 && block.Values.Count == 5 && block.Values[1] == 5)
            return new(category, color, "D-Drive", new[] {
                new ReadOnlyParameter("Gain",block.Values[2],2),new ReadOnlyParameter("Tone",block.Values[3],3),
                new ReadOnlyParameter("Vol",block.Values[4],4) },block);
        if (block.TypeCode == 5 && block.Values.Count == 6 && block.Values[1] == 0)
            return new(category,color,"Touch Wah",new[] {
                new ReadOnlyParameter("Attack",block.Values[2],2),new ReadOnlyParameter("Peak",block.Values[3],3),
                new ReadOnlyParameter("Sensitivity",block.Values[4],4),new ReadOnlyParameter("Level",block.Values[5],5) },block);
        if (block.TypeCode == 5 && block.Values.Count == 6 && block.Values[1] == 1)
            return new(category,color,"Auto Wah",new[] {
                new ReadOnlyParameter("Rate",block.Values[2],2),new ReadOnlyParameter("Peak",block.Values[3],3),
                new ReadOnlyParameter("Range",block.Values[4],4),new ReadOnlyParameter("Level",block.Values[5],5) },block);
        if (block.TypeCode == 5 && block.Values.Count == 6 && block.Values[1] == 2)
            return new(category,color,"Q-Filter",new[] {
                new ReadOnlyParameter("Rate",block.Values[2],2),new ReadOnlyParameter("Q",block.Values[3],3),
                new ReadOnlyParameter("Mix",block.Values[4],4) },block);
        if (block.TypeCode == 4 && block.Values.Count == 6 && block.Values[1] == 0)
            return new(category,color,"Red Comp",new[] {
                new ReadOnlyParameter("Sensitivity",block.Values[2],2),new ReadOnlyParameter("Level",block.Values[3],3) },block);
        if (block.TypeCode == 4 && block.Values.Count == 6 && block.Values[1] == 1)
            return new(category,color,"Yellow Comp",new[] {
                new ReadOnlyParameter("Attack",block.Values[2],2),new ReadOnlyParameter("Ratio",block.Values[3],3),
                new ReadOnlyParameter("Threshold",block.Values[4],4),new ReadOnlyParameter("Level",block.Values[5],5) },block);
        if (block.TypeCode == 4 && block.Values.Count == 6 && block.Values[1] == 3)
            return new(category,color,"Intel Reducer",new[] {
                new ReadOnlyParameter("Threshold",block.Values[2],2),new ReadOnlyParameter("Depth",block.Values[3],3),
                new ReadOnlyParameter("Level",block.Values[4],4) },block);
        if (block.TypeCode == 4 && block.Values.Count == 6 && block.Values[1] == 2)
            return new(category,color,"Noise Killer",new[] {
                new ReadOnlyParameter("Threshold",block.Values[2],2),new ReadOnlyParameter("Level",block.Values[3],3) },block);
        if (AmpParameters.IsKnownLayout(block))
            return new(category,color,PresetEffectSearch.Catalog.Single(e=>e.TypeCode==2&&e.Model==block.Values[1]).Name,
                AmpParameters.Settings.Select(s=>new ReadOnlyParameter(s.Name,block.Values[s.ValueIndex],s.ValueIndex)).ToArray(),block);
        if (DelayParameters.IsKnownLayout(block))
            return new(category,color,block.Values[1]==1?"Analog Delay":PresetEffectSearch.Catalog.Single(e=>e.TypeCode==7&&e.Model==block.Values[1]).Name,
                DelayParameters.For(block).Select(s=>new ReadOnlyParameter(s.Name,block.Values[s.ValueIndex],s.ValueIndex,s.Unit)).ToArray(),block);
        if (ReverbParameters.IsKnownLayout(block))
            return new(category,color,block.Values[1]==1?"Hall Reverb":PresetEffectSearch.Catalog.Single(e=>e.TypeCode==8&&e.Model==block.Values[1]).Name,
                ReverbParameters.For(block).Select(s=>new ReadOnlyParameter(s.Name,block.Values[s.ValueIndex],s.ValueIndex,s.Unit)).ToArray(),block);
        if (ModParameters.IsKnownLayout(block))
            return new(category,color,PresetEffectSearch.Catalog.Single(e=>e.TypeCode==6&&e.Model==block.Values[1]).Name,
                ModParameters.For(block).Select(s=>new ReadOnlyParameter(s.Name,s.DisplayValue(block.Values[s.ValueIndex]),s.ValueIndex,s.Unit)).ToArray(),block);
        var known=block.Values.Count>=2?PresetEffectSearch.Catalog.FirstOrDefault(e=>e.TypeCode==block.TypeCode && e.Model==block.Values[1]):null;
        return new(category,color,known?.Name??category,Array.Empty<ReadOnlyParameter>(),block);
    }
}
