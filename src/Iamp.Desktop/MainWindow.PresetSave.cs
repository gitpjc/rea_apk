using System.Windows;
using System.Windows.Controls;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button savePreset=Button("Save / rename preset…");
    private readonly TextBlock saveMessage=Text("Read the amplifier before saving.",12,MutedBrush,new Thickness(0,8,0,0));
    private bool lastSaveSucceeded;
    private bool presetSaveDialogOpen;
    private string? presetSaveNotice;

    private bool SupportedSaveSnapshot()
    {
        if(currentSnapshot is not {} current || snapshotFirmware is not {} version)return false;
        try {
            PresetSaveCommand.ValidateSource(version,Convert.FromHexString(current.RawStatePayloadHex),Convert.FromHexString(current.RawDetailPayloadHex));
            return true;
        }
        catch(Exception ex) when(ex is FormatException or NotSupportedException or System.IO.IOException) {return false;}
    }

    private void UpdateSaveState()
    {
        var supported=!preview && !localSnapshotSource && editSnapshotValid && snapshotAddress.HasValue && SupportedSaveSnapshot();
        savePreset.IsEnabled=supported && operation is null && !presetSaveDialogOpen && !HasPendingInputs();
        UpdatePresetTransferState();
        saveMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            !supported?(presetSaveNotice??(!editSnapshotValid && currentSnapshot is not null?
                "Refresh amplifier to verify its current settings before saving or renaming.":
                "Connect a supported V1.1.9 amplifier before saving or renaming a preset.")):
            HasPendingInputs()?"Apply or clear pending effect and mixer inputs before saving.":
            presetSaveNotice??"Save your current amplifier settings, rename this preset, or save a copy to another slot. The destination is confirmed before overwriting.";
    }

    private async Task SavePresetAsync(bool confirmedTest=false)
    {
        lastSaveSucceeded=false;UpdateSaveState();
        if(!savePreset.IsEnabled || currentSnapshot is not {} expected || snapshotAddress is null)return;
        if(confirmedTest){await SavePresetToAsync((byte)expected.WireIndex,expected.Name,true);return;}
        presetSaveDialogOpen=true;UpdateSaveState();
        PresetSaveChoice? choice=null;
        try {var dialog=CreatePresetSaveDialog(expected);if(dialog.ShowDialog()==true)choice=dialog.Tag as PresetSaveChoice;}
        finally {presetSaveDialogOpen=false;UpdateSaveState();}
        if(choice is not null) {
            if(ReferenceEquals(expected,currentSnapshot))await SavePresetToAsync(choice.Destination,choice.Name);
            else {presetSaveNotice="The current preset changed while the dialog was open. Review it and save again; nothing was written.";UpdateSaveState();}
        }
    }

    private async Task SavePresetToAsync(byte destination,string name,bool confirmedTest=false)
    {
        lastSaveSucceeded=false;UpdateSaveState();
        if(!savePreset.IsEnabled || currentSnapshot is not {} expected || snapshotAddress is not {} target)return;
        try {_=PresetSaveCommand.EncodeName(name);}
        catch(ArgumentException){status.Text="Enter a valid preset name before saving.";return;}
        presetSaveNotice=null;Begin("Checking amplifier and save destination…");
        var writeWorkflowStarted=false;
        var recover=false;string? failureMessage=null;
        try {
            var proof=await UseSessionAsync(target,session=>session.ReadPresetSaveDestinationAsync(expected,destination,operation!.Token));
            if(!confirmedTest && !ConfirmPresetOverwrite(expected,proof,name)){status.Text="Save cancelled · Nothing saved.";return;}
            status.Text="Saving preset once and verifying its complete readback…";
            writeWorkflowStarted=true;
            var confirmed=await UseSessionAsync(target,session=>session.SavePresetAsync(expected,proof,name,operation!.Token));
            DecodedPreset? storedSource=null;var sourceIndexConfirmed=true;
            if(confirmed.WireIndex!=expected.WireIndex) {
                try {
                    // The active source index contained unsaved live effects. Once Save As
                    // leaves it, refresh its stored chain rather than persisting that live copy.
                    var source=await UseSessionAsync(target,session=>session.ReadPresetSaveDestinationAsync(confirmed,(byte)expected.WireIndex,operation!.Token));
                    storedSource=PresetDataParser.Parse(Convert.FromHexString(source.RawDetailPayloadHex));
                }
                catch(Exception) {sourceIndexConfirmed=false;await ReleaseConnectionAsync();}
            }
            currentSnapshot=confirmed;originalSnapshotDetail=confirmed.RawDetailPayloadHex;
            liveEditUnsaved=false;editSnapshotValid=sourceIndexConfirmed;snapshotTime=DateTimeOffset.Now;
            UpdateSavedPresetLibrary(confirmed,storedSource,sourceIndexConfirmed);
            if(!sourceIndexConfirmed) {
                presetEffectIndex=presetEffectIndex.Where(p=>p.Key!=expected.WireIndex).ToDictionary(p=>p.Key,p=>p.Value);
                effectCacheStore.Invalidate(target,snapshotFirmware!);
                effectCacheNote.Text="Saved · Reconnect to refresh the source preset's effect index";
            }
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            UpdateMixerState(resetPending:true);Filter();
            snapshot.Text=SnapshotLabel();status.Text=sourceIndexConfirmed?$"Preset save and recall verified · {confirmed.SlotLabel} · {confirmed.Name}":
                $"Preset saved and verified · {confirmed.SlotLabel} · Reconnect to refresh the source effect index.";
            lastSaveSucceeded=true;
        }
        catch(Exception ex) {
            editSnapshotValid=false;
            failureMessage=!writeWorkflowStarted || ex is PresetSavePreconditionException?
                ex is PresetSavePreconditionException?"Save stopped before writing · The amplifier changed. Nothing was saved. Review its refreshed settings and destination.":
                "Save stopped before writing · The connection check failed. Nothing was saved.":ex is TransferStateChangedException?
                "Amplifier transfer state changed · Save outcome may be unknown. Wait for a stable amplifier and read it; no automatic retry.":
                "Save not confirmed · Stored outcome may be unknown. Read the amplifier before another action; do not repeat the save automatically.";
            recover=!closing && ex is not TransferStateChangedException && !operation!.IsCancellationRequested;
            // A failed workflow is terminal. Discard it before any further reads;
            // recovery must never replay the save or recall the destination.
            await ReleaseConnectionAsync();status.Text=failureMessage;
            snapshot.Text=SnapshotLabel();
        }
        finally {status.Foreground=MutedBrush;End();}
        if(failureMessage is not null) {
            if(recover && TryReadTarget(out var selected) && selected==target)await ReadAsync();
            presetSaveNotice=failureMessage+(editSnapshotValid?" Current amplifier settings have been reloaded; no save was retried.":" Use Connect / Refresh amplifier to recover.");
            status.Text=presetSaveNotice;UpdateSaveState();
            // Keep the outcome beside Save too, including when reconnection fails.
            if(!editSnapshotValid)saveMessage.Text=presetSaveNotice;
        }
    }

    private bool ConfirmPresetOverwrite(CurrentPresetSnapshot expected,PresetSaveDestination proof,string name)
    {
        operation!.Token.ThrowIfCancellationRequested();
        // Human review time must not consume the Bluetooth operation deadline.
        operation.CancelAfter(Timeout.InfiniteTimeSpan);
        try {return CreatePresetOverwriteDialog(expected,proof,name).ShowDialog()==true;}
        finally {operation.CancelAfter(TimeSpan.FromSeconds(75));}
    }

    private void UpdateSavedPresetLibrary(CurrentPresetSnapshot confirmed,DecodedPreset? storedSource=null,bool persistCache=true)
    {
        names=names.Where(n=>n.WireIndex!=confirmed.WireIndex).Append(new PresetName(confirmed.WireIndex,confirmed.Name)).OrderBy(n=>n.WireIndex).ToArray();
        var updated=presetEffectIndex.ToDictionary(p=>p.Key,p=>p.Value);updated[confirmed.WireIndex]=confirmed.Blocks;presetEffectIndex=updated;
        if(storedSource is not null) {
            names=names.Where(n=>n.WireIndex!=storedSource.WireIndex).Append(new PresetName(storedSource.WireIndex,storedSource.Name)).OrderBy(n=>n.WireIndex).ToArray();
            updated[storedSource.WireIndex]=storedSource.Blocks;
        }
        if(persistCache)CacheLivePresetNames();
        if(persistCache && snapshotAddress is {} target && snapshotFirmware is {} version && names.Count==80 && presetEffectIndex.Count==80) {
            var records=names.Select(n=>new DecodedPreset((byte)n.WireIndex,n.Name,presetEffectIndex[n.WireIndex],Array.Empty<byte>()));
            effectCacheNote.Text=effectCacheStore.Save(target,version,records)?"Effects cached · Saved preset updated":"Preset saved · Effect cache could not be updated";
        }
    }

    // Offline CLI QA only: builds synthetic UI state from a retained baseline.
    // A synthetic address is used; no transport method is called.
    internal void PrepareSavePreview(string baselinePath)
    {
        using var document=System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(baselinePath));
        var root=document.RootElement;
        var state=Convert.FromHexString(root.GetProperty("current").GetProperty("RawStatePayloadHex").GetString()!);
        var detail=Convert.FromHexString(root.GetProperty("current").GetProperty("RawDetailPayloadHex").GetString()!);
        var version=root.GetProperty("identity").GetProperty("Firmware").GetString()!;
        PresetSaveCommand.ValidateSource(version,state,detail);
        void Show(byte[] bytes) {
            var decoded=PresetDataParser.Parse(bytes);
            currentSnapshot=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,Convert.ToHexString(state),Convert.ToHexString(bytes),decoded.Blocks);
            liveEditUnsaved=currentSnapshot.RawDetailPayloadHex!=originalSnapshotDetail;
            SetEffects(decoded.Blocks);
            UpdateSaveState();
        }
        void RequireDisabled() {UpdateSaveState();if(savePreset.IsEnabled)throw new InvalidOperationException("Unsupported snapshot enabled Save.");}
        snapshotFirmware=version;snapshotAddress=1;editSnapshotValid=true;localSnapshotSource=false;
        originalSnapshotDetail=Convert.ToHexString(detail);Show(detail);
        if(!savePreset.IsEnabled)throw new InvalidOperationException("Unchanged supported preset did not enable rename/save-as.");
        var changed=PresetDataParser.WithParameterValue(detail,1,2,26);Show(changed);
        if(!savePreset.IsEnabled)throw new InvalidOperationException("Supported synthetic edit did not enable Save.");
        foreach(var bytes in new[]{PresetDataParser.WithParameterValue(detail,1,2,27),PresetDataParser.WithParameterValue(changed,1,3,49),
            PresetDataParser.WithParameterValue(changed,2,2,38)}) {Show(bytes);if(!savePreset.IsEnabled)throw new InvalidOperationException("Verified general parameter edits did not enable saving.");}
        thresholdInput.Text="24";RequireDisabled();Show(changed);
        Show(changed);localSnapshotSource=true;RequireDisabled();localSnapshotSource=false;
        editSnapshotValid=false;RequireDisabled();editSnapshotValid=true;
        snapshotAddress=null;RequireDisabled();snapshotAddress=1;
        snapshotFirmware="V1.1.2";RequireDisabled();snapshotFirmware=version;
        Begin("Offline busy-state check");RequireDisabled();End();
        if(!savePreset.IsEnabled)throw new InvalidOperationException("Supported synthetic Save did not recover after busy state.");
        firmware.Text=version;
        currentPreset.Text="0D · British Lead Solo";SetEffects(currentSnapshot!.Blocks);
        snapshot.Text="Synthetic general save/rename state · No device connected";
        status.Text="Offline save guard checks passed · No amplifier commands";
        Console.WriteLine("Offline save UI QA passed: unchanged rename/save-as and verified parameter edits enabled; pending inputs, unsupported firmware, local snapshots, invalid targets and busy states disabled.");
    }
}
