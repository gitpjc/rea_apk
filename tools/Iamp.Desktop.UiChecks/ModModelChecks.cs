using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class ModModelChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static T Property<T>(object owner, string name) => (T)owner.GetType().GetProperty(name, Flags)!.GetValue(owner)!;
    private static void Set(object owner, string name, object? value) => owner.GetType().GetField(name, Flags)!.SetValue(owner, value);
    private static object? Call(object owner, string name, params object?[] args) => owner.GetType().GetMethod(name, Flags)!.Invoke(owner, args);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed record Editor(int Index, Slider Slider, TextBox Input, Button Apply);
    private sealed record Mapping(decimal Scale, decimal Offset, int DecimalPlaces)
    {
        internal decimal Display(ushort raw) => raw * Scale + Offset;
        internal string Format(ushort raw) => Display(raw).ToString($"F{DecimalPlaces}", CultureInfo.InvariantCulture);
    }
    private static readonly IReadOnlyDictionary<ushort, ushort[]> Defaults = ReadDefaults();
    private static readonly IReadOnlyDictionary<(ushort Model, int Index), Mapping> Mappings = ReadMappings();
    private static IReadOnlyDictionary<(ushort Model, int Index), Mapping> ReadMappings()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "f15-mod-model-observations.json")));
        return document.RootElement.GetProperty("models").EnumerateArray().SelectMany(model =>
            model.GetProperty("parameterDefinitions").EnumerateArray().Select(setting => (
                Key: (model.GetProperty("model").GetUInt16(), setting.GetProperty("index").GetInt32()),
                Value: new Mapping(setting.TryGetProperty("scale", out var scale) ? scale.GetDecimal() : 1m,
                    setting.TryGetProperty("offset", out var offset) ? offset.GetDecimal() : 0m,
                    setting.TryGetProperty("decimalPlaces", out var places) ? places.GetInt32() : 0))))
            .ToDictionary(item => item.Key, item => item.Value);
    }
    private static IReadOnlyDictionary<ushort, ushort[]> ReadDefaults()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "f15-mod-model-observations.json")));
        return document.RootElement.GetProperty("models").EnumerateArray().ToDictionary(m => m.GetProperty("model").GetUInt16(),
            m => m.GetProperty("defaults").EnumerateArray().Select(v => v.GetUInt16()).ToArray());
    }
    private static Editor[] Editors(MainWindow window, PresetEffectBlock block)
    {
        var specs = ModParameters.For(block);
        var shared = Property<IEnumerable>(window, "AdditionalEditors").Cast<object>()
            .Where(e => specs.Any(s => s.Profile == Property<byte>(e, "ProfilePosition") && s.ValueIndex == Property<int>(e, "ParameterIndex")))
            .Select(e => new Editor(Property<int>(e, "ParameterIndex"), Property<Slider>(e, "Slider"),
                Property<TextBox>(e, "Input"), Property<Button>(e, "Apply")));
        var editors = shared.ToArray();
        return specs.Select(s => editors.Single(e => e.Index == s.ValueIndex)).ToArray();
    }
    private static CurrentPresetSnapshot Snapshot(ushort model, ushort secondModel)
    {
        var blocks = Enumerable.Range(1, 20).Select(i => new PresetEffectBlock((byte)i, 0, Array.Empty<ushort>())).ToArray();
        ushort[] Values(ushort id) => Defaults.TryGetValue(id, out var values) ? values.ToArray() : new ushort[] { 1, id };
        blocks[0] = new(1, 6, Values(model));
        blocks[1] = new(2, 6, Values(secondModel));
        return new(4, "Offline Mod", "00", "00", blocks);
    }
    private static void Load(MainWindow window, CurrentPresetSnapshot snapshot)
    {
        Set(window, "currentSnapshot", snapshot); Set(window, "snapshotAddress", (ulong)1);
        Set(window, "snapshotFirmware", "V1.1.9"); Set(window, "editSnapshotValid", true); Set(window, "operation", null);
        Call(window, "SetEffects", (object)snapshot.Blocks);
    }
    private static void Select(MainWindow window, byte position) => ((Button)Field<WrapPanel>(window, "effectChain")
        .Children[position - 1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void NoHardware(MainWindow window) => Require(Field<F15ReadSession?>(window, "connectedSession") is null &&
        Field<CancellationTokenSource?>(window, "operation") is null, "Offline Mod checks started device work.");
    private static void Blocked(Editor[] editors, string message) => Require(editors.All(e => !e.Slider.IsEnabled && !e.Apply.IsEnabled), message);

    internal static void Run()
    {
        var window = new MainWindow();
        try
        {
            foreach (var model in ModParameters.Models)
            {
                var snapshot = Snapshot(model, model); var values = snapshot.Blocks[0].Values;
                var specs = ModParameters.For(snapshot.Blocks[0]);
                var editors = Editors(window, snapshot.Blocks[0]);
                Require(editors.Select(e => e.Index).SequenceEqual(specs.Select(s => s.ValueIndex)), "Mod model editor indices differ from observed settings.");
                Load(window, snapshot);
                Require(Field<StackPanel>(window, "effectDetails").Children.OfType<Slider>().SequenceEqual(editors.Select(e => e.Slider)),
                    "Mod sliders do not follow the observed mobile model order.");
                Require(Field<Button>(window, "effectPowerButton").IsEnabled, "Verified Mod power control unavailable.");
                var root = (FrameworkElement)window.Content;
                var scroll = Field<ScrollViewer>(window, "libraryScroll");
                foreach (var size in new[] { new Size(1366, 768), new Size(911, 512), new Size(683, 384), new Size(620, 320), new Size(1920, 1080) })
                {
                    for (var pass = 0; pass < 3; pass++) { root.Measure(size); root.Arrange(new Rect(size)); root.UpdateLayout(); }
                    foreach (var editor in editors)
                    {
                        var point = editor.Slider.TransformToAncestor(scroll).Transform(new Point());
                        Require(editor.Slider.ActualWidth > 0 && point.X >= 0 && point.X + editor.Slider.ActualWidth <= scroll.ViewportWidth + 1,
                            "Mod slider exceeds viewport.");
                    }
                }
                foreach (var editor in editors)
                {
                    var definition = ObservedParameterCommands.Definition(specs.Single(s => s.ValueIndex == editor.Index).Parameter);
                    var mapping = Mappings[(model, editor.Index)];
                    var longest = Enumerable.Range(definition.Minimum, definition.Maximum - definition.Minimum + 1)
                        .Max(raw => mapping.Format((ushort)raw).Length);
                    Require(editor.Input.MaxLength >= longest, "Mod input cannot hold its complete signed/decimal physical range.");
                    Require(editor.Slider.IsEnabled && editor.Slider.Minimum == definition.Minimum && editor.Slider.Maximum == definition.Maximum && !editor.Apply.IsEnabled,
                        "Mod initial range/state incorrect.");
                    Require(editor.Input.Text == mapping.Format(values[editor.Index]), "Mod initial physical value differs from captured mapping.");
                    Require(((SolidColorBrush)editor.Slider.Foreground).Color.ToString() == "#FF52B7FF", "Mod slider fill differs from category color.");
                    for (var target = (int)definition.Minimum; target <= definition.Maximum; target++)
                    {
                        editor.Slider.Value = target;
                        var physicalText = mapping.Format((ushort)target);
                        Require(editor.Input.Text == physicalText && editor.Apply.IsEnabled == (target != values[editor.Index]), "Mod raw slider/physical input/Apply synchronization failed.");
                        Require((bool)Call(window, "HasPendingInputs")! == (target != values[editor.Index]), "Mod pending parser differs.");
                        editor.Input.Text = physicalText; Require(editor.Slider.Value == target, "Mod physical input did not update raw slider.");
                        Require(ReferenceEquals(snapshot, Field<CurrentPresetSnapshot>(window, "currentSnapshot")), "Mod pending input changed confirmed state.");
                        NoHardware(window);
                    }
                    var retained = editor.Slider.Value;
                    var first = mapping.Display(definition.Minimum); var last = mapping.Display(definition.Maximum);
                    foreach (var invalid in new[] { "", "bad", "NaN", "1e1",
                        (Math.Min(first, last) - Math.Abs(mapping.Scale)).ToString(CultureInfo.InvariantCulture),
                        (Math.Max(first, last) + Math.Abs(mapping.Scale)).ToString(CultureInfo.InvariantCulture),
                        (first + mapping.Scale / 2m).ToString(CultureInfo.InvariantCulture) })
                    {
                        editor.Input.Text = invalid;
                        Require(!editor.Apply.IsEnabled && editor.Slider.Value == retained, "Invalid Mod input was accepted or overwritten.");
                    }
                    editor.Input.Text = mapping.Format(values[editor.Index]);
                }
                VerifyJson(snapshot);
                foreach (var useMaximum in new[] { false, true })
                {
                    var endpointValues = values.ToArray();
                    foreach (var spec in specs) endpointValues[spec.ValueIndex] = useMaximum ? spec.Maximum : spec.Minimum;
                    var endpointBlocks = snapshot.Blocks.ToArray(); endpointBlocks[0] = endpointBlocks[0] with { Values = endpointValues };
                    VerifyJson(snapshot with { Blocks = endpointBlocks });
                }
                VerifyDuplicates(window, snapshot, editors);
                var other = ModParameters.Models.FirstOrDefault(m => m != model, model);
                if (other != model) VerifyDuplicates(window, Snapshot(model, other), editors);
                Load(window, snapshot); Call(window, "Begin", "Offline Mod busy check"); Blocked(editors, "Busy Mod controls permit edits."); Call(window, "End");
                Set(window, "snapshotFirmware", "unsupported"); Call(window, "UpdateThresholdState"); Blocked(editors, "Unsupported firmware permits Mod edits.");
                Require(!Field<Button>(window, "effectPowerButton").IsEnabled, "Unsupported firmware permits Mod power edits.");
                Load(window, snapshot); Set(window, "editSnapshotValid", false); Call(window, "UpdateThresholdState"); Blocked(editors, "Invalid snapshot permits Mod edits.");
                Load(window, snapshot); Set(window, "snapshotAddress", null); Call(window, "UpdateThresholdState"); Blocked(editors, "Missing target permits Mod edits.");
                foreach (var invalidIndex in Enumerable.Range(2, values.Count - 2))
                {
                    var spec = specs.FirstOrDefault(s => s.ValueIndex == invalidIndex);
                    var invalidValues = values.ToArray(); invalidValues[invalidIndex] = (ushort)(spec is null ? 1 : spec.Maximum + 1);
                    var invalidBlocks = snapshot.Blocks.ToArray(); invalidBlocks[0] = invalidBlocks[0] with { Values = invalidValues };
                    Load(window, snapshot with { Blocks = invalidBlocks }); Blocked(editors, "Invalid Mod source values permit parameter edits.");
                }
                var off = snapshot.Blocks.ToArray(); var disabled = values.ToArray(); disabled[0] = 0; off[0] = off[0] with { Values = disabled };
                Load(window, snapshot with { Blocks = off }); Blocked(editors, "Disabled Mod permits parameter edits.");
                Require(Field<Button>(window, "effectPowerButton").IsEnabled && (string)Field<Button>(window, "effectPowerButton").Content == "Enable effect", "Disabled Mod cannot be reenabled.");
                VerifyJson(snapshot with { Blocks = off }); NoHardware(window);
            }
            var known = Snapshot(ModParameters.Models[0], ModParameters.Models[0]);
            var knownEditors = Editors(window, known.Blocks[0]);
            var unknown = Snapshot(ushort.MaxValue, ushort.MaxValue); Load(window, unknown); Blocked(knownEditors, "Unknown Mod permits parameter edits.");
            Require(!Field<Button>(window, "effectPowerButton").IsEnabled, "Unknown Mod permits power edits.");
            var codec = typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetJsonDocument")!;
            var unknownJson = (string)codec.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
                .Invoke(null, [unknown, "V1.1.9", DateTimeOffset.Now, "amplifierSnapshot", true, false])!;
            using (var document = JsonDocument.Parse(unknownJson))
                Require(document.RootElement.GetProperty("effects").EnumerateArray().All(e => e.GetProperty("settings").GetArrayLength() == 0 &&
                    e.GetProperty("enabled").ValueKind == JsonValueKind.Null), "Unknown Mod JSON exposes guessed settings/state.");
            var preview = new MainWindow(preview: true);
            try { Load(preview, known); Blocked(Editors(preview, known.Blocks[0]), "Preview Mod permits writes."); Require(!Field<Button>(preview, "effectPowerButton").IsEnabled, "Preview Mod permits power writes."); NoHardware(preview); }
            finally { preview.Close(); }
            Console.WriteLine($"PASS: {ModParameters.Models.Count} Mod models, model-specific mobile-order controls/JSON values, every observed integer, five viewport sizes, same/different model duplicate pending isolation/refresh and invalid/busy/firmware/disabled/preview/no-hardware guards.");
        }
        finally { window.Close(); }
    }

    private static void VerifyDuplicates(MainWindow window, CurrentPresetSnapshot snapshot, Editor[] editors)
    {
        foreach (var editor in editors)
        {
            var firstSpec = ModParameters.For(snapshot.Blocks[0]).Single(s => s.ValueIndex == editor.Index);
            var secondSpec = ModParameters.For(snapshot.Blocks[1]).FirstOrDefault(s => s.ValueIndex == editor.Index);
            if (secondSpec is null) continue;
            var secondEditor = Editors(window, snapshot.Blocks[1]).Single(e => e.Index == editor.Index);
            Load(window, snapshot); var original = snapshot.Blocks[0].Values[editor.Index]; var secondOriginal = snapshot.Blocks[1].Values[editor.Index];
            var firstPending = original == firstSpec.Maximum ? firstSpec.Minimum : checked((ushort)(original + 1));
            var secondPending = secondOriginal == secondSpec.Minimum ? secondSpec.Maximum : secondSpec.Minimum;
            var firstMapping = Mappings[(snapshot.Blocks[0].Values[1], editor.Index)];
            var secondMapping = Mappings[(snapshot.Blocks[1].Values[1], editor.Index)];
            editor.Input.Text = firstMapping.Format(firstPending); Select(window, 2);
            Require(secondEditor.Input.Text == secondMapping.Format(secondOriginal), "Second Mod reused first pending value.");
            secondEditor.Input.Text = secondMapping.Format(secondPending); Select(window, 1);
            Require(editor.Input.Text == firstMapping.Format(firstPending), "First Mod lost pending value.");
            Call(window, "SetEffectsPreservingPending", (object)snapshot.Blocks);
            Require(editor.Input.Text == firstMapping.Format(firstPending), "Unchanged refresh lost first Mod pending value.");
            Select(window, 2); Require(secondEditor.Input.Text == secondMapping.Format(secondPending), "Unchanged refresh merged Mod pending inputs.");
            Require(!Field<Button>(window, "changeEffectModel").IsEnabled && !Field<Button>(window, "effectPowerButton").IsEnabled, "Hidden pending Mod values permit chain/power edits.");
            Load(window, snapshot); Require(!(bool)Call(window, "HasPendingInputs")!, "Fresh snapshot retained Mod pending values.");
        }
    }

    private static void VerifyJson(CurrentPresetSnapshot snapshot)
    {
        var type = typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetJsonDocument")!;
        var json = (string)type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .Invoke(null, [snapshot, "V1.1.9", DateTimeOffset.Now, "amplifierSnapshot", true, false])!;
        using var document = JsonDocument.Parse(json);
        var effect = document.RootElement.GetProperty("effects")[0];
        var settings = effect.GetProperty("settings").EnumerateArray().ToArray();
        var specs = ModParameters.For(snapshot.Blocks[0]);
        Require(settings.Select(s => s.GetProperty("name").GetString()).SequenceEqual(specs.Select(s => s.Name)), "Mod JSON names/order differ from mobile.");
        Require(settings.Select(s => s.GetProperty("value").GetDecimal()).SequenceEqual(specs.Select(s =>
            Mappings[(snapshot.Blocks[0].Values[1], s.ValueIndex)].Display(snapshot.Blocks[0].Values[s.ValueIndex]))), "Mod JSON physical values differ from captured mapping and confirmed state.");
        Require(settings.Select(s => s.GetProperty("unit").GetString()).SequenceEqual(specs.Select(s => s.Unit)) && settings.All(s => s.GetProperty("group").ValueKind == JsonValueKind.Null), "Mod JSON units/groups differ from verified layout.");
        var model = snapshot.Blocks[0].Values[1];
        Require(effect.GetProperty("name").GetString() == PresetEffectSearch.Catalog.Single(e => e.TypeCode == 6 && e.Model == model).Name, "Mod model title differs from verified presentation.");
        Require(effect.GetProperty("enabled").GetBoolean() == (snapshot.Blocks[0].Values[0] == 1), "Mod JSON enabled state differs.");
    }
}
