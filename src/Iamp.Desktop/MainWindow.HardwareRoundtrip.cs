using System.IO;
using System.Text.Json;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    /// <summary>
    /// Explicit hardware QA only: the CLI opt-in runs the actual UI read/recall/edit handlers.
    /// Requires captured V1.1.9 slot 4 at threshold 25/Tone 100 and independently restores its full baseline.
    /// Never called by the default app, demo preview or read-only preview.
    /// </summary>
    internal async Task RunHardwareRoundtripPreviewAsync(string bluetoothAddress,string outputPath)
    {
        CurrentPresetSnapshot? baseline=null;
        ulong? baselineAddress=null;
        Exception? failure=null;
        string stage="initial UI read";
        bool baselineRestored=false;
        var evidencePath=Path.ChangeExtension(Path.GetFullPath(outputPath),".roundtrip.json");
        Directory.CreateDirectory(Path.GetDirectoryName(evidencePath)!);
        try {
            if(!await ReadSnapshotPreviewAsync(bluetoothAddress) || !editSnapshotValid || snapshotFirmware!="V1.1.9" ||
                currentSnapshot is not {} initial || initial.WireIndex!=4 || !IsCapturedPresetConfiguration(initial) ||
                EditableNoiseKiller()?.Values[2]!=25 || EditableNoiseKiller()?.Values[3]!=50 ||
                Editable808Tone()?.Values[2]!=39 || Editable808Tone()?.Values[3]!=100 || Editable808Tone()?.Values[4]!=100 || snapshotAddress is null)
                throw new InvalidOperationException("UI test requires the captured V1.1.9 preset 4 baseline at Noise Killer threshold 25 and OD808 Tone 100.");
            _=PresetSelectionCommand.Create(Convert.FromHexString(initial.RawStatePayloadHex),4);
            baseline=initial;
            baselineAddress=snapshotAddress;
            // Persist the complete original state/detail before the first hardware mutation.
            await File.WriteAllTextAsync(evidencePath,JsonSerializer.Serialize(new {operation="desktop_ui_roundtrip",stage="baseline recorded; no writes yet",baseline},new JsonSerializerOptions {WriteIndented=true}));
            stage="UI recall 4 to 5";
            presets.SelectedItem=names.Single(p=>p.WireIndex==5);
            if(!selectPreset.IsEnabled)throw new InvalidOperationException("UI recall button is disabled for captured slot 5.");
            await SelectPresetAsync();
            RequireUiMutation(5,null);
            stage="UI recall 5 to 4";
            presets.SelectedItem=names.Single(p=>p.WireIndex==4);
            if(!selectPreset.IsEnabled)throw new InvalidOperationException("UI recall button is disabled for captured slot 4.");
            await SelectPresetAsync();
            RequireUiMutation(4,25);
            RequireExactBaseline(currentSnapshot!,baseline);
            stage="UI threshold 25 to 26";
            thresholdInput.Text="26";
            if(!applyThreshold.IsEnabled)throw new InvalidOperationException("UI Apply button is disabled for threshold 26.");
            await ApplyThresholdAsync();RequireUiMutation(4,26);
            stage="UI threshold 26 to 25";
            thresholdInput.Text="25";
            if(!applyThreshold.IsEnabled)throw new InvalidOperationException("UI Apply button is disabled for threshold 25 restoration.");
            await ApplyThresholdAsync();RequireUiMutation(4,25);
            RequireExactBaseline(currentSnapshot!,baseline);
            if(liveEditUnsaved)throw new InvalidOperationException("Restored UI threshold still reports a difference from its original snapshot.");
            presets.SelectedItem=names.Single(p=>p.WireIndex==5);
            if(!selectPreset.IsEnabled)throw new InvalidOperationException("Exact threshold restoration did not re-enable preset recall.");
            stage="UI OD808 Tone 100 to 98";
            SelectEffect(effectButtons.Single(item=>item.Effect.Source.Position==2 && item.Effect.Source.TypeCode==1).Effect);
            toneInput.Text="98";
            if(!applyTone.IsEnabled)throw new InvalidOperationException("UI Apply Tone button is disabled for Tone 98.");
            await ApplyToneAsync();RequireUiTone(98);
            stage="UI OD808 Tone 98 to 100";
            toneInput.Text="100";
            if(!applyTone.IsEnabled)throw new InvalidOperationException("UI Apply Tone button is disabled for Tone 100 restoration.");
            await ApplyToneAsync();RequireUiTone(100);
            RequireExactBaseline(currentSnapshot!,baseline);
            if(liveEditUnsaved || !selectPreset.IsEnabled)
                throw new InvalidOperationException("Exact Tone restoration did not restore the original snapshot UI state.");
            stage="UI Noise Killer Level 50 to 48 to 50";
            await RoundtripAdditionalEditorAsync(noiseLevelEditor,48,50,baseline);
            stage="UI OD808 Gain 39 to 37 to 39";
            await RoundtripAdditionalEditorAsync(gainEditor,37,39,baseline);
            stage="UI OD808 Vol 100 to 98 to 100";
            await RoundtripAdditionalEditorAsync(volumeEditor,98,100,baseline);
            stage="UI combined five-parameter dirty-state update and restore";
            SelectEffect(effectButtons.Single(item=>item.Effect.Source.Position==1).Effect);
            thresholdInput.Text="26";
            if(!applyThreshold.IsEnabled)throw new InvalidOperationException("Threshold Apply is disabled for combined update.");
            await ApplyThresholdAsync();RequireUiMutation(4,26);
            await ApplyAdditionalUiValueAsync(noiseLevelEditor,48);
            await ApplyAdditionalUiValueAsync(gainEditor,37);
            SelectEffect(effectButtons.Single(item=>item.Effect.Source.Position==2).Effect);
            toneInput.Text="98";
            if(!applyTone.IsEnabled)throw new InvalidOperationException("Tone Apply is disabled for combined update.");
            await ApplyToneAsync();RequireUiTone(98);
            await ApplyAdditionalUiValueAsync(volumeEditor,98);
            RequireUiMutation(4,26);RequireUiTone(98);
            if(EditableNoiseKiller()?.Values[3]!=48 || Editable808Tone()?.Values[2]!=37 || Editable808Tone()?.Values[4]!=98 || !liveEditUnsaved || !selectionMessage.Text.Contains("confirmation",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Combined changed values or dirty-state recall confirmation were not preserved.");
            SelectEffect(effectButtons.Single(item=>item.Effect.Source.Position==1).Effect);
            thresholdInput.Text="25";
            if(!applyThreshold.IsEnabled)throw new InvalidOperationException("Threshold Apply is disabled during combined restoration.");
            await ApplyThresholdAsync();RequireUiMutation(4,25);
            await ApplyAdditionalUiValueAsync(noiseLevelEditor,50);
            await ApplyAdditionalUiValueAsync(gainEditor,39);
            SelectEffect(effectButtons.Single(item=>item.Effect.Source.Position==2).Effect);
            toneInput.Text="100";
            if(!applyTone.IsEnabled)throw new InvalidOperationException("Tone Apply is disabled during combined restoration.");
            await ApplyToneAsync();RequireUiTone(100);
            await ApplyAdditionalUiValueAsync(volumeEditor,100);
            RequireExactBaseline(currentSnapshot!,baseline);
            if(liveEditUnsaved || !selectPreset.IsEnabled)throw new InvalidOperationException("Combined restoration did not restore the snapshot/recall UI state.");
        }
        catch(Exception ex) {failure=new InvalidOperationException($"Hardware UI test failed at {stage}: {ex.Message}",ex);}
        finally {
            if(baseline is not null && baselineAddress is {} target) {
                try {
                    // Separate bounded cleanup survives UI handler cancellation/failure and owns a new session.
                    using var cleanup=new CancellationTokenSource(TimeSpan.FromSeconds(90));
                    await using var session=await F15ReadSession.ConnectAsync(target,cleanup.Token);
                    var identity=await session.ReadIdentityAsync(cleanup.Token);
                    if(identity.Firmware!="V1.1.9")throw new NotSupportedException("Firmware changed; cleanup will not send commands.");
                    var actual=await session.ReadCurrentPresetAsync(cleanup.Token);
                    RequirePreservedGlobalState(actual,baseline);
                    if(actual.WireIndex==5) {
                        PresetSelectionCommand.RequireObservedPreset(5,Convert.FromHexString(actual.RawDetailPayloadHex));
                        actual=await session.SelectObservedPresetAsync(actual,4,cleanup.Token);
                    }
                    if(actual.WireIndex!=4)throw new NotSupportedException("Unknown current slot; cleanup will not send commands.");
                    RequirePreservedGlobalState(actual,baseline);
                    var block=actual.Blocks.FirstOrDefault()??throw new FormatException("Missing cleanup effect block.");
                    if(block.Position!=1 || block.TypeCode!=4 || block.Values.Count!=6 || block.Values[0]!=1 || block.Values[1]!=2 ||
                        block.Values[2] is <25 or >28 || block.Values[3] is <47 or >50 || block.Values[4]!=0 || block.Values[5]!=0)
                        throw new NotSupportedException("Unknown Noise Killer configuration; cleanup will not send commands.");
                    var tone=actual.Blocks.FirstOrDefault(b=>b.Position==2);
                    if(!IsSupported808ToneBlock(tone))throw new NotSupportedException("Unknown OD808 configuration; cleanup will not send commands.");
                    var detail=Convert.FromHexString(actual.RawDetailPayloadHex);
                    detail=PresetDataParser.WithParameterValue(detail,1,2,25);
                    detail=PresetDataParser.WithParameterValue(detail,1,3,50);
                    detail=PresetDataParser.WithParameterValue(detail,2,2,39);
                    detail=PresetDataParser.WithParameterValue(detail,2,3,100);
                    detail=PresetDataParser.WithParameterValue(detail,2,4,100);
                    if(Convert.ToHexString(detail)!=baseline.RawDetailPayloadHex)
                        throw new NotSupportedException("Unknown preset changes; cleanup will not overwrite them.");
                    if(block.Values[2]!=25)actual=await session.SetNoiseKillerThresholdAsync(actual,25,cleanup.Token);
                    if(actual.Blocks[0].Values[3]!=50)actual=await session.SetNoiseKillerLevelAsync(actual,50,cleanup.Token);
                    if(actual.Blocks[1].Values[2]!=39)actual=await session.Set808GainAsync(actual,39,cleanup.Token);
                    if(actual.Blocks[1].Values[3]!=100)actual=await session.Set808ToneAsync(actual,100,cleanup.Token);
                    if(actual.Blocks[1].Values[4]!=100)actual=await session.Set808VolumeAsync(actual,100,cleanup.Token);
                    var verified=await session.ReadCurrentPresetAsync(cleanup.Token);
                    RequireExactBaseline(verified,baseline);
                    baselineRestored=true;currentSnapshot=verified;originalSnapshotDetail=baseline.RawDetailPayloadHex;
                    liveEditUnsaved=false;editSnapshotValid=true;snapshotTime=DateTimeOffset.Now;
                    currentPreset.Text=$"{verified.SlotLabel} · {verified.Name}";SetEffects(verified.Blocks);
                }
                catch(Exception cleanupError) {
                    editSnapshotValid=false;
                    failure=new AggregateException("Hardware UI test baseline could not be independently restored and verified.",
                        failure is null?new[]{cleanupError}:new[]{failure,cleanupError});
                }
            }
            snapshot.Text=SnapshotLabel();
            status.Text=failure is null&&baselineRestored?"Hardware UI round trip passed · Full baseline restored · Nothing saved":
                baselineRestored?"Hardware UI test failed · Full baseline restored · Nothing saved":"Hardware UI test failed · Baseline restoration not verified";
            UpdateThresholdState();
            await File.WriteAllTextAsync(evidencePath,JsonSerializer.Serialize(new {operation="desktop_ui_roundtrip",stage,baselineRestored,
                passed=failure is null&&baselineRestored,failure=failure?.ToString(),baseline,final=currentSnapshot},new JsonSerializerOptions {WriteIndented=true}));
        }
        if(failure is not null)throw failure;
        if(!baselineRestored)throw new InvalidOperationException("Hardware UI test did not establish a restored baseline.");
    }

    private void RequireUiMutation(int index,ushort? threshold)
    {
        if(!lastMutationSucceeded || !editSnapshotValid || currentSnapshot?.WireIndex!=index ||
            (threshold.HasValue && EditableNoiseKiller()?.Values[2]!=threshold.Value))
            throw new InvalidOperationException($"UI operation was not confirmed: {status.Text}");
    }
    private void RequireUiTone(ushort tone)
    {
        if(!lastMutationSucceeded || !editSnapshotValid || currentSnapshot?.WireIndex!=4 || Editable808Tone()?.Values[3]!=tone)
            throw new InvalidOperationException($"UI Tone operation was not confirmed: {status.Text}");
    }
    private async Task ApplyAdditionalUiValueAsync(PendingParameterEditor editor,ushort value)
    {
        SelectEffect(effectButtons.Single(item=>item.Effect.Source.Position==editor.Position).Effect);
        editor.Input.Text=value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if(!editor.Apply.IsEnabled)throw new InvalidOperationException($"UI Apply {editor.Label} is disabled for {value}.");
        await ApplyAdditionalParameterAsync(editor);
        if(!lastMutationSucceeded || !editSnapshotValid || currentSnapshot?.WireIndex!=4 || EditableBlock(editor)?.Values[editor.ParameterIndex]!=value)
            throw new InvalidOperationException($"UI {editor.Label} operation was not confirmed: {status.Text}");
    }
    private async Task RoundtripAdditionalEditorAsync(PendingParameterEditor editor,ushort changed,ushort original,CurrentPresetSnapshot baseline)
    {
        await ApplyAdditionalUiValueAsync(editor,changed);
        await ApplyAdditionalUiValueAsync(editor,original);
        RequireExactBaseline(currentSnapshot!,baseline);
        if(liveEditUnsaved || !selectPreset.IsEnabled)throw new InvalidOperationException($"Restoring {editor.Label} did not restore the snapshot/recall UI state.");
    }
    private static void RequirePreservedGlobalState(CurrentPresetSnapshot actual,CurrentPresetSnapshot baseline)
    {
        var state=Convert.FromHexString(actual.RawStatePayloadHex);
        if(state.Length!=92 || actual.WireIndex is not (4 or 5) || state[0]!=0x12 || state[1]!=actual.WireIndex)
            throw new NotSupportedException("Unknown state schema/current slot; cleanup will not send commands.");
        state[1]=(byte)baseline.WireIndex;
        if(Convert.ToHexString(state)!=baseline.RawStatePayloadHex)
            throw new NotSupportedException("Global state differs from baseline; cleanup will not overwrite it.");
    }
    private static void RequireExactBaseline(CurrentPresetSnapshot actual,CurrentPresetSnapshot baseline)
    {
        if(actual.WireIndex!=baseline.WireIndex || actual.RawStatePayloadHex!=baseline.RawStatePayloadHex ||
            actual.RawDetailPayloadHex!=baseline.RawDetailPayloadHex)
            throw new InvalidOperationException("Full preset state/detail do not match the original baseline.");
    }
}
