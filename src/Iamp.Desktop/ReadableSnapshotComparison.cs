using System.Globalization;
using Iamp.Protocol;

namespace Iamp.Desktop;

internal sealed record ReadableSnapshotComparison(SnapshotComparisonResult Stored,SnapshotArchive File,
    SnapshotArchive Displayed,string FileName,bool DisplayedIsLocal)
{
    internal IReadOnlyList<SnapshotDifference> Differences=>Stored.Differences;
    internal bool RawPayloadsMatch=>Stored.RawPayloadsMatch;

    internal static PresetEffectBlock? FindMatchingEffect(PresetEffectBlock effect,IReadOnlyList<PresetEffectBlock> own,IReadOnlyList<PresetEffectBlock> other)
    {
        bool SameModel(PresetEffectBlock b)=>effect.TypeCode==b.TypeCode&&effect.Values.Count>=2&&b.Values.Count>=2&&effect.Values[1]==b.Values[1];
        var occurrence=own.Where(b=>b.Position<effect.Position).Count(SameModel);
        return other.Where(b=>b.TypeCode!=0).OrderBy(b=>b.Position).Where(SameModel).Skip(occurrence).FirstOrDefault();
    }

    internal static string Format(ReadOnlyParameter p)
    {
        if(p.IsOff)return "Off";
        var unit=p.Unit??(p.Name.EndsWith(" (ms)",StringComparison.Ordinal)?"ms":null);
        return p.Value.ToString("0.################",CultureInfo.InvariantCulture)+(string.IsNullOrEmpty(unit)?"":" "+unit);
    }
    internal static string Label(ReadOnlyParameter p)=>p.Name.EndsWith(" (ms)",StringComparison.Ordinal)?p.Name[..^5]:p.Name;
    internal static string State(EffectPresentation shown)=>shown.Enabled switch{true=>"Enabled",false=>"Disabled",_=>"State unavailable"};
}
