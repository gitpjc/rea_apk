using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class PresetAddressTests
{
    [Theory]
    [InlineData(1,"0A")][InlineData(2,"0B")][InlineData(3,"0C")][InlineData(4,"0D")]
    [InlineData(5,"1A")][InlineData(9,"2A")][InlineData(80,"19D")]
    public void LabelsMatchTheMobileBankConvention(int index,string label)
        => Assert.Equal(label,PresetAddress.FromWireIndex(index).Label);

    [Fact]
    public void AllEightyLabelsAreUniqueAndRecallPreservesEveryOtherByte()
    {
        Assert.Equal(80,Enumerable.Range(1,80).Select(i=>PresetAddress.FromWireIndex(i).Label).Distinct().Count());
        var state=new byte[92];state[0]=0x12;state[1]=4;
        for(var i=2;i<92;i++)state[i]=(byte)i;
        for(byte target=1;target<=80;target++) {
            var request=PresetSelectionCommand.CreateForAnySlot(state,target);
            Assert.Equal((byte)0x13,request[0]);Assert.Equal(target,request[1]);Assert.Equal(state[2..],request[2..]);
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>PresetSelectionCommand.CreateForAnySlot(state,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PresetSelectionCommand.CreateForAnySlot(state,81));
        Assert.Throws<FormatException>(()=>PresetSelectionCommand.CreateForAnySlot(state[..91],1));
    }
}
