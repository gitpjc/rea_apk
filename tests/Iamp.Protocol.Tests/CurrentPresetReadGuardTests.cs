using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class CurrentPresetReadGuardTests
{
    private static (byte[] State, byte[] Detail) Captured()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "fixtures/f15-observed-preset.json")));
        byte[] Payload(string key) => FrameCodec.Decode(Convert.FromHexString(
            fixture.RootElement.GetProperty(key).GetProperty("frameHex").GetString()!)).Payload;
        return (Payload("state"), Payload("detail"));
    }

    [Fact]
    public void AcceptsIndependentCaptureWithoutAlteringOpaqueBytes()
    {
        var (state, detail) = Captured();
        var originalState = state.ToArray();
        var originalDetail = detail.ToArray();
        var preset = CurrentPresetReadGuard.Validate(state, detail, state.ToArray());
        Assert.Equal((byte)16, preset.WireIndex);
        Assert.Equal("Cali MK3 Lead", preset.Name);
        Assert.Equal(originalState, state);
        Assert.Equal(originalDetail, detail);
    }

    [Fact]
    public void RejectsChangeInEveryStateFieldEvenWithTheSamePresetIndex()
    {
        var (state, detail) = Captured();
        for (var offset = 2; offset < state.Length; offset++)
        {
            var after = state.ToArray();
            after[offset] ^= 1;
            Assert.Throws<IOException>(() => CurrentPresetReadGuard.Validate(state, detail, after));
        }
    }

    [Fact]
    public void RejectsPresetSwitchAndMismatchedDetail()
    {
        var (state, detail) = Captured();
        var switched = state.ToArray(); switched[1] = 17;
        Assert.Throws<IOException>(() => CurrentPresetReadGuard.Validate(state, detail, switched));
        var otherDetail = detail.ToArray(); otherDetail[1] = 17;
        Assert.Throws<IOException>(() => CurrentPresetReadGuard.Validate(state, otherDetail, state));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(91)]
    [InlineData(93)]
    public void RejectsUnobservedStateLengthsAtEitherEnd(int length)
    {
        var (state, detail) = Captured();
        var malformed = new byte[length];
        state.AsSpan(0, Math.Min(length, state.Length)).CopyTo(malformed);
        Assert.Throws<FormatException>(() => CurrentPresetReadGuard.Validate(malformed, detail, state));
        Assert.Throws<FormatException>(() => CurrentPresetReadGuard.Validate(state, detail, malformed));
    }

    [Theory]
    [InlineData(0x13, 16)]
    [InlineData(0x12, 0)]
    [InlineData(0x12, 81)]
    public void RejectsInvalidStateDiscriminatorOrIndex(byte discriminator, byte index)
    {
        var (state, _) = Captured();
        state[0] = discriminator; state[1] = index;
        Assert.Throws<FormatException>(() => CurrentPresetReadGuard.ValidateState(state));
    }

    [Fact]
    public void UnknownStableStateFieldsAndEffectTypesRemainOpaque()
    {
        var (state, detail) = Captured();
        state[^1] = 0x93;
        detail[23] = 0xF0;
        var preset = CurrentPresetReadGuard.Validate(state, detail, state.ToArray());
        Assert.Equal((byte)0xF0, preset.Blocks[0].TypeCode);
    }
}
