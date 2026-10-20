using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Iamp.Protocol;
using Iamp.Transport.Windows;
using Microsoft.Win32;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button exportSnapshot=Button("Export this snapshot…"),openSnapshot=Button("Open snapshot…"),captureSnapshot=Button("Capture current preset"),snapshotsNavigation=Button("Snapshots");
    private readonly ScrollViewer snapshotsScroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed};
    private readonly StackPanel snapshotView=new();
    private readonly TextBlock snapshotContext=Text("Capture the connected amplifier’s current preset or open a snapshot file.",13,MutedBrush);
    private SnapshotArchive? viewedSnapshot;
    private string? viewedSnapshotFile;
    private ulong? viewedSnapshotAddress;
    // Retained for isolated legacy diagnostic/editor preview guards. File viewing no longer sets this flag.
    private bool localSnapshotSource;
    private const int MaximumSnapshotFileBytes=64*1024;
    private const string SnapshotFilter="iAMP snapshots (Snapshot-*.json;*-snapshot.json;*.iamp-snapshot.json)|Snapshot-*.json;*-snapshot.json;*.iamp-snapshot.json|JSON files (*.json)|*.json";

    private void BuildSnapshotsPage()
    {
        var page=new StackPanel{Margin=new Thickness(32,0,32,24)};page.Children.Add(Text("Snapshots",30));
        page.Children.Add(Text("Capture one preset for offline viewing and comparison. Use Save / rename preset on Presets to store settings on the amplifier.",13,MutedBrush,new Thickness(0,7,0,12)));
        var actions=new WrapPanel();
        foreach(var button in new[]{captureSnapshot,openSnapshot,exportSnapshot,compareSnapshot}){button.Margin=new Thickness(0,0,8,8);actions.Children.Add(button);}
        page.Children.Add(actions);snapshotContext.Margin=new Thickness(0,4,0,12);page.Children.Add(snapshotContext);page.Children.Add(snapshotView);snapshotsScroll.Content=page;
        captureSnapshot.Click+=async(_,_)=>await CaptureSnapshotAsync();
    }
    private void InitializeSnapshotArchive()
    {
        exportSnapshot.Click+=async(_,_)=>await ChooseExportSnapshotAsync();
        openSnapshot.Click+=async(_,_)=>await ChooseOpenSnapshotAsync();UpdateSnapshotArchiveState();
    }
    private void UpdateSnapshotArchiveState()
    {
        showPresetJson.IsEnabled=currentSnapshot is not null&&operation is null;
        exportSnapshot.IsEnabled=!preview&&operation is null&&viewedSnapshot is not null;
        openSnapshot.IsEnabled=!preview&&operation is null;
        captureSnapshot.IsEnabled=!preview&&operation is null&&connectedSession is {IsAvailable:true}&&snapshotFirmware is not null&&!tunerActive&&!tunerStarting&&!drumReady&&!drumStarting;
        captureSnapshot.ToolTip=captureSnapshot.IsEnabled?"Read the amplifier into a separate, fixed snapshot. Pending editor inputs are excluded.":"Connect an amplifier in the header and stop active Tools before capturing.";
        exportSnapshot.ToolTip=viewedSnapshot is null?"Capture or open a snapshot first.":"Export the snapshot shown below, without refreshing the amplifier.";
        UpdateComparisonState();UpdateSharedHeader();
    }
    private void UpdateSnapshotContext()
    {
        if(viewedSnapshot is not {} shown){snapshotContext.Text="No snapshot being viewed. Capture the connected amplifier’s current preset or open a file.";return;}
        var source=viewedSnapshotFile is {} file?$"File · {file}":$"Amplifier capture · Bluetooth {viewedSnapshotAddress:X12}";
        var changed=viewedSnapshotFile is null&&observedAmplifierPreset is {} actual&&
            (actual.WireIndex!=shown.Preset.WireIndex||actual.RawDetailPayloadHex!=Convert.ToHexString(shown.DetailPayload)||
             (connectedAddress.HasValue&&connectedAddress!=viewedSnapshotAddress));
        snapshotContext.Text=$"Viewing {PresetAddress.FromWireIndex(shown.Preset.WireIndex).Label} · {shown.Preset.Name}\n{source} · Captured {shown.CapturedAt.ToLocalTime():dd MMM yyyy, HH:mm:ss}\n"+
            (changed?"The amplifier has changed since this snapshot was captured. Capture again to update it.":"This is a fixed capture. Opening, exporting or comparing it does not change the amplifier.")+
            (viewedSnapshotFile is null?" Unapplied editor inputs are excluded.":"");
    }
    private void SetViewedSnapshot(SnapshotArchive archive,string? fileName,ulong? target)
    {
        viewedSnapshot=archive;viewedSnapshotFile=fileName;viewedSnapshotAddress=target;snapshotView.Children.Clear();
        var column=(ScrollViewer)CreateComparisonPresetColumn("SNAPSHOT BEING VIEWED",archive,archive,null);
        var content=column.Content;column.Content=null;snapshotView.Children.Add((UIElement)content);
        UpdateSnapshotArchiveState();
    }
    private async Task CaptureSnapshotAsync()
    {
        if(!captureSnapshot.IsEnabled||connectedSession is not {} session||snapshotFirmware is not {} version)return;
        var target=connectedAddress;Begin("Capturing current amplifier preset…");
        try {
            while(polling)await Task.Delay(40,operation!.Token);
            if(connectedSession!=session||!session.IsAvailable)throw new IOException("Connection changed.");
            var actual=await session.ReadCurrentPresetAsync(operation!.Token);
            var archive=SnapshotArchiveCodec.Decode(SnapshotArchiveCodec.Encode(version,DateTimeOffset.UtcNow,Convert.FromHexString(actual.RawStatePayloadHex),Convert.FromHexString(actual.RawDetailPayloadHex)));
            operation.Token.ThrowIfCancellationRequested();observedAmplifierPreset=actual;
            if(currentSnapshot is {} editor&&(editor.RawStatePayloadHex!=actual.RawStatePayloadHex||editor.RawDetailPayloadHex!=actual.RawDetailPayloadHex))
            {editSnapshotValid=false;refreshTimer.Stop();}
            SetViewedSnapshot(archive,null,target);
            status.Text="Snapshot captured · Preset editor and pending inputs preserved";
        }
        catch(OperationCanceledException){status.Text="Capture cancelled · Previous snapshot preserved";}
        catch(Exception){status.Text="Could not capture the preset. Check the connection; previous snapshot preserved.";}
        finally{status.Foreground=MutedBrush;End();}
    }
    private async Task ChooseExportSnapshotAsync()
    {
        if(!exportSnapshot.IsEnabled||viewedSnapshot is not {} archive)return;
        var dialog=CreateSnapshotSavePicker(ToCurrentSnapshot(archive));
        if(dialog.ShowDialog(this)==true)await ExportArchiveToPathAsync(archive,dialog.FileName);
    }
    private static CurrentPresetSnapshot ToCurrentSnapshot(SnapshotArchive archive)=>new(archive.Preset.WireIndex,archive.Preset.Name,Convert.ToHexString(archive.StatePayload),Convert.ToHexString(archive.DetailPayload),archive.Preset.Blocks);
    private static SaveFileDialog CreateSnapshotSavePicker(CurrentPresetSnapshot current)=>new(){Filter=SnapshotFilter,DefaultExt=".json",AddExtension=true,OverwritePrompt=true,Title="Export this snapshot",FileName=CreateSnapshotFileName(current)};
    private static string CreateSnapshotFileName(CurrentPresetSnapshot current)
    {
        var name=System.Text.RegularExpressions.Regex.Replace(current.Name,@"[\s<>:""/\\|?*\x00-\x1F]+","-").Trim('-','.');
        name=System.Text.RegularExpressions.Regex.Replace(name,"-{2,}","-");if(name.Length>120)name=name[..120].TrimEnd('-','.');if(string.IsNullOrWhiteSpace(name))name="Unnamed";
        return $"Snapshot-{current.SlotLabel}-{name}_{DateTime.Now.ToString("yyyy-MM-dd_HHmm",System.Globalization.CultureInfo.InvariantCulture)}.json";
    }
    // Developer evidence exports explicitly retain the editor's latest confirmed capture.
    private Task ExportSnapshotToPathAsync(string path)=>currentSnapshot is {} current&&snapshotFirmware is {} version&&snapshotTime is {} time?
        ExportArchiveToPathAsync(SnapshotArchiveCodec.Decode(SnapshotArchiveCodec.Encode(version,time,Convert.FromHexString(current.RawStatePayloadHex),Convert.FromHexString(current.RawDetailPayloadHex))),path):Task.CompletedTask;
    private async Task ExportArchiveToPathAsync(SnapshotArchive archive,string path)
    {
        if(preview||operation is not null)return;Begin("Exporting snapshot…");string? temporary=null;
        try {
            var json=SnapshotArchiveCodec.Encode(archive.Firmware,archive.CapturedAt,archive.StatePayload,archive.DetailPayload);
            var destination=Path.GetFullPath(path);temporary=destination+$".{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(temporary,json,new UTF8Encoding(false),operation!.Token);operation.Token.ThrowIfCancellationRequested();
            File.Move(temporary,destination,true);temporary=null;status.Text="Snapshot exported · No amplifier changes";
        }
        catch(OperationCanceledException){status.Text="Export cancelled · No amplifier changes";}
        catch(Exception){status.Text="Could not export the snapshot. Check the destination and try again.";}
        finally{if(temporary is not null)try{File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}status.Foreground=MutedBrush;End();}
    }
    private async Task ChooseOpenSnapshotAsync()
    {
        if(!openSnapshot.IsEnabled)return;
        var dialog=new OpenFileDialog{Filter=SnapshotFilter,DefaultExt=".json",CheckFileExists=true,Multiselect=false,Title="Open snapshot for viewing"};
        if(dialog.ShowDialog(this)==true)await LoadSnapshotFromPathAsync(dialog.FileName);
    }
    private static async Task<SnapshotArchive> ReadArchiveFileAsync(string path,CancellationToken token)
    {
        await using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,4096,true);
        var bytes=new byte[MaximumSnapshotFileBytes+1];var count=0;
        while(count<bytes.Length){var read=await input.ReadAsync(bytes.AsMemory(count),token);if(read==0)break;count+=read;}
        if(count>MaximumSnapshotFileBytes)throw new FormatException("Snapshot exceeds 64 KiB.");
        var start=count>=3&&bytes[0]==0xEF&&bytes[1]==0xBB&&bytes[2]==0xBF?3:0;
        return SnapshotArchiveCodec.Decode(new UTF8Encoding(false,true).GetString(bytes,start,count-start));
    }
    private async Task<bool> LoadSnapshotFromPathAsync(string path)
    {
        if(preview||operation is not null)return false;Begin("Opening snapshot for viewing…");
        try {
            var archive=await ReadArchiveFileAsync(path,operation!.Token);operation.Token.ThrowIfCancellationRequested();
            SetViewedSnapshot(archive,Path.GetFileName(path),null);status.Text="Snapshot opened · Amplifier connection and preset editor unchanged";return true;
        }
        catch(OperationCanceledException){status.Text="Open cancelled · Previous snapshot preserved";return false;}
        catch(Exception){status.Text="Could not open this snapshot. Use a valid file no larger than 64 KiB. Previous snapshot preserved.";return false;}
        finally{status.Foreground=MutedBrush;End();}
    }
    internal async Task<bool> LoadSnapshotPreviewAsync(string path,string outputPath)
    {
        var editor=currentSnapshot;var originalNames=names;
        if(!await LoadSnapshotFromPathAsync(path))return false;
        if(!ReferenceEquals(editor,currentSnapshot)||!ReferenceEquals(names,originalNames))throw new InvalidOperationException("Snapshot viewing changed the preset editor.");
        await NavigateToPageAsync(snapshotsScroll);
        Console.WriteLine("Snapshot preview passed: independent file view; editor and preset names preserved.");return true;
    }
}
