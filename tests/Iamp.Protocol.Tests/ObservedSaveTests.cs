using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class ObservedSaveTests
{
    private static JsonDocument Fixture()=>JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory,"fixtures/f15-observed-save.json")));

    private static (byte[] State,byte[] Detail,byte[] Request) Baseline()
    {
        using var fixture=Fixture();
        var frames=fixture.RootElement.GetProperty("frames");
        var request=Convert.FromHexString(frames[0].GetProperty("hex").GetString()!);
        var state=FrameCodec.Decode(Convert.FromHexString(frames[2].GetProperty("hex").GetString()!)).Payload;
        state[0]=0x12;
        // Independently captured stock preset detail, not output from the builder.
        var detail=FrameCodec.Decode(Convert.FromHexString(fixture.RootElement.GetProperty("returnedDetail").GetProperty("frameHex").GetString()!)).Payload;
        return (state,detail,request);
    }

    [Fact]
    public void PreservesObservedSaveSequenceFramingAcrossArbitraryNotificationSplits()
    {
        using var fixture=Fixture();
        foreach(var item in fixture.RootElement.GetProperty("frames").EnumerateArray()) {
            var bytes=Convert.FromHexString(item.GetProperty("hex").GetString()!);
            var payload=Convert.FromHexString(item.GetProperty("payloadHex").GetString()!);
            Assert.Equal(payload,FrameCodec.Decode(bytes).Payload);
            Assert.Equal(bytes,FrameCodec.Encode(payload));
            for(var split=1;split<bytes.Length;split++) {
                var decoder=new FrameStreamDecoder();
                Assert.Empty(decoder.Push(bytes.AsSpan(0,split)));
                Assert.Equal(payload,Assert.Single(decoder.Push(bytes.AsSpan(split))).Payload);
            }
        }
    }

    [Fact]
    public void ReconstructsOnlyTheIndependentUnchangedSaveEnvelope()
    {
        var (state,detail,request)=Baseline();
        var originalState=(byte[])state.Clone();var originalDetail=(byte[])detail.Clone();
        Assert.Equal(request,FrameCodec.Encode(ObservedPresetSaveCommand.CreateUnchanged("V1.1.9",state,detail)));
        Assert.Equal(originalState,state);Assert.Equal(originalDetail,detail);
    }

    [Fact]
    public void RejectsChangedContentDifferentDestinationAndUnobservedFirmware()
    {
        var (state,detail,_)=Baseline();
        Assert.Throws<NotSupportedException>(()=>ObservedPresetSaveCommand.CreateUnchanged("V1.1.2",state,detail));
        var otherSlot=(byte[])state.Clone();otherSlot[1]=5;
        Assert.Throws<NotSupportedException>(()=>ObservedPresetSaveCommand.CreateUnchanged("V1.1.9",otherSlot,detail));
        var changedState=(byte[])state.Clone();changedState[20]^=1;
        Assert.Throws<NotSupportedException>(()=>ObservedPresetSaveCommand.CreateUnchanged("V1.1.9",changedState,detail));
        var changedDetail=PresetDataParser.WithParameterValue(detail,1,2,26);
        Assert.Throws<NotSupportedException>(()=>ObservedPresetSaveCommand.CreateUnchanged("V1.1.9",state,changedDetail));
        var renamed=(byte[])detail.Clone();renamed[2]=(byte)'X';
        Assert.Throws<NotSupportedException>(()=>ObservedPresetSaveCommand.CreateUnchanged("V1.1.9",state,renamed));
        Assert.Throws<NotSupportedException>(()=>ObservedPresetSaveCommand.CreateUnchanged("V1.1.9",Array.Empty<byte>(),detail));
    }

    [Theory]
    [InlineData(25)]
    [InlineData(26)]
    public void ObservedThresholdSaveKeepsTheExactCapturedSlotAndNameEnvelope(int threshold)
    {
        var(state,detail,request)=Baseline();
        var changed=PresetDataParser.WithParameterValue(detail,1,2,(ushort)threshold);
        Assert.Equal(request,FrameCodec.Encode(ObservedPresetSaveCommand.CreateForObservedThreshold("V1.1.9",state,changed)));
        Assert.Equal((ushort)threshold,PresetDataParser.Parse(changed).Blocks[0].Values[2]);
    }

    [Theory]
    [InlineData(1,2,24)]
    [InlineData(1,2,27)]
    [InlineData(1,3,49)]
    [InlineData(2,2,38)]
    [InlineData(2,3,99)]
    public void ObservedThresholdSaveRejectsEveryUnprovenContentChange(int position,int parameter,int value)
    {
        var(state,detail,_)=Baseline();
        var changed=PresetDataParser.WithParameterValue(detail,(byte)position,parameter,(ushort)value);
        Assert.Throws<NotSupportedException>(()=>ObservedPresetSaveCommand.CreateForObservedThreshold("V1.1.9",state,changed));
    }
}
