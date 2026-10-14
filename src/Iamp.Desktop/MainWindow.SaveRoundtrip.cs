using System.IO;
using System.Text.Json;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    /// <summary>Explicit CLI opt-in only; same-slot save 25→26→25 and full independent restoration.</summary>
    internal async Task RunSaveRoundtripPreviewAsync(string target,string outputPath)
    {
        CurrentPresetSnapshot? baseline=null;ulong? deviceAddress=null;Exception? failure=null;
        var save26Attempted=false;var originalSaveAttempted=false;var originalSaveConfirmed=false;var baselineRestored=false;
        var stage="read baseline";var steps=new List<object>();
        var evidencePath=Path.ChangeExtension(Path.GetFullPath(outputPath),".save-roundtrip.json");
        Directory.CreateDirectory(Path.GetDirectoryName(evidencePath)!);
        async Task Record()=>await File.WriteAllTextAsync(evidencePath,JsonSerializer.Serialize(new {operation="desktop_same_slot_save_25_26_25",
            version=DesktopVersion.Current,stage,baseline,steps,save26Attempted,originalSaveAttempted,originalSaveConfirmed,baselineRestored,
            passed=failure is null && baselineRestored,failure=failure?.ToString()},new JsonSerializerOptions {WriteIndented=true}));
        try {
            if(!await ReadSnapshotPreviewAsync(target) || currentSnapshot is not {} current || snapshotAddress is null)
                throw new IOException("Could not read the original amplifier baseline.");
            _=ObservedPresetSaveCommand.CreateUnchanged(snapshotFirmware!,Convert.FromHexString(current.RawStatePayloadHex),Convert.FromHexString(current.RawDetailPayloadHex));
            baseline=current;deviceAddress=snapshotAddress;await Record();
            stage="UI apply threshold 26";thresholdInput.Text="26";
            if(!applyThreshold.IsEnabled)throw new InvalidOperationException("Threshold Apply is unavailable.");
            await ApplyThresholdAsync();RequireUiMutation(4,26);
            if(!savePreset.IsEnabled)throw new InvalidOperationException("The supported edited profile did not enable Save.");
            stage="UI same-slot save 26";save26Attempted=true;await Record();
            await SavePresetAsync(confirmedTest:true);
            if(!lastSaveSucceeded || liveEditUnsaved)throw new IOException("UI save 26 was not confirmed.");
            steps.Add(new {stage,snapshot=currentSnapshot});await Record();
            using(var check=new CancellationTokenSource(TimeSpan.FromSeconds(60))) {
                await using var independent=await F15ReadSession.ConnectAsync(deviceAddress.Value,check.Token);
                var readback=await independent.ReadCurrentPresetAsync(check.Token);
                RequireExactBaseline(readback,currentSnapshot!);
                steps.Add(new {stage="independent saved26 readback",snapshot=readback});
            }
            stage="UI apply original threshold 25";thresholdInput.Text="25";
            if(!applyThreshold.IsEnabled)throw new InvalidOperationException("Restoration Apply is unavailable.");
            await ApplyThresholdAsync();RequireUiMutation(4,25);RequireExactBaseline(currentSnapshot!,baseline);
            if(!savePreset.IsEnabled)throw new InvalidOperationException("Original-value restoration did not enable Save.");
            stage="UI save original threshold 25";originalSaveAttempted=true;await Record();
            await SavePresetAsync(confirmedTest:true);originalSaveConfirmed=lastSaveSucceeded;
            if(!originalSaveConfirmed)throw new IOException("Original-value save outcome is unknown; no automatic repeat.");
            steps.Add(new {stage,snapshot=currentSnapshot});await Record();
        }
        catch(Exception ex) {failure=ex;}
        finally {
            if(baseline is not null && deviceAddress.HasValue) {
                try {
                    using var cleanup=new CancellationTokenSource(TimeSpan.FromSeconds(90));
                    await using var session=await F15ReadSession.ConnectAsync(deviceAddress.Value,cleanup.Token);
                    var actual=await session.ReadCurrentPresetAsync(cleanup.Token);
                    _=ObservedPresetSaveCommand.CreateForObservedThreshold("V1.1.9",Convert.FromHexString(actual.RawStatePayloadHex),Convert.FromHexString(actual.RawDetailPayloadHex));
                    RequirePreservedGlobalState(actual,baseline);
                    if(actual.Blocks[0].Values[2]!=25)actual=await session.SetNoiseKillerThresholdAsync(actual,25,cleanup.Token);
                    RequireExactBaseline(actual,baseline);
                    if(save26Attempted && !originalSaveAttempted) {
                        stage="independent original-value restorative save";originalSaveAttempted=true;await Record();
                        actual=await session.SaveObservedThresholdAsync(actual,cleanup.Token);originalSaveConfirmed=true;
                    }
                    if(save26Attempted && !originalSaveConfirmed)
                        throw new IOException("Original live values restored, but stored baseline is unverified after ambiguous save. No save was repeated.");
                    // Only the exact original profiles reach the existing guarded recall API.
                    actual=await session.SelectObservedPresetAsync(actual,5,cleanup.Token);
                    actual=await session.SelectObservedPresetAsync(actual,4,cleanup.Token);
                    actual=await session.ReadCurrentPresetAsync(cleanup.Token);RequireExactBaseline(actual,baseline);
                    baselineRestored=true;currentSnapshot=actual;originalSnapshotDetail=actual.RawDetailPayloadHex;
                    liveEditUnsaved=false;editSnapshotValid=true;snapshotTime=DateTimeOffset.Now;
                    currentPreset.Text=$"Index {actual.WireIndex} · {actual.Name}";SetEffects(actual.Blocks);
                    steps.Add(new {stage="independent original saved-value recall 5 to 4",snapshot=actual});
                }
                catch(Exception ex) {editSnapshotValid=false;failure=failure is null?ex:new AggregateException(failure,ex);}
            }
            status.Text=failure is null&&baselineRestored?"Windows save test passed · Original saved preset restored and recalled":
                baselineRestored?"Windows save test failed · Original saved preset restored":"Windows save test failed · Stored baseline restoration unresolved";
            snapshot.Text=SnapshotLabel();UpdateThresholdState();await Record();
        }
        if(failure is not null)throw failure;
        if(!baselineRestored)throw new IOException("Saved baseline was not independently verified.");
    }
}
