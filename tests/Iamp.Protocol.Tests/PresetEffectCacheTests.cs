using System.Text.Json.Nodes;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class PresetEffectCacheTests
{
    private static readonly DateTimeOffset At=new(2026,10,15,0,0,0,TimeSpan.FromHours(11));
    private static DecodedPreset[] Records()=>Enumerable.Range(1,80).Select(i=>new DecodedPreset((byte)i,"Private preset name",
        new[]{new PresetEffectBlock(1,4,new ushort[]{(ushort)(i%2),2,25,50,0,0}),new PresetEffectBlock(2,0,Array.Empty<ushort>())},Array.Empty<byte>())).ToArray();
    private static string Json()=>PresetEffectCacheCodec.Encode("V1.1.9",At,Records());

    [Fact]
    public void RoundTripCachesSearchIdentityWithoutNamesParametersOrEnableFlags()
    {
        var json=Json();Assert.DoesNotContain("Private preset name",json);Assert.DoesNotContain("Values",json);Assert.DoesNotContain("Raw",json);
        var cache=PresetEffectCacheCodec.Decode(json,"V1.1.9");Assert.Equal(At,cache.RefreshedAt);
        var records=PresetEffectCacheCodec.ToRecords(cache);Assert.Equal(80,records.Count);
        Assert.All(records,p=>{Assert.True(PresetEffectSearch.Contains(p.Blocks,4,2));Assert.Single(p.Blocks);Assert.Equal(new ushort[]{0,2},p.Blocks[0].Values);});
    }

    [Theory]
    [InlineData("Format","wrong")][InlineData("Firmware","V1.1.2")]
    public void RejectsWrongIdentity(string field,string value)
    {
        var root=JsonNode.Parse(Json())!;root[field]=value;
        Assert.Throws<FormatException>(()=>PresetEffectCacheCodec.Decode(root.ToJsonString(),"V1.1.9"));
    }

    [Theory]
    [InlineData("schema")][InlineData("missing")][InlineData("duplicate")][InlineData("slot0")]
    [InlineData("slot81")][InlineData("nullRecord")][InlineData("nullEffects")][InlineData("emptyType")]
    [InlineData("tooManyEffects")][InlineData("nullEffect")][InlineData("noDate")]
    public void RejectsMalformedCoverageAndRecords(string issue)
    {
        var root=JsonNode.Parse(Json())!;var presets=root["Presets"]!.AsArray();
        switch(issue) {
            case "schema":root["SchemaVersion"]=2;break;
            case "missing":presets.RemoveAt(0);break;
            case "duplicate":presets[1]!["WireIndex"]=1;break;
            case "slot0":presets[0]!["WireIndex"]=0;break;
            case "slot81":presets[0]!["WireIndex"]=81;break;
            case "nullRecord":presets[0]=null;break;
            case "nullEffects":presets[0]!["Effects"]=null;break;
            case "emptyType":presets[0]!["Effects"]![0]!["TypeCode"]=0;break;
            case "tooManyEffects":for(var i=0;i<20;i++)presets[0]!["Effects"]!.AsArray().Add(new JsonObject{["TypeCode"]=4,["Model"]=2});break;
            case "nullEffect":presets[0]!["Effects"]![0]=null;break;
            case "noDate":root["RefreshedAt"]=default(DateTimeOffset).ToString("O");break;
        }
        Assert.Throws<FormatException>(()=>PresetEffectCacheCodec.Decode(root.ToJsonString(),"V1.1.9"));
    }

    [Theory]
    [InlineData("")][InlineData("{")][InlineData("null")]
    public void RejectsInvalidJson(string json)=>Assert.Throws<FormatException>(()=>PresetEffectCacheCodec.Decode(json,"V1.1.9"));

    [Fact]
    public void RejectsOversizeAndUnverifiedFirmware()
    {
        Assert.Throws<FormatException>(()=>PresetEffectCacheCodec.Decode(new string(' ',PresetEffectCacheCodec.MaximumCharacters+1),"V1.1.9"));
        Assert.Throws<FormatException>(()=>PresetEffectCacheCodec.Decode(Json(),"V1.1.2"));
    }
}
