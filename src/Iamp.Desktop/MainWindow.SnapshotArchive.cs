using System.IO;
using System.Text;
using Iamp.Protocol;
using Iamp.Transport.Windows;
using Microsoft.Win32;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly System.Windows.Controls.Button exportSnapshot=Button("Export snapshot");
    private readonly System.Windows.Controls.Button openSnapshot=Button("Open snapshot");
    private bool localSnapshotSource;
    private const int MaximumSnapshotFileBytes=64*1024;
    private const string SnapshotFilter="iAMP snapshots (*-snapshot.json;*.iamp-snapshot.json)|*-snapshot.json;*.iamp-snapshot.json|JSON files (*.json)|*.json";

    private void InitializeSnapshotArchive()
    {
        exportSnapshot.Click+=async(_,_)=>await ChooseExportSnapshotAsync();
        openSnapshot.Click+=async(_,_)=>await ChooseOpenSnapshotAsync();
        UpdateSnapshotArchiveState();
    }
    private void UpdateSnapshotArchiveState()
    {
        showPresetJson.IsEnabled=currentSnapshot is not null && operation is null;
        exportSnapshot.IsEnabled=!preview && operation is null && currentSnapshot is not null && snapshotFirmware is not null && snapshotTime.HasValue;
        openSnapshot.IsEnabled=!preview && operation is null;
    }
    private async Task ChooseExportSnapshotAsync()
    {
        if(!exportSnapshot.IsEnabled || currentSnapshot is not {} current)return;
        var dialog=CreateSnapshotSavePicker(current);
        if(dialog.ShowDialog(this)!=true)return;
        await ExportSnapshotToPathAsync(dialog.FileName);
    }
    private static SaveFileDialog CreateSnapshotSavePicker(CurrentPresetSnapshot current)=>new()
    {
        Filter=SnapshotFilter,DefaultExt=".json",AddExtension=true,OverwritePrompt=true,Title="Export local snapshot",
        FileName=CreateSnapshotFileName(current)
    };
    private static string CreateSnapshotFileName(CurrentPresetSnapshot current)
    {
        var name=System.Text.RegularExpressions.Regex.Replace(current.Name,@"[\s<>:""/\\|?*\x00-\x1F]+","-").Trim('-','.');
        name=System.Text.RegularExpressions.Regex.Replace(name,"-{2,}","-");
        if(name.Length>120)name=name[..120].TrimEnd('-','.');
        if(string.IsNullOrWhiteSpace(name))name="Unnamed";
        return $"Preset-{current.SlotLabel}-{name}-snapshot.json";
    }
    private async Task ExportSnapshotToPathAsync(string path)
    {
        if(preview || operation is not null || currentSnapshot is not {} current || snapshotFirmware is not {} version || snapshotTime is not {} capturedAt)return;
        Begin("Exporting local snapshot…");
        string? temporary=null;
        try {
            var json=SnapshotArchiveCodec.Encode(version,capturedAt,Convert.FromHexString(current.RawStatePayloadHex),Convert.FromHexString(current.RawDetailPayloadHex));
            var destination=Path.GetFullPath(path);temporary=destination+$".{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(temporary,json,new UTF8Encoding(false),operation!.Token);
            operation.Token.ThrowIfCancellationRequested();
            File.Move(temporary,destination,true);temporary=null;
            status.Text="Snapshot exported locally · No amplifier changes";
        }
        catch(OperationCanceledException) {status.Text="Snapshot export cancelled · No amplifier changes";}
        catch(Exception) {status.Text="Could not export the snapshot. Check the destination and try again.";}
        finally {
            if(temporary is not null)try {File.Delete(temporary);}catch(IOException) {}catch(UnauthorizedAccessException) {}
            status.Foreground=MutedBrush;End();
        }
    }
    private async Task ChooseOpenSnapshotAsync()
    {
        if(!openSnapshot.IsEnabled)return;
        var dialog=new OpenFileDialog {Filter=SnapshotFilter,DefaultExt=".iamp-snapshot.json",CheckFileExists=true,Multiselect=false,Title="Open local snapshot"};
        if(dialog.ShowDialog(this)==true)await LoadSnapshotFromPathAsync(dialog.FileName);
    }
    /// <summary>Offline QA entry; uses the same bounded decoder and UI commit as the Open snapshot button.</summary>
    internal async Task<bool> LoadSnapshotPreviewAsync(string path,string outputPath)
    {
        if(!await LoadSnapshotFromPathAsync(path))return false;
        var original=currentSnapshot!;var originalFirmware=snapshotFirmware;var originalTime=snapshotTime;
        var originalDetail=originalSnapshotDetail;var originalBadge=sourceBadge.Text;
        var originalNames=names;var originalCurrentLabel=currentPreset.Text;
        var directory=Path.GetDirectoryName(Path.GetFullPath(outputPath))!;Directory.CreateDirectory(directory);
        var prefix=Path.Combine(directory,$".snapshot-qa-{Guid.NewGuid():N}");
        var invalid=prefix+".invalid.json";var exported=prefix+".iamp-snapshot.json";
        try {
            RequireLocalArchiveReadOnly();
            await File.WriteAllTextAsync(invalid,"{}");
            if(await LoadSnapshotFromPathAsync(invalid))throw new InvalidOperationException("Malformed local archive was accepted.");
            if(currentSnapshot?.RawStatePayloadHex!=original.RawStatePayloadHex || currentSnapshot.RawDetailPayloadHex!=original.RawDetailPayloadHex ||
                snapshotFirmware!=originalFirmware || snapshotTime!=originalTime || originalSnapshotDetail!=originalDetail ||
                sourceBadge.Text!=originalBadge || !ReferenceEquals(names,originalNames) || currentPreset.Text!=originalCurrentLabel ||
                presetCount.Text!="—" || libraryCount.Text!="—" || rhythmCount.Text!="—")
                throw new InvalidOperationException("Failed archive load changed the previous snapshot or metadata.");
            RequireLocalArchiveReadOnly();
            await ExportSnapshotToPathAsync(exported);
            var decoded=SnapshotArchiveCodec.Decode(await File.ReadAllTextAsync(exported));
            if(decoded.Firmware!=originalFirmware || decoded.CapturedAt!=originalTime ||
                Convert.ToHexString(decoded.StatePayload)!=original.RawStatePayloadHex || Convert.ToHexString(decoded.DetailPayload)!=original.RawDetailPayloadHex)
                throw new InvalidOperationException("Actual archive export did not preserve the loaded snapshot bytes/metadata.");
            RequireLocalArchiveReadOnly();
            snapshot.Text=SnapshotLabel();status.Text="LOCAL SNAPSHOT · OFFLINE · Import/export checks passed · No amplifier changes";
            Console.WriteLine("Snapshot preview QA passed: all writes disabled; malformed load preserved state; actual export preserved raw bytes and metadata.");
            return true;
        }
        finally {
            File.Delete(invalid);File.Delete(exported);
        }
    }
    private void RequireLocalArchiveReadOnly()
    {
        if(!localSnapshotSource || snapshotAddress is not null || editSnapshotValid || liveEditUnsaved ||
            applyThreshold.IsEnabled || applyTone.IsEnabled || noiseLevelEditor.Apply.IsEnabled || gainEditor.Apply.IsEnabled ||
            volumeEditor.Apply.IsEnabled || selectPreset.IsEnabled || savePreset.IsEnabled)
            throw new InvalidOperationException("Imported archive enabled a hardware mutation or retained a hardware target.");
    }

    private async Task<bool> LoadSnapshotFromPathAsync(string path)
    {
        if(preview || operation is not null)return false;
        Begin("Opening local snapshot…");
        await ReleaseConnectionAsync();
        try {
            if(new FileInfo(path).Length>MaximumSnapshotFileBytes)throw new FormatException("Snapshot exceeds the 64 KiB local budget.");
            // A second size check and bounded buffer prevent growth between FileInfo and reading from bypassing the budget.
            string json;
            {
                await using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,4096,true);
                if(input.Length>MaximumSnapshotFileBytes)throw new FormatException("Snapshot exceeds the 64 KiB local budget.");
                var bytes=new byte[MaximumSnapshotFileBytes+1];var count=0;
                while(count<bytes.Length) {
                    var read=await input.ReadAsync(bytes.AsMemory(count),operation!.Token);
                    if(read==0)break;
                    count+=read;
                }
                if(count>MaximumSnapshotFileBytes)throw new FormatException("Snapshot exceeds the 64 KiB local budget.");
                var start=count>=3 && bytes[0]==0xEF && bytes[1]==0xBB && bytes[2]==0xBF?3:0;
                json=new UTF8Encoding(false,true).GetString(bytes,start,count-start);
            }
            var decoded=SnapshotArchiveCodec.Decode(json);
            var loaded=new CurrentPresetSnapshot(decoded.Preset.WireIndex,decoded.Preset.Name,
                Convert.ToHexString(decoded.StatePayload),Convert.ToHexString(decoded.DetailPayload),decoded.Preset.Blocks);
            operation!.Token.ThrowIfCancellationRequested();
            // No awaits after decoding: commit the validated local snapshot together on the UI thread.
            currentSnapshot=loaded;snapshotFirmware=decoded.Firmware;snapshotTime=decoded.CapturedAt;originalSnapshotDetail=loaded.RawDetailPayloadHex;
            snapshotAddress=null;editSnapshotValid=false;liveEditUnsaved=false;localSnapshotSource=true;lastReadSucceeded=false;
            UpdateMixerState(resetPending:true);
            sourceBadge.Text="LOCAL SNAPSHOT";firmware.Text=decoded.Firmware;
            batterySummary.Text="Battery · Not included in local snapshots";batteryReadAt=null;
            presetCount.Text=libraryCount.Text=rhythmCount.Text="—";
            SetPresetEffectIndex(new[]{new DecodedPreset((byte)loaded.WireIndex,loaded.Name,loaded.Blocks,Array.Empty<byte>())});
            effectCacheNote.Text="Local snapshot · One preset";
            names=new[]{new PresetName(loaded.WireIndex,loaded.Name)};presetNamesAreCached=false;Filter();
            currentPreset.Text=$"{loaded.SlotLabel} · {loaded.Name}";SetEffects(loaded.Blocks);
            snapshot.Text=SnapshotLabel();status.Text="LOCAL SNAPSHOT · OFFLINE · Read the amplifier before editing · No amplifier changes";
            return true;
        }
        catch(OperationCanceledException) {status.Text="Snapshot open cancelled · Previous snapshot preserved";return false;}
        catch(Exception) {status.Text="Could not open this snapshot. File must be valid and no larger than 64 KiB. Previous snapshot preserved.";return false;}
        finally {status.Foreground=MutedBrush;End();}
    }
}
