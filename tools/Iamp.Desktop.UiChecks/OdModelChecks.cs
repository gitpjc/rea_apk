using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class OdModelChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static T Property<T>(object owner, string name) => (T)owner.GetType().GetProperty(name, Flags)!.GetValue(owner)!;
    private static void Set(object owner, string name, object? value) => owner.GetType().GetField(name, Flags)!.SetValue(owner, value);
    private static object? Call(object owner, string name, params object?[] args) => owner.GetType().GetMethod(name, Flags)!.Invoke(owner, args);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed record Editor(int Index, Slider Slider, TextBox Input, Button Apply);
    private static Editor[] Editors(MainWindow window)
    {
        var shared = Property<IEnumerable>(window, "AdditionalEditors").Cast<object>()
            .Where(e => Property<byte>(e, "ProfilePosition") == 2)
            .Select(e => new Editor(Property<int>(e, "ParameterIndex"), Property<Slider>(e, "Slider"),
                Property<TextBox>(e, "Input"), Property<Button>(e, "Apply")));
        return shared.Append(new(3, Field<Slider>(window, "toneSlider"), Field<TextBox>(window, "toneInput"),
            Field<Button>(window, "applyTone"))).OrderBy(e => e.Index).ToArray();
    }
    private static CurrentPresetSnapshot Snapshot(ushort model, ushort secondModel)
    {
        var blocks = Enumerable.Range(1, 20).Select(i => new PresetEffectBlock((byte)i, 0, Array.Empty<ushort>())).ToArray();
        blocks[0] = new(1, 1, new ushort[] { 1, model, 10, 20, 30 });
        blocks[1] = new(2, 1, new ushort[] { 1, secondModel, 10, 20, 30 });
        return new(4, "Offline OD", "00", "00", blocks);
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
        Field<CancellationTokenSource?>(window, "operation") is null, "Offline OD checks started device work.");
    private static void Blocked(Editor[] editors, string message) => Require(editors.All(e => !e.Slider.IsEnabled && !e.Apply.IsEnabled), message);

    internal static void Run()
    {
        var window = new MainWindow();
        try
        {
            var editors = Editors(window);
            Require(editors.Select(e => e.Index).SequenceEqual(new[] { 2, 3, 4 }), "OD must expose Gain/Tone/Vol editors.");
            foreach (var model in OdParameters.Models)
            {
                var snapshot = Snapshot(model, model); var values = snapshot.Blocks[0].Values;
                Load(window, snapshot);
                Require(Field<StackPanel>(window, "effectDetails").Children.OfType<Slider>().SequenceEqual(editors.Select(e => e.Slider)),
                    "OD sliders do not follow mobile Gain/Tone/Vol order.");
                Require(Field<Button>(window, "effectPowerButton").IsEnabled, "Verified OD power control unavailable.");
                foreach (var editor in editors)
                {
                    Require(editor.Slider.IsEnabled && editor.Slider.Minimum == 0 && editor.Slider.Maximum == 100 && !editor.Apply.IsEnabled,
                        "OD initial range/state incorrect.");
                    Require(((SolidColorBrush)editor.Slider.Foreground).Color.ToString() == "#FFD6AE48", "OD slider fill differs from category color.");
                    for (var target = 0; target <= 100; target++)
                    {
                        editor.Slider.Value = target;
                        Require(editor.Input.Text == target.ToString() && editor.Apply.IsEnabled == (target != values[editor.Index]), "OD slider/input/Apply synchronization failed.");
                        Require((bool)Call(window, "HasPendingInputs")! == (target != values[editor.Index]), "OD pending parser differs.");
                        editor.Input.Text = target.ToString(); Require(editor.Slider.Value == target, "OD numeric input did not update slider.");
                        Require(ReferenceEquals(snapshot, Field<CurrentPresetSnapshot>(window, "currentSnapshot")), "OD pending input changed confirmed state.");
                        NoHardware(window);
                    }
                    var retained = editor.Slider.Value;
                    foreach (var invalid in new[] { "", "bad", "-1", "101" })
                    {
                        editor.Input.Text = invalid;
                        Require(!editor.Apply.IsEnabled && editor.Slider.Value == retained, "Invalid OD input was accepted or overwritten.");
                    }
                    editor.Input.Text = values[editor.Index].ToString();
                }
                VerifyJson(snapshot);
                VerifyDuplicates(window, snapshot, editors);
                var other = OdParameters.Models.FirstOrDefault(m => m != model);
                if (other != model) VerifyDuplicates(window, Snapshot(model, other), editors);
                Load(window, snapshot); Call(window, "Begin", "Offline OD busy check"); Blocked(editors, "Busy OD controls permit edits."); Call(window, "End");
                Set(window, "snapshotFirmware", "unsupported"); Call(window, "UpdateThresholdState"); Blocked(editors, "Unsupported firmware permits OD edits.");
                Require(!Field<Button>(window, "effectPowerButton").IsEnabled, "Unsupported firmware permits OD power edits.");
                Load(window, snapshot); Set(window, "editSnapshotValid", false); Call(window, "UpdateThresholdState"); Blocked(editors, "Invalid snapshot permits OD edits.");
                Load(window, snapshot); Set(window, "snapshotAddress", null); Call(window, "UpdateThresholdState"); Blocked(editors, "Missing target permits OD edits.");
                var off = snapshot.Blocks.ToArray(); var disabled = values.ToArray(); disabled[0] = 0; off[0] = off[0] with { Values = disabled };
                Load(window, snapshot with { Blocks = off }); Blocked(editors, "Disabled OD permits parameter edits.");
                Require(Field<Button>(window, "effectPowerButton").IsEnabled && (string)Field<Button>(window, "effectPowerButton").Content == "Enable effect", "Disabled OD cannot be reenabled.");
                VerifyJson(snapshot with { Blocks = off }); NoHardware(window);
            }
            var unknown = Snapshot(ushort.MaxValue, ushort.MaxValue); Load(window, unknown); Blocked(editors, "Unknown OD permits parameter edits.");
            Require(!Field<Button>(window, "effectPowerButton").IsEnabled, "Unknown OD permits power edits.");
            var preview = new MainWindow(preview: true);
            try { Load(preview, Snapshot(OdParameters.Models[0], OdParameters.Models[0])); Blocked(Editors(preview), "Preview OD permits writes."); Require(!Field<Button>(preview, "effectPowerButton").IsEnabled, "Preview OD permits power writes."); NoHardware(preview); }
            finally { preview.Close(); }
            Console.WriteLine($"PASS: {OdParameters.Models.Count} OD models, three mobile-order controls/JSON values, every integer 0–100, same/different model duplicate pending isolation/refresh and invalid/busy/firmware/disabled/preview/no-hardware guards.");
        }
        finally { window.Close(); }
    }

    private static void VerifyDuplicates(MainWindow window, CurrentPresetSnapshot snapshot, Editor[] editors)
    {
        foreach (var editor in editors)
        {
            Load(window, snapshot); var original = snapshot.Blocks[0].Values[editor.Index];
            editor.Input.Text = (original + 1).ToString(); Select(window, 2);
            Require(editor.Input.Text == original.ToString(), "Second OD reused first pending value.");
            editor.Input.Text = (original + 2).ToString(); Select(window, 1);
            Require(editor.Input.Text == (original + 1).ToString(), "First OD lost pending value.");
            Call(window, "SetEffectsPreservingPending", (object)snapshot.Blocks);
            Require(editor.Input.Text == (original + 1).ToString(), "Unchanged refresh lost first OD pending value.");
            Select(window, 2); Require(editor.Input.Text == (original + 2).ToString(), "Unchanged refresh merged OD pending inputs.");
            Require(!Field<Button>(window, "changeEffectModel").IsEnabled && !Field<Button>(window, "effectPowerButton").IsEnabled, "Hidden pending OD values permit chain/power edits.");
            Load(window, snapshot); Require(!(bool)Call(window, "HasPendingInputs")!, "Fresh snapshot retained OD pending values.");
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
        Require(settings.Select(s => s.GetProperty("name").GetString()).SequenceEqual(OdParameters.Settings.Select(s => s.Name)), "OD JSON names/order differ from mobile.");
        Require(settings.Select(s => s.GetProperty("value").GetDecimal()).SequenceEqual(OdParameters.Settings.Select(s => (decimal)snapshot.Blocks[0].Values[s.ValueIndex])), "OD JSON values differ from confirmed state.");
        Require(settings.All(s => s.GetProperty("unit").ValueKind == JsonValueKind.Null && s.GetProperty("group").ValueKind == JsonValueKind.Null), "OD JSON invented units/groups.");
        var model = snapshot.Blocks[0].Values[1];
        Require(effect.GetProperty("name").GetString() == (model == 3 ? "OD808" : PresetEffectSearch.Catalog.Single(e => e.TypeCode == 1 && e.Model == model).Name), "OD model title differs from verified presentation.");
        Require(effect.GetProperty("enabled").GetBoolean() == (snapshot.Blocks[0].Values[0] == 1), "OD JSON enabled state differs.");
    }
}
