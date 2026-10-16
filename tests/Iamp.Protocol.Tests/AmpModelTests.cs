using System.Buffers.Binary;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class AmpModelTests
{
    public static IEnumerable<object[]> Captures()
    {
        using var document = ReadCaptures();
        foreach (var model in document.RootElement.GetProperty("models").EnumerateArray())
            foreach (var sample in model.GetProperty("cases").EnumerateArray())
                yield return [model.GetProperty("model").GetUInt16(), sample.GetProperty("kind").GetString()!,
                    sample.GetProperty("index").GetInt32(), sample.GetProperty("target").GetUInt16(),
                    sample.GetProperty("before").EnumerateArray().Select(v => v.GetUInt16()).ToArray(),
                    sample.GetProperty("frameHex").GetString()!];
    }

    public static IEnumerable<object[]> Models()
    {
        using var document = ReadCaptures();
        foreach (var model in document.RootElement.GetProperty("models").EnumerateArray())
            yield return [model.GetProperty("model").GetUInt16(),
                model.GetProperty("defaults").EnumerateArray().Select(v => v.GetUInt16()).ToArray()];
    }

    private static JsonDocument ReadCaptures() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "fixtures", "f15-amp-model-observations.json")));

    [Theory]
    [MemberData(nameof(Captures))]
    public void EncoderMatchesIndependentMobileParameterAndPowerFrames(
        ushort model, string kind, int index, ushort target, ushort[] values, string frameHex)
    {
        Assert.Equal(model, values[1]);
        var original = values.ToArray();
        var block = new PresetEffectBlock(3, 2, values);
        var encoded = kind == "power" ? EffectBypassCommand.Create(block, target == 1) :
            ObservedParameterCommands.Create(block, AmpParameters.Settings.Single(s => s.ValueIndex == index).Parameter, target);
        Assert.Equal(frameHex, Convert.ToHexString(FrameCodec.Encode(encoded)));
        Assert.Equal(original, block.Values);
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void EveryIntegerAndPhysicalPositionPreservesOtherWords(ushort model, ushort[] values)
    {
        Assert.Equal(model, values[1]);
        Assert.True(AmpParameters.IsSupported(new(1, 2, values)));
        foreach (var spec in AmpParameters.Settings)
            for (byte position = 1; position <= 20; position++)
                for (ushort target = 0; target <= 100; target++)
                {
                    var payload = ObservedParameterCommands.Create(new(position, 2, values), spec.Parameter, target);
                    Assert.Equal(position, payload[1]);
                    Assert.Equal(2, payload[2]);
                    Assert.Equal(18, payload[3]);
                    for (var i = 0; i < values.Length; i++)
                        Assert.Equal(i == spec.ValueIndex ? target : values[i],
                            BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2)));
                }
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void GuardsRejectUnknownDisabledMalformedAndOutOfRangeStates(ushort model, ushort[] values)
    {
        var block = new PresetEffectBlock(1, 2, values);
        Assert.Equal(model, values[1]);
        Assert.Equal(3, ObservedParameterCommands.Profile(block));
        foreach (var spec in AmpParameters.Settings)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ObservedParameterCommands.Create(block, spec.Parameter, 101));
            foreach (var position in new byte[] { 0, 21 })
                Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(block with { Position = position }, spec.Parameter, 50));
            foreach (var invalid in new[] {
                block with { TypeCode = 9 }, block with { Values = values.Take(8).ToArray() },
                block with { Values = values.Append((ushort)0).ToArray() },
                WithValue(block, 0, 0), WithValue(block, 0, 2), WithValue(block, 1, ushort.MaxValue),
                WithValue(block, 8, 2) }.Concat(Enumerable.Range(2, 6).Select(i => WithValue(block, i, 101))))
            {
                Assert.False(AmpParameters.IsSupported(invalid));
                Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(invalid, spec.Parameter, 50));
            }
        }
        Assert.True(AmpParameters.IsKnownLayout(WithValue(block, 0, 0)));
        Assert.False(AmpParameters.IsKnownLayout(WithValue(block, 1, ushort.MaxValue)));
        foreach (ushort flag in new ushort[] { 0, 1 })
        {
            var flagged = WithValue(block, 8, flag);
            Assert.True(AmpParameters.IsSupported(flagged));
            var payload = ObservedParameterCommands.Create(flagged, ObservedParameter.AmpGain, 50);
            Assert.Equal(flag, BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(20, 2)));
        }
    }

    [Fact]
    public void SupportedModelsExactlyMatchRetainedCaptures()
    {
        Assert.Equal(Models().Select(row => (ushort)row[0]).Order(), AmpParameters.Models.Order());
        Assert.Equal(new[] { "Gain", "Bass", "Mid", "Treble", "Presence", "Master" }, AmpParameters.Settings.Select(s => s.Name));
        Assert.Equal(Enumerable.Range(2, 6), AmpParameters.Settings.Select(s => s.ValueIndex));
    }

    [Fact]
    public void ExplicitPhysicalPositionDistinguishesDuplicatesWhileImplicitResolutionRefusesThem()
    {
        var first = new PresetEffectBlock(6, 2, new ushort[] { 1, 31, 50, 40, 30, 20, 10, 60, 1 });
        var second = first with { Position = 9 };
        Assert.Same(first, ObservedParameterCommands.FindSupportedBlock([first], ObservedParameter.AmpGain));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock([first, second], ObservedParameter.AmpGain));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock([first, WithValue(second, 1, 0)], ObservedParameter.AmpGain));
        Assert.Equal(6, ObservedParameterCommands.Create(first, ObservedParameter.AmpGain, 45)[1]);
        Assert.Equal(9, ObservedParameterCommands.Create(second, ObservedParameter.AmpGain, 45)[1]);
    }

    private static PresetEffectBlock WithValue(PresetEffectBlock block, int index, ushort value)
    {
        var values = block.Values.ToArray();
        values[index] = value;
        return block with { Values = values };
    }
}
