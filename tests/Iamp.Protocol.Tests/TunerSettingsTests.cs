using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class TunerSettingsTests
{
    private static byte[] Baseline()
    {
        var state = Enumerable.Range(0, 92).Select(i => (byte)(i * 37)).ToArray();
        state[0] = 0x12; state[1] = 4; state[44] = 10; state[45] = 1;
        return state;
    }

    [Fact]
    public void EveryCapturedReferenceAndOutputAtAllSlotsParsesAndPreservesUnrelatedBytes()
    {
        for (var slot = 1; slot <= 80; slot++)
        foreach (var mode in new[] { TunerOutputMode.Mute, TunerOutputMode.Bypass })
        {
            var source = Baseline(); source[1] = (byte)slot; source[45] = (byte)mode;
            var original = source.ToArray();
            for (var reference = 430; reference <= 445; reference++)
            {
                var expected = source.ToArray(); expected[0] = 0x13; expected[44] = (byte)(reference - 430);
                var command = TunerCommands.CreateReference(source, reference);
                Assert.Equal(expected, command);
                command[0] = 0x12;
                var parsed = TunerSettings.Parse(command);
                Assert.Equal(reference, parsed.ReferenceHertz);
                Assert.Equal(mode, parsed.OutputMode);
            }
            foreach (var targetMode in new[] { TunerOutputMode.Mute, TunerOutputMode.Bypass })
            {
                var expected = source.ToArray(); expected[0] = 0x13; expected[45] = (byte)targetMode;
                Assert.Equal(expected, TunerCommands.CreateOutputMode(source, targetMode));
            }
            Assert.Equal(original, source);
        }
    }

    [Fact]
    public void EveryUnsupportedReferenceByteOrOutputByteRejectsParsingAndBothSettingsUpdates()
    {
        for (var value = 0; value <= 255; value++)
        {
            if (value > 15)
            {
                var reference = Baseline(); reference[44] = (byte)value;
                AssertInvalidState(reference);
            }
            if (value is not (1 or 2))
            {
                var output = Baseline(); output[45] = (byte)value;
                AssertInvalidState(output);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(44)]
    [InlineData(45)]
    [InlineData(91)]
    [InlineData(93)]
    public void RejectsIncompleteOrExtendedState(int length) => AssertInvalidState(new byte[length]);

    [Theory]
    [InlineData(0x00, 4)]
    [InlineData(0x13, 4)]
    [InlineData(0x12, 0)]
    [InlineData(0x12, 81)]
    [InlineData(0x12, 255)]
    public void RejectsOtherOpcodesAndInvalidSlots(byte opcode, byte slot)
    {
        var source = Baseline(); source[0] = opcode; source[1] = slot;
        AssertInvalidState(source);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(429)]
    [InlineData(446)]
    [InlineData(int.MaxValue)]
    public void RejectsUnsupportedTargetReference(int reference) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TunerCommands.CreateReference(Baseline(), reference));

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(257)]
    public void RejectsUnsupportedTargetMode(int mode) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TunerCommands.CreateOutputMode(Baseline(), (TunerOutputMode)mode));

    [Fact]
    public void ModeCommandsAreExactCapturedPayloadsAndReturnIndependentArrays()
    {
        Assert.Equal(new byte[] { 0x41, 0x02 }, TunerCommands.CreateEnter());
        Assert.Equal(new byte[] { 0x41, 0x00 }, TunerCommands.CreateExit());
        var entry = TunerCommands.CreateEnter(); entry[1] = 1;
        var exit = TunerCommands.CreateExit(); exit[1] = 1;
        Assert.Equal(new byte[] { 0x41, 0x02 }, TunerCommands.CreateEnter());
        Assert.Equal(new byte[] { 0x41, 0x00 }, TunerCommands.CreateExit());
    }

    private static void AssertInvalidState(byte[] state)
    {
        Assert.Throws<NotSupportedException>(() => TunerSettings.Parse(state));
        Assert.Throws<NotSupportedException>(() => TunerCommands.CreateReference(state, 440));
        Assert.Throws<NotSupportedException>(() => TunerCommands.CreateOutputMode(state, TunerOutputMode.Mute));
    }
}
