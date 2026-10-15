using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class EffectChainTests
{
    private const string Detail="220442726974697368204C65616420536F6C6F00000001040C01000200190032000000000002010A0100030027006400640003021201001F00640023002B0028002D004B0001000407100100010011001900F40100000000000005081401000100140018002300000000000000000000000600000700000800000900000A00000B00000C00000D00000E00000F00001000001100001200001300001400000000";
    [Fact]
    public void CapturedMobilePayloadsMatch()
    {
        var blocks=PresetDataParser.Parse(Convert.FromHexString(Detail)).Blocks;
        Assert.Equal(new byte[]{0x2D,151,4,1,3},EffectChainCommands.Move(blocks,1,3,151));
        Assert.Equal(new byte[]{0x2D,0,1,6,5,0},EffectChainCommands.Add(blocks,PresetEffectSearch.Catalog.Single(e=>e.TypeCode==5&&e.Model==0),0));
        Assert.Equal(new byte[]{0x2D,50,3,5},EffectChainCommands.Remove(blocks,5,50));
    }
    [Fact]
    public void EveryMoveAndInversePreserveCompleteBytesIncludingDisabledFlagsAndOpaqueFields()
    {
        var detail=Convert.FromHexString(Detail);detail[^1]=0xA7;detail[^2]=0x59;
        detail=PresetDataParser.WithParameterValue(detail,1,0,0);
        for(byte from=1;from<=5;from++)for(byte to=1;to<=5;to++) {
            var moved=EffectChainCommands.ExpectedMove(detail,from,to);
            Assert.Equal(detail,EffectChainCommands.ExpectedMove(moved,to,from));
            var decoded=PresetDataParser.Parse(moved);
            Assert.Equal((byte)4,decoded.WireIndex);Assert.Equal("British Lead Solo",decoded.Name);
            var original=PresetDataParser.Parse(detail).Blocks[from-1];
            Assert.Equal(original.TypeCode,decoded.Blocks[to-1].TypeCode);Assert.Equal(original.Values,decoded.Blocks[to-1].Values);
        }
    }
    [Fact]
    public void RemovalCompactsAndRetainsUnrelatedBlocks()
    {
        var removed=PresetDataParser.Parse(EffectChainCommands.ExpectedRemove(Convert.FromHexString(Detail),2));
        Assert.Equal(new byte[]{4,2,7,8},removed.Blocks.Take(4).Select(b=>b.TypeCode));
        Assert.All(removed.Blocks.Skip(4),b=>{Assert.Equal(0,b.TypeCode);Assert.Empty(b.Values);});
    }
    [Fact]
    public void InvalidPositionsHolesAndUnknownModelsAreRejectedButRepeatedCategoriesAreAllowed()
    {
        var blocks=PresetDataParser.Parse(Convert.FromHexString(Detail)).Blocks;
        Assert.Throws<ArgumentOutOfRangeException>(()=>EffectChainCommands.Move(blocks,0,1,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>EffectChainCommands.Remove(blocks,6,0));
        Assert.Equal(new byte[]{0x2D,0,1,6,4,0},EffectChainCommands.Add(blocks,PresetEffectSearch.Catalog.First(e=>e.TypeCode==4),0));
        Assert.Throws<NotSupportedException>(()=>EffectChainCommands.Add(blocks,new(5,999,"Filter","Unknown"),0));
        var bad=blocks.ToArray();bad[5]=bad[0] with {Position=6};
        Assert.Equal(6,EffectChainCommands.Active(bad).Count);
        var repeated=EffectChainCommands.ExpectedMove(AddRecord(Convert.FromHexString(Detail),"06040C010002000F00320000000000"),6,2);
        Assert.Equal(4,PresetDataParser.Parse(repeated).Blocks[1].TypeCode);
        bad=blocks.ToArray();bad[6]=new(7,5,new ushort[]{1,0,60,45,55,70});
        Assert.Throws<NotSupportedException>(()=>EffectChainCommands.Active(bad));
    }
    private static byte[] AddRecord(byte[] before,string record)
    {
        var offset=22;foreach(var b in PresetDataParser.Parse(before).Blocks.Take(5))offset+=3+b.Values.Count*2;
        return before[..offset].Concat(Convert.FromHexString(record)).Concat(before[(offset+3)..]).ToArray();
    }
    [Fact]
    public void AddingAndRemovingAmpAcceptOnlyCapturedBooleanAmpFlagSideEffects()
    {
        var before=Convert.FromHexString(Detail);
        var originalFlagOff=PresetDataParser.WithParameterValue(before,3,8,0);
        var added=AddRecord(originalFlagOff,"060212010000004B00320032003200320037000100");
        var amp=PresetEffectSearch.Catalog.Single(e=>e.TypeCode==2&&e.Model==0);
        Assert.True(EffectChainCommands.IsExpectedAddition(before,added,amp));
        Assert.True(EffectChainCommands.IsExpectedRemoval(added,before,6));
        Assert.False(EffectChainCommands.IsExpectedAddition(before,PresetDataParser.WithParameterValue(added,3,3,34),amp));
        Assert.False(EffectChainCommands.IsExpectedAddition(before,PresetDataParser.WithParameterValue(added,3,8,2),amp));
        var filter=AddRecord(originalFlagOff,"06050C010000003C002D0037004600");
        Assert.False(EffectChainCommands.IsExpectedAddition(before,filter,PresetEffectSearch.Catalog.Single(e=>e.TypeCode==5&&e.Model==0)));
        var moved=EffectChainCommands.ExpectedMove(added,6,2);
        var flagsChanged=PresetDataParser.WithParameterValue(PresetDataParser.WithParameterValue(moved,2,8,0),4,8,1);
        Assert.True(EffectChainCommands.IsExpectedMove(added,flagsChanged,6,2));
        Assert.False(EffectChainCommands.IsExpectedMove(added,PresetDataParser.WithParameterValue(flagsChanged,4,4,42),6,2));
    }
    [Theory]
    [InlineData(0)][InlineData(1)]
    public void AmpParameterUpdatesPreserveOpaqueAmpFlag(int flag)
    {
        var block=new PresetEffectBlock(3,2,new ushort[]{1,31,100,35,43,40,45,75,(ushort)flag});
        var encoded=ObservedParameterCommands.Create(block,ObservedParameter.AmpBass,34);
        Assert.Equal((byte)flag,encoded[^2]);Assert.Equal(0,encoded[^1]);
    }
    [Fact]
    public void IdenticalModelAdditionMoveAndRemovalPreserveOriginalModuleBytes()
    {
        var before=Convert.FromHexString(Detail);var added=AddRecord(before,"06040C010002000F00320000000000");
        var model=PresetEffectSearch.Catalog.Single(e=>e.TypeCode==4&&e.Model==2);
        Assert.True(EffectChainCommands.IsExpectedAddition(before,added,model));
        Assert.Equal(before,EffectChainCommands.ExpectedRemove(added,6));
        var moved=EffectChainCommands.ExpectedMove(added,6,2);
        Assert.Equal((ushort)25,PresetDataParser.Parse(moved).Blocks[0].Values[2]);
        Assert.Equal((ushort)15,PresetDataParser.Parse(moved).Blocks[1].Values[2]);
        Assert.Equal(added,EffectChainCommands.ExpectedMove(moved,2,6));
    }
    [Fact]
    public void AdditionRequiresUnchangedOriginalBytesCorrectModelAndEnabledNewBlock()
    {
        var before=Convert.FromHexString(Detail);var offset=22;
        foreach(var b in PresetDataParser.Parse(before).Blocks.Take(5))offset+=3+b.Values.Count*2;
        var record=Convert.FromHexString("06050C010000003C002D0037004600");
        var added=before[..offset].Concat(record).Concat(before[(offset+3)..]).ToArray();
        var effect=PresetEffectSearch.Catalog.Single(e=>e.TypeCode==5&&e.Model==0);
        Assert.True(EffectChainCommands.IsExpectedAddition(before,added,effect));
        var wrong=PresetDataParser.WithParameterValue(added,1,2,24);Assert.False(EffectChainCommands.IsExpectedAddition(before,wrong,effect));
        wrong=PresetDataParser.WithParameterValue(added,6,0,0);Assert.False(EffectChainCommands.IsExpectedAddition(before,wrong,effect));
        wrong=added.ToArray();wrong[^1]=1;Assert.False(EffectChainCommands.IsExpectedAddition(before,wrong,effect));
    }
}
