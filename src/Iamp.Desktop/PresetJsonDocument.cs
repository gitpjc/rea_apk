using System.IO;
using System.Text;
using System.Text.Json;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

internal static class PresetJsonDocument
{
    internal static string Create(CurrentPresetSnapshot preset,string? firmware,DateTimeOffset? readAt,string sourceKind,bool confirmed,bool hasPendingInputs)
    {
        var effects=preset.Blocks.Where(b=>b.TypeCode!=0).Select(block=> {
            var shown=EffectPresentation.From(block);
            return new {
                position=block.Position,category=shown.Category,name=shown.Title,enabled=shown.Enabled,
                settings=shown.Parameters.Select(p=> {
                    const string suffix=" (ms)";var milliseconds=p.Name.EndsWith(suffix,StringComparison.Ordinal);
                    return new {name=milliseconds?p.Name[..^suffix.Length]:p.Name,value=p.Value,unit=milliseconds?"ms":null};
                }).ToArray()
            };
        }).ToArray();
        return JsonSerializer.Serialize(new {
            schemaVersion="2.0.0",
            preset=new {slot=preset.SlotLabel,wireIndex=preset.WireIndex,name=preset.Name},
            source=new {kind=sourceKind,firmware,readAt,confirmedSnapshot=confirmed,hasUnappliedInputs=hasPendingInputs},effects
        },new JsonSerializerOptions {WriteIndented=true});
    }
    internal static string DefaultFileName(CurrentPresetSnapshot preset)
    {
        var name=new string(preset.Name.Select(c=>char.IsControl(c)||Path.GetInvalidFileNameChars().Contains(c)?'_':c).ToArray()).Trim().TrimEnd('.');
        if(string.IsNullOrWhiteSpace(name))name="Preset";
        if(name.Length>120)name=name[..120].TrimEnd();
        return $"{preset.SlotLabel} {name}.json";
    }
    internal static async Task SaveAsync(string path,string json)
    {
        var destination=Path.GetFullPath(path);var temporary=destination+$".{Guid.NewGuid():N}.tmp";
        try {await File.WriteAllTextAsync(temporary,json,new UTF8Encoding(false)).ConfigureAwait(false);File.Move(temporary,destination,true);}
        finally {if(File.Exists(temporary))File.Delete(temporary);}
    }
}
