using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public sealed class ModModelTests
{
    private static JsonDocument ReadCaptures() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "fixtures", "f15-mod-model-observations.json")));

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
        var block = new PresetEffectBlock(5, 6, values);
        var encoded = kind == "power" ? EffectBypassCommand.Create(block, target == 1) :
            ObservedParameterCommands.Create(block, ModParameters.For(block).Single(s => s.ValueIndex == index).Parameter, target);
        Assert.Equal(frameHex, Convert.ToHexString(FrameCodec.Encode(encoded)));
        Assert.Equal(original, block.Values);
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void EveryIntegerAndPhysicalPositionPreservesOtherWords(ushort model, ushort[] values)
    {
        Assert.Equal(model, values[1]);
        Assert.True(ModParameters.IsSupported(new(1, 6, values)));
        foreach (var spec in ModParameters.For(new(1, 6, values)))
        {
            var definition = ObservedParameterCommands.Definition(spec.Parameter);
            for (byte position = 1; position <= 20; position++)
                for (var raw = (int)definition.Minimum; raw <= definition.Maximum; raw++)
                {
                    var target = checked((ushort)raw);
                    var original = values.ToArray();
                    var payload = ObservedParameterCommands.Create(new(position, 6, values), spec.Parameter, target);
                    Assert.Equal(position, payload[1]);
                    Assert.Equal(6, payload[2]);
                    Assert.Equal(values.Length * 2, payload[3]);
                    for (var i = 0; i < values.Length; i++)
                        Assert.Equal(i == spec.ValueIndex ? target : values[i],
                            BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(4 + i * 2, 2)));
                    Assert.Equal(original, values);
                }
        }
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void GuardsRejectUnknownDisabledMalformedAndOutOfRangeStates(ushort model, ushort[] values)
    {
        var block = new PresetEffectBlock(1, 6, values);
        Assert.Equal(model, values[1]);
        var settings = ModParameters.For(block);
        Assert.NotEmpty(settings);
        Assert.Equal(settings[0].Profile, ObservedParameterCommands.Profile(block));
        var invalidValues = settings.Select(s => WithValue(block, s.ValueIndex, (ushort)(s.Maximum + 1)))
            .Concat(settings.Where(s => s.Minimum > 0).Select(s => WithValue(block, s.ValueIndex, (ushort)(s.Minimum - 1))))
            .Concat(Enumerable.Range(2, values.Length - 2).Where(i => settings.All(s => s.ValueIndex != i)).Select(i => WithValue(block, i, 1)));
        foreach (var parameter in Enum.GetValues<ObservedParameter>().Where(p =>
            ObservedParameterCommands.Definition(p).Position != settings[0].Profile))
            Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(block, parameter,
                ObservedParameterCommands.Definition(parameter).Minimum));
        foreach (var spec in settings)
        {
            var definition = ObservedParameterCommands.Definition(spec.Parameter);
            Assert.Throws<ArgumentOutOfRangeException>(() => ObservedParameterCommands.Create(block, spec.Parameter, (ushort)(definition.Maximum + 1)));
            if (definition.Minimum > 0)
                Assert.Throws<ArgumentOutOfRangeException>(() => ObservedParameterCommands.Create(block, spec.Parameter, (ushort)(definition.Minimum - 1)));
            foreach (var position in new byte[] { 0, 21 })
                Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(block with { Position = position }, spec.Parameter, 50));
            foreach (var invalid in new[] {
                block with { TypeCode = 9 }, block with { Values = values.Take(values.Length - 1).ToArray() },
                block with { Values = values.Append((ushort)0).ToArray() },
                block with { Values = Array.Empty<ushort>() }, block with { Values = new ushort[] { 1 } },
                WithValue(block, 0, 0), WithValue(block, 0, 2), WithValue(block, 1, ushort.MaxValue)
            }.Concat(invalidValues))
            {
                Assert.False(ModParameters.IsSupported(invalid));
                Assert.Throws<NotSupportedException>(() => ObservedParameterCommands.Create(invalid, spec.Parameter, 50));
            }
        }
        var disabled = WithValue(block, 0, 0);
        Assert.True(ModParameters.IsKnownLayout(disabled));
        Assert.True(EffectBypassCommand.TryGetEnabled(disabled, out var enabled));
        Assert.False(enabled);
        Assert.False(ModParameters.IsKnownLayout(WithValue(block, 1, ushort.MaxValue)));
        foreach (var state in new[] { false, true })
        {
            var expected = values.ToArray(); expected[0] = (ushort)(state ? 1 : 0);
            Assert.Equal(expected, EffectBypassCommand.ExpectedValues(block, state));
        }
    }

    [Fact]
    public void SupportedModelsExactlyMatchRetainedCaptures()
    {
        Assert.Equal(Enumerable.Range(0, 17).Select(v => (ushort)v), ModParameters.Models.Order());
        Assert.Equal(Models().Select(row => (ushort)row[0]).Order(), ModParameters.Models.Order());
        using var document = ReadCaptures();
        foreach (var model in document.RootElement.GetProperty("models").EnumerateArray())
        {
            var values = model.GetProperty("defaults").EnumerateArray().Select(v => v.GetUInt16()).ToArray();
            var settings = ModParameters.For(new(1, 6, values));
            var capturedSettings = model.GetProperty("parameterDefinitions").EnumerateArray().ToArray();
            Assert.Equal(model.GetProperty("settings").EnumerateArray().Select(s => s.GetString()), settings.Select(s => s.Name));
            Assert.Equal(model.GetProperty("units").EnumerateArray().Select(s => s.GetString()), settings.Select(s => s.Unit));
            Assert.Equal(capturedSettings.Length, settings.Count);
            Assert.Equal(settings.Count, settings.Select(s => s.ValueIndex).Distinct().Count());
            foreach (var (spec, captured) in settings.Zip(capturedSettings))
            {
                var definition = ObservedParameterCommands.Definition(spec.Parameter);
                Assert.Equal(captured.GetProperty("name").GetString(), spec.Name);
                Assert.Equal(captured.GetProperty("index").GetInt32(), spec.ValueIndex);
                Assert.Equal(captured.GetProperty("minimum").GetUInt16(), spec.Minimum);
                Assert.Equal(captured.GetProperty("maximum").GetUInt16(), spec.Maximum);
                Assert.Equal(captured.GetProperty("unit").GetString(), spec.Unit);
                Assert.True(spec.Profile >= 20);
                Assert.Equal(spec.Profile, definition.Position);
                Assert.Equal(spec.ValueIndex, definition.ValueIndex);
                Assert.Equal(spec.Minimum, definition.Minimum);
                Assert.Equal(spec.Maximum, definition.Maximum);
                var scale = captured.TryGetProperty("scale", out var capturedScale) ? capturedScale.GetDecimal() : 1m;
                var offset = captured.TryGetProperty("offset", out var capturedOffset) ? capturedOffset.GetDecimal() : 0m;
                var places = captured.TryGetProperty("decimalPlaces", out var capturedPlaces) ? capturedPlaces.GetInt32() : 0;
                Assert.NotEqual(0m, scale);
                for (var raw = (int)spec.Minimum; raw <= spec.Maximum; raw++)
                {
                    var wire = checked((ushort)raw);
                    var physical = raw * scale + offset;
                    var text = physical.ToString($"F{places}", CultureInfo.InvariantCulture);
                    Assert.Equal(physical, spec.DisplayValue(wire));
                    Assert.Equal(text, spec.Format(wire));
                    Assert.True(spec.TryParse(text, out var parsed));
                    Assert.Equal(wire, parsed);
                }
                var first = spec.Minimum * scale + offset;
                var last = spec.Maximum * scale + offset;
                foreach (var invalid in new[] { "", "bad", "NaN", "1e1",
                    (Math.Min(first, last) - Math.Abs(scale)).ToString(CultureInfo.InvariantCulture),
                    (Math.Max(first, last) + Math.Abs(scale)).ToString(CultureInfo.InvariantCulture),
                    (first + scale / 2m).ToString(CultureInfo.InvariantCulture) })
                    Assert.False(spec.TryParse(invalid, out _));
            }

        }
    }

    [Fact]
    public void RetainedEvidenceCoversBothEndpointsAndPowerStatesForEveryModel()
    {
        using var document = ReadCaptures();
        foreach (var model in document.RootElement.GetProperty("models").EnumerateArray())
        {
            var values = model.GetProperty("defaults").EnumerateArray().Select(v => v.GetUInt16()).ToArray();
            var cases = model.GetProperty("cases").EnumerateArray().ToArray();
            var definitions = model.GetProperty("parameterDefinitions").EnumerateArray().ToArray();
            Assert.True(values.Length >= 3);
            Assert.Equal((ushort)1, values[0]);
            Assert.Equal(model.GetProperty("model").GetUInt16(), values[1]);
            foreach (var definition in definitions)
            {
                var index = definition.GetProperty("index").GetInt32();
                Assert.InRange(index, 2, values.Length - 1);
                foreach (var target in new[] { definition.GetProperty("minimum").GetUInt16(), definition.GetProperty("maximum").GetUInt16() })
                    Assert.Contains(cases, sample => sample.GetProperty("kind").GetString() != "power" &&
                        sample.GetProperty("index").GetInt32() == index && sample.GetProperty("target").GetUInt16() == target);
            }
            foreach (var enabled in new ushort[] { 0, 1 })
                Assert.Contains(cases, sample => sample.GetProperty("kind").GetString() == "power" &&
                    sample.GetProperty("index").GetInt32() == 0 && sample.GetProperty("target").GetUInt16() == enabled);
        }
    }

    [Fact]
    public void ExplicitPhysicalPositionDistinguishesDuplicatesWhileImplicitResolutionRefusesThem()
    {
        var blocks = Models().Select(row => new PresetEffectBlock(6, 6, (ushort[])row[1])).ToArray();
        foreach (var first in blocks)
            foreach (var spec in ModParameters.For(first))
            {
                var second = first with { Position = 9 };
                Assert.Same(first, ObservedParameterCommands.FindSupportedBlock([first], spec.Parameter));
                Assert.Null(ObservedParameterCommands.FindSupportedBlock([first, second], spec.Parameter));
                foreach (var other in blocks.Where(b => b.Values[1] != first.Values[1]))
                {
                    var result = ObservedParameterCommands.FindSupportedBlock([first, other with { Position = 9 }], spec.Parameter);
                    if (ObservedParameterCommands.Profile(other) == spec.Profile) Assert.Null(result);
                    else Assert.Same(first, result);
                }
                Assert.Equal(6, ObservedParameterCommands.Create(first, spec.Parameter, spec.Minimum)[1]);
                Assert.Equal(9, ObservedParameterCommands.Create(second, spec.Parameter, spec.Minimum)[1]);
            }
    }

    private static PresetEffectBlock WithValue(PresetEffectBlock block, int index, ushort value)
    {
        var values = block.Values.ToArray(); values[index] = value;
        return block with { Values = values };
    }
}
