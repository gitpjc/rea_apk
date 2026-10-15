using System.Text.Json;

namespace Iamp.Protocol;

public sealed record EffectIdentity(byte TypeCode,ushort Model);
public sealed record CachedPresetEffects(byte WireIndex,EffectIdentity[] Effects);
public sealed record PresetEffectCache(string Format,int SchemaVersion,string Firmware,DateTimeOffset RefreshedAt,CachedPresetEffects[] Presets);

/// <summary>Search identities only; contains no parameter values and cannot be applied to hardware.</summary>
public static class PresetEffectCacheCodec
{
    public const int MaximumCharacters=128*1024;
    private const string Format="iamp-preset-effect-cache";

    public static string Encode(string firmware,DateTimeOffset refreshedAt,IEnumerable<DecodedPreset> records)
    {
        var cache=new PresetEffectCache(Format,1,firmware,refreshedAt,records.Select(p=>new CachedPresetEffects(p.WireIndex,
            p.Blocks.Where(b=>b.TypeCode!=0 && b.Values.Count>=2).Select(b=>new EffectIdentity(b.TypeCode,b.Values[1])).Distinct().ToArray())).ToArray());
        Validate(cache,firmware);return JsonSerializer.Serialize(cache);
    }

    public static PresetEffectCache Decode(string json,string firmware)
    {
        if(string.IsNullOrWhiteSpace(json)||json.Length>MaximumCharacters)throw new FormatException("Effect cache exceeds its format budget.");
        try {
            var cache=JsonSerializer.Deserialize<PresetEffectCache>(json,new JsonSerializerOptions{MaxDepth=8})??throw new FormatException("Empty effect cache.");
            Validate(cache,firmware);return cache;
        }
        catch(JsonException e){throw new FormatException("Invalid effect cache.",e);}
    }

    public static IReadOnlyList<DecodedPreset> ToRecords(PresetEffectCache cache)
        =>cache.Presets.Select(p=>new DecodedPreset(p.WireIndex,"",p.Effects.Select((e,i)=>
            new PresetEffectBlock((byte)(i+1),e.TypeCode,new ushort[]{0,e.Model})).ToArray(),Array.Empty<byte>())).ToArray();

    private static void Validate(PresetEffectCache cache,string firmware)
    {
        if(cache.Format!=Format || cache.SchemaVersion!=1 || firmware!="V1.1.9" || cache.Firmware!=firmware ||
            cache.RefreshedAt==default || cache.Presets is null || cache.Presets.Length!=80 ||
            cache.Presets.Any(p=>p is null || p.WireIndex is <1 or >80 || p.Effects is null || p.Effects.Length>20 || p.Effects.Any(e=>e is null || e.TypeCode==0)) ||
            cache.Presets.Select(p=>p.WireIndex).Distinct().Count()!=80)
            throw new FormatException("Effect cache identity, schema or preset coverage is invalid.");
    }
}
