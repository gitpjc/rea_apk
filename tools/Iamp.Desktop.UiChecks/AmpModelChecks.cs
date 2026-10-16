using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class AmpModelChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static T Property<T>(object owner, string name) => (T)owner.GetType().GetProperty(name, Flags)!.GetValue(owner)!;
    private static void Set(object owner, string name, object? value) => owner.GetType().GetField(name, Flags)!.SetValue(owner, value);
    private static object? Call(object owner, string name, params object?[] args) => owner.GetType().GetMethod(name, Flags)!.Invoke(owner, args);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static object[] Editors(MainWindow window) => Property<IEnumerable>(window, "AdditionalEditors").Cast<object>()
        .Where(e => Property<byte>(e, "ProfilePosition") == 3).OrderBy(e => Property<int>(e, "ParameterIndex")).ToArray();
    private static ushort[] Values(ushort model) => [1, model, 10, 20, 30, 40, 50, 60, 1];
    private static CurrentPresetSnapshot Snapshot(ushort model, ushort secondModel)
    {
        var blocks = Enumerable.Range(1, 20).Select(i => new PresetEffectBlock((byte)i, 0, Array.Empty<ushort>())).ToArray();
        blocks[0] = new(1, 2, Values(model)); blocks[1] = new(2, 2, Values(secondModel));
        return new(4, "Offline Amp", "00", "00", blocks);
    }
    private static void Load(MainWindow window, CurrentPresetSnapshot snapshot)
    {
        Set(window, "currentSnapshot", snapshot); Set(window, "snapshotAddress", (ulong)1);
        Set(window, "snapshotFirmware", "V1.1.9"); Set(window, "editSnapshotValid", true); Set(window, "operation", null);
        Call(window, "SetEffects", (object)snapshot.Blocks);
    }
    private static void Select(MainWindow window, byte position) => ((Button)Field<WrapPanel>(window, "effectChain")
        .Children[position - 1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Update(MainWindow window) => Call(window, "UpdateThresholdState");
    private static void NoHardware(MainWindow window) => Require(Field<F15ReadSession?>(window, "connectedSession") is null &&
        Field<CancellationTokenSource?>(window, "operation") is null, "Offline Amp checks started device work.");
    private static void Blocked(object[] editors, string message) => Require(editors.All(e =>
        !Property<Slider>(e, "Slider").IsEnabled && !Property<Button>(e, "Apply").IsEnabled), message);

    internal static void Run()
    {
        var window = new MainWindow();
        try
        {
            var editors = Editors(window);
            Require(editors.Length == 6, "Amp must expose six shared editors.");
            Require(editors.Select(e => Property<string>(e, "Label")).SequenceEqual(AmpParameters.Settings.Select(s => s.Name)),
                "Amp editor names/order differ from mobile.");
            Require(editors.Select(e => Property<int>(e, "ParameterIndex")).SequenceEqual(Enumerable.Range(2, 6)),
                "Amp editors do not use the verified value indices.");
            foreach (var model in AmpParameters.Models)
            {
                var snapshot = Snapshot(model, model); var values = snapshot.Blocks[0].Values;
                Load(window, snapshot);
                Require(Field<StackPanel>(window, "effectDetails").Children.OfType<Slider>()
                    .SequenceEqual(editors.Select(e => Property<Slider>(e, "Slider"))), "Amp sliders do not follow mobile order.");
                Require(Field<Button>(window, "effectPowerButton").IsEnabled, "Verified Amp power control unavailable.");
                foreach (var editor in editors)
                {
                    var slider = Property<Slider>(editor, "Slider"); var input = Property<TextBox>(editor, "Input");
                    var apply = Property<Button>(editor, "Apply"); var index = Property<int>(editor, "ParameterIndex");
                    Require(slider.IsEnabled && slider.Minimum == 0 && slider.Maximum == 100 && !apply.IsEnabled,
                        "Amp initial range/state incorrect.");
                    for (var target = 0; target <= 100; target++)
                    {
                        slider.Value = target;
                        Require(input.Text == target.ToString() && apply.IsEnabled == (target != values[index]),
                            "Amp slider/input/Apply synchronization failed.");
                        Require((bool)Call(window, "HasPendingInputs")! == (target != values[index]), "Amp pending parser differs.");
                        input.Text = target.ToString(); Require(slider.Value == target, "Amp numeric input did not update slider.");
                        Require(ReferenceEquals(snapshot, Field<CurrentPresetSnapshot>(window, "currentSnapshot")),
                            "Amp pending input changed confirmed state.");
                        NoHardware(window);
                    }
                    var retained = slider.Value;
                    foreach (var invalid in new[] { "", "bad", "-1", "101" })
                    {
                        input.Text = invalid;
                        Require(!apply.IsEnabled && slider.Value == retained, "Invalid Amp input was accepted or overwritten.");
                    }
                    input.Text = values[index].ToString();
                }
                VerifyJson(snapshot);
                VerifyDuplicates(window, snapshot, editors);
                var other = AmpParameters.Models.FirstOrDefault(m => m != model);
                if (other != model) VerifyDuplicates(window, Snapshot(model, other), editors);
                Load(window, snapshot);
                Call(window, "Begin", "Offline Amp busy check"); Blocked(editors, "Busy Amp controls permit edits."); Call(window, "End");
                Set(window, "snapshotFirmware", "unsupported"); Update(window); Blocked(editors, "Unsupported firmware permits Amp edits.");
                Require(!Field<Button>(window, "effectPowerButton").IsEnabled, "Unsupported firmware permits Amp power edits.");
                Load(window, snapshot); Set(window, "editSnapshotValid", false); Update(window);
                Blocked(editors, "Invalid snapshot permits Amp edits.");
                Load(window, snapshot); Set(window, "snapshotAddress", null); Update(window);
                Blocked(editors, "Missing amplifier target permits Amp edits.");
                Load(window, snapshot);
                var disabled = values.ToArray(); disabled[0] = 0;
                var off = snapshot.Blocks.ToArray(); off[0] = off[0] with { Values = disabled };
                Load(window, snapshot with { Blocks = off }); Blocked(editors, "Disabled Amp permits parameter edits.");
                Require(Field<Button>(window, "effectPowerButton").IsEnabled &&
                    (string)Field<Button>(window, "effectPowerButton").Content == "Enable effect", "Disabled Amp cannot be reenabled.");
                NoHardware(window);
            }
            var preview = new MainWindow(preview: true);
            try
            {
                Load(preview, Snapshot(AmpParameters.Models[0], AmpParameters.Models[0]));
                Blocked(Editors(preview), "Preview Amp permits parameter writes.");
                Require(!Field<Button>(preview, "effectPowerButton").IsEnabled, "Preview Amp permits power writes.");
                NoHardware(preview);
            }
            finally { preview.Close(); }
            Console.WriteLine($"PASS: {AmpParameters.Models.Count} Amp models, six mobile-order controls/JSON values, every integer 0–100, same/different model duplicate pending isolation and unchanged refresh, invalid/busy/firmware/disabled/preview/no-hardware guards.");
        }
        finally { window.Close(); }
    }

    private static void VerifyDuplicates(MainWindow window, CurrentPresetSnapshot snapshot, object[] editors)
    {
        Load(window, snapshot); var input = Property<TextBox>(editors[0], "Input");
        input.Text = "11"; Select(window, 2); Require(input.Text == "10", "Second Amp reused the first pending value.");
        input.Text = "12"; Select(window, 1); Require(input.Text == "11", "First Amp lost its pending value.");
        Call(window, "SetEffectsPreservingPending", (object)snapshot.Blocks);
        Require(input.Text == "11", "Unchanged refresh lost first Amp pending input.");
        Select(window, 2); Require(input.Text == "12", "Unchanged refresh merged duplicate Amp pending inputs.");
        Require(!Field<Button>(window, "changeEffectModel").IsEnabled && !Field<Button>(window, "effectPowerButton").IsEnabled,
            "Hidden pending Amp inputs permit chain/power edits.");
        Load(window, snapshot); Require(!(bool)Call(window, "HasPendingInputs")!, "Fresh snapshot retained Amp pending inputs.");
    }

    private static void VerifyJson(CurrentPresetSnapshot snapshot)
    {
        var type = typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetJsonDocument")!;
        var json = (string)type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .Invoke(null, [snapshot, "V1.1.9", DateTimeOffset.Now, "amplifierSnapshot", true, false])!;
        using var document = JsonDocument.Parse(json);
        var settings = document.RootElement.GetProperty("effects")[0].GetProperty("settings").EnumerateArray().ToArray();
        Require(settings.Select(s => s.GetProperty("name").GetString()).SequenceEqual(AmpParameters.Settings.Select(s => s.Name)),
            "Amp JSON names/order differ from mobile.");
        Require(settings.Select(s => s.GetProperty("value").GetDecimal()).SequenceEqual(
            AmpParameters.Settings.Select(s => (decimal)snapshot.Blocks[0].Values[s.ValueIndex])), "Amp JSON values differ from confirmed state.");
    }
}
