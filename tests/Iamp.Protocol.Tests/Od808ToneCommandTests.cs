using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class Od808ToneCommandTests
{
    private static PresetEffectBlock Baseline => new(2, 1, new ushort[] { 1, 3, 39, 100, 100 });

    [Theory]
    [InlineData(97, "AA550E002502010A010003002700610064000CB5")]
    [InlineData(99, "AA550E002502010A01000300270063006400E1DD")]
    [InlineData(100, "AA550E002502010A01000300270064006400B0F0")]
    public void MatchesCapturedPhoneFrames(int tone, string frameHex)
        => Assert.Equal(frameHex, Convert.ToHexString(FrameCodec.Encode(Od808ToneCommand.Create(Baseline, (ushort)tone))));

    [Fact]
    public void RejectsOtherLayoutsAndUnverifiedValues()
    {
        Assert.Throws<NotSupportedException>(() => Od808ToneCommand.Create(Baseline with { Position = 21 }, 99));
        Assert.Throws<NotSupportedException>(() => Od808ToneCommand.Create(Baseline with { Values = new ushort[] { 1, 3, 101, 100, 100 } }, 99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Od808ToneCommand.Create(Baseline, 101));
    }
}
