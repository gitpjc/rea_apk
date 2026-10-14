using Iamp.Protocol;
using Xunit;

namespace Iamp.Protocol.Tests;

public sealed class NoiseKillerCommandTests
{
    private static PresetEffectBlock Baseline => new(1, 4, new ushort[] { 1, 2, 25, 50, 0, 0 });

    [Theory]
    [InlineData(27, "AA5510002501040C010002001B00320000000000AC64")]
    [InlineData(28, "AA5510002501040C010002001C003200000000006B7C")]
    [InlineData(25, "AA5510002501040C01000200190032000000000023C2")]
    public void MatchesIndependentPhoneCapture(int threshold, string expected)
        => Assert.Equal(expected, Convert.ToHexString(FrameCodec.Encode(NoiseKillerCommand.Create(Baseline, (ushort)threshold))));

    [Fact]
    public void RejectsOtherModelsAndUnverifiedValues()
    {
        Assert.Throws<NotSupportedException>(() => NoiseKillerCommand.Create(Baseline with { TypeCode = 1 }, 26));
        Assert.Throws<NotSupportedException>(() => NoiseKillerCommand.Create(Baseline with { Position = 2 }, 26));
        Assert.Throws<NotSupportedException>(() => NoiseKillerCommand.Create(Baseline with { Values = new ushort[] { 1, 2, 25, 101, 0, 0 } }, 26));
        Assert.Throws<ArgumentOutOfRangeException>(() => NoiseKillerCommand.Create(Baseline, 101));
    }
}
