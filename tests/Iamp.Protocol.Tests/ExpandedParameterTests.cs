using System.Buffers.Binary;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class ExpandedParameterTests
{
    [Theory]
    [InlineData(ObservedParameter.NoiseLevel, 47, "AA5510002501040C0100020019002F00000000007705")]
    [InlineData(ObservedParameter.NoiseLevel, 49, "AA5510002501040C010002001900310000000000ED22")]
    [InlineData(ObservedParameter.NoiseLevel, 50, "AA5510002501040C01000200190032000000000023C2")]
    [InlineData(ObservedParameter.OdGain, 36, "AA550E002502010A010003002400640064007E10")]
    [InlineData(ObservedParameter.OdGain, 38, "AA550E002502010A01000300260064006400F550")]
    [InlineData(ObservedParameter.OdGain, 39, "AA550E002502010A01000300270064006400B0F0")]
    [InlineData(ObservedParameter.OdVolume, 97, "AA550E002502010A010003002700640061004F05")]
    [InlineData(ObservedParameter.OdVolume, 99, "AA550E002502010A010003002700640063002967")]
    [InlineData(ObservedParameter.OdVolume, 100, "AA550E002502010A01000300270064006400B0F0")]
    public void MatchesIndependentPhoneFrames(ObservedParameter parameter, int value, string frame)
    {
        var block = parameter == ObservedParameter.NoiseLevel
            ? new PresetEffectBlock(1, 4, new ushort[] { 1, 2, 25, 50, 0, 0 })
            : new PresetEffectBlock(2, 1, new ushort[] { 1, 3, 39, 100, 100 });
        Assert.Equal(frame, Convert.ToHexString(FrameCodec.Encode(ObservedParameterCommands.Create(block, parameter, (ushort)value))));
    }

    [Fact]
    public void PreservesOtherPendingDeviceValuesInCombinedEdits()
    {
        var block = new PresetEffectBlock(2, 1, new ushort[] { 1, 3, 37, 98, 99 });
        var request = ObservedParameterCommands.Create(block, ObservedParameter.OdVolume, 97);
        Assert.Equal((ushort)37, BinaryPrimitives.ReadUInt16LittleEndian(request.AsSpan(8, 2)));
        Assert.Equal((ushort)98, BinaryPrimitives.ReadUInt16LittleEndian(request.AsSpan(10, 2)));
        Assert.Equal((ushort)97, BinaryPrimitives.ReadUInt16LittleEndian(request.AsSpan(12, 2)));
        var noise = new PresetEffectBlock(1, 4, new ushort[] { 1, 2, 27, 48, 0, 0 });
        request = ObservedParameterCommands.Create(noise, ObservedParameter.NoiseThreshold, 26);
        Assert.Equal((ushort)48, BinaryPrimitives.ReadUInt16LittleEndian(request.AsSpan(10, 2)));
    }

    [Fact]
    public void RejectsUnknownFlagsLayoutsAndValues()
    {
        var block = new PresetEffectBlock(2, 1, new ushort[] { 0, 3, 39, 100, 100 });
        Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(block, ObservedParameter.OdGain, 37));
        block = block with { Values = new ushort[] { 1, 3, 39, 100, 100 } };
        Assert.Throws<ArgumentOutOfRangeException>(() => ObservedParameterCommands.Create(block, ObservedParameter.OdGain, 40));
        Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(block, ObservedParameter.NoiseLevel, 48));
        Assert.Throws<ArgumentOutOfRangeException>(() => ObservedParameterCommands.Definition((ObservedParameter)255));
    }
}
