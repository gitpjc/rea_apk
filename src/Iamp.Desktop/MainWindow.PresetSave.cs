using System.Windows;
using System.Windows.Controls;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button savePreset=Button("Save current preset");
    private readonly TextBlock saveMessage=Text("Read the amplifier before saving.",12,MutedBrush,new Thickness(0,8,0,0));
    private bool lastSaveSucceeded;

    private bool SupportedSaveSnapshot()
    {
        if(currentSnapshot is not {} current || snapshotFirmware is not {} version)return false;
        try {
            _=ObservedPresetSaveCommand.CreateForObservedThreshold(version,Convert.FromHexString(current.RawStatePayloadHex),Convert.FromHexString(current.RawDetailPayloadHex));
            return true;
        }
        catch(Exception ex) when(ex is FormatException or NotSupportedException) {return false;}
    }

    private void UpdateSaveState()
    {
        var supported=!preview && !localSnapshotSource && editSnapshotValid && snapshotAddress.HasValue && SupportedSaveSnapshot();
        savePreset.IsEnabled=supported && liveEditUnsaved && operation is null;
        saveMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            !supported?"Save supports 0D British Lead Solo, Threshold 25 or 26, with all other original values unchanged.":
            !liveEditUnsaved?"No new live edits to save. Matching the read snapshot alone does not prove storage.":
            "Save overwrites the current slot only after confirmation, then recalls it and checks every value.";
    }

    private async Task SavePresetAsync(bool confirmedTest=false)
    {
        lastSaveSucceeded=false;UpdateSaveState();
        if(!savePreset.IsEnabled || currentSnapshot is not {} expected || snapshotAddress is not {} target)return;
        if(!confirmedTest && MessageBox.Show(this,
            $"Save British Lead Solo to its existing slot 0D?\n\nNoise Killer Threshold: {expected.Blocks[0].Values[2]}\nAll other original values and the name remain unchanged.\n\nThis overwrites the saved value in that slot.",
            "Confirm preset save",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
        Begin("Checking amplifier before same-slot save…");
        try {
            CurrentPresetSnapshot confirmed;
            confirmed=await UseSessionAsync(target,session=>session.SaveObservedThresholdAsync(expected,operation!.Token));
            currentSnapshot=confirmed;originalSnapshotDetail=confirmed.RawDetailPayloadHex;
            liveEditUnsaved=false;editSnapshotValid=true;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            snapshot.Text=SnapshotLabel();status.Text="Same-slot save and recall verified · 0D · British Lead Solo";
            lastSaveSucceeded=true;
        }
        catch(Exception ex) {
            editSnapshotValid=false;
            status.Text=ex is TransferStateChangedException?
                "Amplifier transfer state changed · Save outcome may be unknown. Wait for a stable amplifier and read it; no automatic retry.":
                "Save not confirmed · Stored outcome may be unknown. Read the amplifier before another action; do not repeat the save automatically.";
            snapshot.Text=SnapshotLabel();
        }
        finally {status.Foreground=MutedBrush;End();}
    }

    // Offline CLI QA only: builds synthetic UI state from a retained baseline.
    // Address zero is not a selected physical device; no transport method is called.
    internal void PrepareSavePreview(string baselinePath)
    {
        using var document=System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(baselinePath));
        var root=document.RootElement;
        var state=Convert.FromHexString(root.GetProperty("current").GetProperty("RawStatePayloadHex").GetString()!);
        var detail=Convert.FromHexString(root.GetProperty("current").GetProperty("RawDetailPayloadHex").GetString()!);
        var version=root.GetProperty("identity").GetProperty("Firmware").GetString()!;
        _=ObservedPresetSaveCommand.CreateUnchanged(version,state,detail);
        void Show(byte[] bytes) {
            var decoded=PresetDataParser.Parse(bytes);
            currentSnapshot=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,Convert.ToHexString(state),Convert.ToHexString(bytes),decoded.Blocks);
            liveEditUnsaved=currentSnapshot.RawDetailPayloadHex!=originalSnapshotDetail;
            UpdateSaveState();
        }
        void RequireDisabled() {UpdateSaveState();if(savePreset.IsEnabled)throw new InvalidOperationException("Unsupported snapshot enabled Save.");}
        snapshotFirmware=version;snapshotAddress=0;editSnapshotValid=true;localSnapshotSource=false;
        originalSnapshotDetail=Convert.ToHexString(detail);Show(detail);RequireDisabled();
        var changed=PresetDataParser.WithParameterValue(detail,1,2,26);Show(changed);
        if(!savePreset.IsEnabled)throw new InvalidOperationException("Supported synthetic edit did not enable Save.");
        foreach(var bytes in new[]{PresetDataParser.WithParameterValue(detail,1,2,27),PresetDataParser.WithParameterValue(changed,1,3,49),
            PresetDataParser.WithParameterValue(changed,2,2,38)}) {Show(bytes);RequireDisabled();}
        Show(changed);localSnapshotSource=true;RequireDisabled();localSnapshotSource=false;
        editSnapshotValid=false;RequireDisabled();editSnapshotValid=true;
        snapshotAddress=null;RequireDisabled();snapshotAddress=0;
        snapshotFirmware="V1.1.2";RequireDisabled();snapshotFirmware=version;
        Begin("Offline busy-state check");RequireDisabled();End();
        if(!savePreset.IsEnabled)throw new InvalidOperationException("Supported synthetic Save did not recover after busy state.");
        sourceBadge.Text="OFFLINE SAVE PREVIEW";firmware.Text=version;
        currentPreset.Text="0D · British Lead Solo";SetEffects(currentSnapshot!.Blocks);
        snapshot.Text="Synthetic Threshold 26 edit · No device connected";
        status.Text="Offline save guard checks passed · No amplifier commands";
        Console.WriteLine("Offline save UI QA passed: supported edit enabled; unsupported content, firmware, local snapshots, invalid targets and busy states disabled.");
    }
}
