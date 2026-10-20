using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Iamp.Protocol;
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
                    return new {name=milliseconds?p.Name[..^suffix.Length]:p.Name,value=p.IsOff?(object)"Off":p.Value,
                        unit=p.IsOff?null:p.Unit??(milliseconds?"ms":null),group=p.Group};
                }).ToArray()
            };
        }).ToArray();
        return JsonSerializer.Serialize(new {
            schemaVersion="4.0.0",
            preset=new {slot=preset.SlotLabel,wireIndex=preset.WireIndex,name=preset.Name},
            source=new {kind=sourceKind,firmware,readAt,confirmedSnapshot=confirmed,hasUnappliedInputs=hasPendingInputs},effects,
            restore=CreateRestore(preset,firmware)
        },new JsonSerializerOptions {WriteIndented=true});
    }
    private static object? CreateRestore(CurrentPresetSnapshot preset,string? firmware)
    {
        try {
            var detail=Convert.FromHexString(preset.RawDetailPayloadHex);
            var decoded=PresetTransferRecord.Validate(firmware??"",detail);
            if(decoded.WireIndex!=preset.WireIndex || decoded.Name!=preset.Name)return null;
            return new {format="iamp-desktop-preset",deviceFamily="F15i",firmware,
                detailPayloadHex=Convert.ToHexString(detail),detailSha256=Convert.ToHexString(SHA256.HashData(detail))};
        }
        catch(Exception ex) when(ex is FormatException or NotSupportedException or ArgumentException or IOException){return null;}
    }

    internal static byte[] ParseImport(string json)
    {
        if(Encoding.UTF8.GetByteCount(json)>1024*1024)throw new FormatException("Preset file is too large.");
        try {
            using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});var root=document.RootElement;
            void NoDuplicates(JsonElement node){
                if(node.ValueKind==JsonValueKind.Object){var names=new HashSet<string>(StringComparer.Ordinal);foreach(var item in node.EnumerateObject()){if(!names.Add(item.Name))throw new FormatException("Preset JSON contains duplicate fields.");NoDuplicates(item.Value);}}
                else if(node.ValueKind==JsonValueKind.Array)foreach(var item in node.EnumerateArray())NoDuplicates(item);
            }
            NoDuplicates(root);
            if(root.TryGetProperty("format",out var fileFormat)&&fileFormat.ValueKind==JsonValueKind.String&&fileFormat.GetString()=="iamp-desktop-snapshot")
                throw new FormatException("This is a Snapshot file. Import requires a Preset export (Preset-…json). To view a snapshot, use Open snapshot on the Snapshots screen.");
            if(!root.TryGetProperty("schemaVersion",out var schema)||schema.ValueKind!=JsonValueKind.String||schema.GetString()!="4.0.0")throw new FormatException("Choose a preset exported with the Export button. Older display-only JSON cannot be restored completely.");
            var restore=root.GetProperty("restore");
            if(restore.ValueKind!=JsonValueKind.Object || restore.GetProperty("format").GetString()!="iamp-desktop-preset" || restore.GetProperty("deviceFamily").GetString()!="F15i")throw new FormatException("This file does not contain a supported complete preset export.");
            var firmware=restore.GetProperty("firmware").GetString()!;
            var hex=restore.GetProperty("detailPayloadHex").GetString();
            if(hex is null || hex.Length>PresetTransferRecord.MaximumDetailBytes*2)throw new FormatException("Invalid preset payload length.");
            var detail=Convert.FromHexString(hex);var decoded=PresetTransferRecord.Validate(firmware,detail);
            var digest=restore.GetProperty("detailSha256").GetString();
            if(digest is null || !string.Equals(digest,Convert.ToHexString(SHA256.HashData(detail)),StringComparison.OrdinalIgnoreCase))throw new FormatException("Preset integrity check failed. Export the preset again.");
            var header=root.GetProperty("preset");
            if(header.GetProperty("wireIndex").GetInt32()!=decoded.WireIndex || header.GetProperty("name").GetString()!=decoded.Name || header.GetProperty("slot").GetString()!=PresetAddress.FromWireIndex(decoded.WireIndex).Label)throw new FormatException("Preset name or slot differs from its restore data.");
            var snapshot=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,"",hex,decoded.Blocks);
            using var regenerated=JsonDocument.Parse(Create(snapshot,firmware,null,"file",true,false));
            if(!JsonElement.DeepEquals(root.GetProperty("effects"),regenerated.RootElement.GetProperty("effects")))throw new FormatException("Readable settings differ from the restore data. Import an unmodified export.");
            return detail;
        }
        catch(Exception ex) when(ex is JsonException or KeyNotFoundException or InvalidOperationException or OverflowException or ArgumentException){throw new FormatException("Invalid preset export file.",ex);}
    }
    internal static string DefaultFileName(CurrentPresetSnapshot preset)
    {
        var name=new string(preset.Name.Select(c=>char.IsControl(c)||Path.GetInvalidFileNameChars().Contains(c)?'_':c).ToArray()).Trim().TrimEnd('.');
        if(string.IsNullOrWhiteSpace(name))name="Preset";
        if(name.Length>120)name=name[..120].TrimEnd();
        return $"Preset-{preset.SlotLabel}-{name.Replace(' ','-')}_{DateTime.Now.ToString("yyyy-MM-dd_HHmm",System.Globalization.CultureInfo.InvariantCulture)}.json";
    }
    internal static async Task SaveAsync(string path,string json)
    {
        var destination=Path.GetFullPath(path);var temporary=destination+$".{Guid.NewGuid():N}.tmp";
        try {await File.WriteAllTextAsync(temporary,json,new UTF8Encoding(false)).ConfigureAwait(false);File.Move(temporary,destination,true);}
        finally {if(File.Exists(temporary))File.Delete(temporary);}
    }
}
