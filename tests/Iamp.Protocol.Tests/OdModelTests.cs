using System.Buffers.Binary;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class OdModelTests
{
    private static JsonDocument ReadCaptures() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "fixtures", "f15-od-model-observations.json")));

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

    [Theory]
    [MemberData(nameof(Captures))]
    public void EncoderMatchesIndependentMobileParameterAndPowerFrames(
        ushort model, string kind, int index, ushort target, ushort[] values, string frameHex)
    {
        Assert.Equal(model, values[1]);
        var original = values.ToArray();
        var block = new PresetEffectBlock(2, 1, values);
        var encoded = kind == "power" ? EffectBypassCommand.Create(block, target == 1) :
            ObservedParameterCommands.Create(block, OdParameters.Settings.Single(s => s.ValueIndex == index).Parameter, target);
        Assert.Equal(frameHex, Convert.ToHexString(FrameCodec.Encode(encoded)));
        Assert.Equal(original, block.Values);
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void EveryIntegerAndPhysicalPositionPreservesOtherWords(ushort model, ushort[] values)
    {
        Assert.Equal(model, values[1]);
        Assert.True(OdParameters.IsSupported(new(1, 1, values)));
        foreach (var spec in OdParameters.Settings)
            for (byte position = 1; position <= 20; position++)
                for (ushort target = 0; target <= 100; target++)
                {
                    var original = values.ToArray();
                    var payload = ObservedParameterCommands.Create(new(position, 1, values), spec.Parameter, target);
                    Assert.Equal(position, payload[1]);
                    Assert.Equal(1, payload[2]);
                    Assert.Equal(10, payload[3]);
                    for (var i = 0; i < values.Length; i++)
                        Assert.Equal(i == spec.ValueIndex ? target : values[i],
                            BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2)));
                    Assert.Equal(original, values);
                }
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void GuardsRejectUnknownDisabledMalformedAndOutOfRangeStates(ushort model, ushort[] values)
    {
        var block = new PresetEffectBlock(1, 1, values);
        Assert.Equal(model, values[1]);
        Assert.Equal(2, ObservedParameterCommands.Profile(block));
        foreach (var spec in OdParameters.Settings)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ObservedParameterCommands.Create(block, spec.Parameter, 101));
            foreach (var position in new byte[] { 0, 21 })
                Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(block with { Position = position }, spec.Parameter, 50));
            foreach (var invalid in new[] {
                block with { TypeCode = 9 }, block with { Values = values.Take(4).ToArray() },
                block with { Values = values.Append((ushort)0).ToArray() },
                block with { Values = Array.Empty<ushort>() }, block with { Values = new ushort[] { 1 } },
                WithValue(block, 0, 0), WithValue(block, 0, 2), WithValue(block, 1, ushort.MaxValue)
            }.Concat(Enumerable.Range(2, 3).Select(i => WithValue(block, i, 101))))
            {
                Assert.False(OdParameters.IsSupported(invalid));
                Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(invalid, spec.Parameter, 50));
            }
        }
        var disabled = WithValue(block, 0, 0);
        Assert.True(OdParameters.IsKnownLayout(disabled));
        Assert.True(EffectBypassCommand.TryGetEnabled(disabled, out var enabled));
        Assert.False(enabled);
        Assert.False(OdParameters.IsKnownLayout(WithValue(block, 1, ushort.MaxValue)));
        foreach (var state in new[] { false, true })
        {
            var expected = values.ToArray(); expected[0] = (ushort)(state ? 1 : 0);
            Assert.Equal(expected, EffectBypassCommand.ExpectedValues(block, state));
        }
    }

    [Fact]
    public void SupportedModelsExactlyMatchRetainedCaptures()
    {
        Assert.Equal(Models().Select(row => (ushort)row[0]).Order(), OdParameters.Models.Order());
        Assert.Equal(new[] { "Gain", "Tone", "Vol" }, OdParameters.Settings.Select(s => s.Name));
        Assert.Equal(Enumerable.Range(2, 3), OdParameters.Settings.Select(s => s.ValueIndex));
    }

    [Fact]
    public void ExplicitPhysicalPositionDistinguishesDuplicatesWhileImplicitResolutionRefusesThem()
    {
        var first = new PresetEffectBlock(6, 1, new ushort[] { 1, 3, 50, 40, 30 });
        var second = first with { Position = 9 };
        Assert.Same(first, ObservedParameterCommands.FindSupportedBlock([first], ObservedParameter.OdGain));
        Assert.Null(ObservedParameterCommands.FindSupportedBlock([first, second], ObservedParameter.OdGain));
        var other = OdParameters.Models.First(m => m != 3);
        Assert.Null(ObservedParameterCommands.FindSupportedBlock([first, WithValue(second, 1, other)], ObservedParameter.OdGain));
        Assert.Equal(6, ObservedParameterCommands.Create(first, ObservedParameter.OdGain, 45)[1]);
        Assert.Equal(9, ObservedParameterCommands.Create(second, ObservedParameter.OdGain, 45)[1]);
    }

    private static PresetEffectBlock WithValue(PresetEffectBlock block, int index, ushort value)
    {
        var values = block.Values.ToArray(); values[index] = value;
        return block with { Values = values };
    }
}
