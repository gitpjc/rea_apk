using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class PresetEffectSearchTests
{
    [Theory]
    [InlineData(0)][InlineData(1)]
    public void DisabledAndEnabledEffectsBothMatch(int enabled)
    {
        var blocks=new[]{new PresetEffectBlock(7,4,new ushort[]{(ushort)enabled,2,25,50,0,0})};
        Assert.True(PresetEffectSearch.Contains(blocks,4,2));
        Assert.False(PresetEffectSearch.Contains(blocks,4,1));
        Assert.False(PresetEffectSearch.Contains(blocks,7,2));
    }

    [Fact]
    public void EmptyAndTruncatedBlocksDoNotMatchModelZero()
    {
        Assert.False(PresetEffectSearch.Contains(new[]{new PresetEffectBlock(1,0,new ushort[]{0,0})},0,0));
        Assert.False(PresetEffectSearch.Contains(new[]{new PresetEffectBlock(1,4,new ushort[]{0})},4,0));
        Assert.False(PresetEffectSearch.Contains(Array.Empty<PresetEffectBlock>(),4,0));
    }

    [Theory]
    [InlineData(4,"British Lead Solo"," 0d ",true)]
    [InlineData(4,"British Lead Solo","LEAD",true)]
    [InlineData(80,"Last","19D",true)]
    [InlineData(80,"Last","80",true)]
    [InlineData(4,"British Lead Solo","clean",false)]
    public void TextSearchPreservesNameBankAndIndexMatching(int index,string name,string term,bool matches)
        =>Assert.Equal(matches,PresetEffectSearch.MatchesText(index,name,term));

    [Fact]
    public void CatalogCoversEveryCapturedBuiltInModelWithoutDuplicateKeys()
    {
        var entries=PresetEffectSearch.Catalog;
        Assert.Equal(124,entries.Count);Assert.Equal(124,entries.Select(e=>(e.TypeCode,e.Model)).Distinct().Count());
        foreach(var (category,count,type) in new[]{("Dyna",4,4),("Filter",3,5),("OD",22,1),("Amp",55,2),("EQ",2,9),("Mod",17,6),("Delay",12,7),("Reverb",9,8)}) {
            var group=entries.Where(e=>e.Category==category).ToArray();Assert.Equal(count,group.Length);
            Assert.All(group,e=>Assert.Equal((byte)type,e.TypeCode));Assert.Equal(Enumerable.Range(0,count),group.Select(e=>(int)e.Model));
        }
        Assert.Equal("Noise Killer",entries.Single(e=>e.TypeCode==4&&e.Model==2).Name);
        Assert.Equal("PLX 100",entries.Single(e=>e.TypeCode==2&&e.Model==31).Name);
        Assert.Equal("Touch Wah",entries.Single(e=>e.TypeCode==5&&e.Model==0).Name);
    }

    [Fact]
    public void IndexRetainsSeparatePresetsAndRejectsDuplicateAndInvalidSlots()
    {
        var off=new[]{new PresetEffectBlock(1,4,new ushort[]{0,2,25,50,0,0})};
        var on=new[]{new PresetEffectBlock(1,4,new ushort[]{1,0,30,50,0,0})};
        var p=new[]{new DecodedPreset(1,"Clean",off,Array.Empty<byte>()),new DecodedPreset(4,"Lead",on,Array.Empty<byte>())};
        var index=PresetEffectSearch.CreateIndex(p);
        Assert.True(PresetEffectSearch.Contains(index[1],4,2));Assert.False(PresetEffectSearch.Contains(index[4],4,2));
        Assert.Throws<FormatException>(()=>PresetEffectSearch.CreateIndex(new[]{p[0],p[0]}));
        Assert.Throws<FormatException>(()=>PresetEffectSearch.CreateIndex(new[]{p[0] with {WireIndex=0}}));
        Assert.Throws<FormatException>(()=>PresetEffectSearch.CreateIndex(new[]{p[0] with {WireIndex=81}}));
    }
}
