using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Iamp.Protocol;
using Iamp.Transport.Windows;
using Microsoft.Win32;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button exportPreset=Button("Export"),importPreset=Button("Import");
    private PresetImportDialogController? activePresetImportDialog;
    // Offline UI checks can supply a chosen path without automating the native file picker.
    private Func<OpenFileDialog,bool?>? presetImportFilePicker=null;
    private readonly StackPanel importRecoveryPanel=new(){Visibility=Visibility.Collapsed,Margin=new Thickness(0,12,0,0)};
    private readonly TextBlock importRecoverySummary=Text("",13),importRecoveryDetails=Text("",12,MutedBrush);
    private readonly Button inspectImport=Button("Reconnect and inspect"),dismissImportWarning=Button("Dismiss notice");
    private ulong? importRecoveryTarget;
    private bool importRecoveryReadConfirmed;

    private void BuildImportRecoveryPanel()
    {
        importRecoveryPanel.Children.Add(Text("Import did not complete",18,Brush("#FFA45C")));
        importRecoverySummary.Margin=new Thickness(0,6,0,8);importRecoveryPanel.Children.Add(importRecoverySummary);
        var actions=new WrapPanel();actions.Children.Add(inspectImport);dismissImportWarning.Margin=new Thickness(10,0,0,0);actions.Children.Add(dismissImportWarning);importRecoveryPanel.Children.Add(actions);
        var details=new Expander{Header="Failure details",Content=importRecoveryDetails,Margin=new Thickness(0,8,0,0)};importRecoveryPanel.Children.Add(details);
        inspectImport.Click+=async(_,_)=>await InspectFailedImportAsync();
        dismissImportWarning.Click+=(_,_)=>{importRecoveryPanel.Visibility=Visibility.Collapsed;importRecoveryTarget=null;};
    }

    private void UpdateImportRecoveryControls()
    {
        var idle=operation is null&&!preview;
        inspectImport.IsEnabled=idle&&importRecoveryTarget.HasValue&&TryReadTarget(out var selected)&&selected==importRecoveryTarget;
        inspectImport.Content=importRecoveryReadConfirmed?"Read current settings":"Reconnect and inspect";
        inspectImport.ToolTip="Read the amplifier’s actual settings without recalling or saving a preset.";
        dismissImportWarning.IsEnabled=idle&&importRecoveryReadConfirmed;
        dismissImportWarning.ToolTip="Read and review the current amplifier settings before dismissing this notice.";
    }

    private void UpdateImportRecoveryAfterRead(bool succeeded,ulong target)
    {
        if(importRecoveryPanel.Visibility!=Visibility.Visible||importRecoveryTarget!=target)return;
        importRecoveryReadConfirmed=succeeded;
        importRecoverySummary.Text=succeeded&&currentSnapshot is {} current?
            $"Current settings have been read: {current.SlotLabel} · {current.Name}. The import remains incomplete. Review the effects shown below before saving or starting another import.":
            "Could not read the amplifier. Turn it on, disconnect phone iAMP, then choose Reconnect and inspect. Nothing will be saved by this check.";
        UpdateImportRecoveryControls();
    }

    private async Task<string> InspectFailedImportAsync()
    {
        if(operation is not null||importRecoveryTarget is not {} target||!TryReadTarget(out var selected)||selected!=target)
            return "Select the amplifier used for the import in the header, then choose Reconnect and inspect.";
        await ReadAsync();return importRecoverySummary.Text;
    }

    private void UpdatePresetTransferState()
    {
        var ready=!preview&&!localSnapshotSource&&editSnapshotValid&&snapshotAddress.HasValue&&
            operation is null&&!presetSaveDialogOpen&&!HasPendingInputs()&&!tunerActive&&!tunerStarting&&!drumReady&&!drumStarting;
        try {if(currentSnapshot is not {} current)ready=false;else {
            PresetSaveCommand.ValidateSource(snapshotFirmware??"",Convert.FromHexString(current.RawStatePayloadHex),Convert.FromHexString(current.RawDetailPayloadHex));
            _=PresetTransferRecord.Validate(snapshotFirmware??"",Convert.FromHexString(current.RawDetailPayloadHex));
        }}catch(Exception ex) when(ex is FormatException or NotSupportedException or ArgumentException or IOException){ready=false;}
        exportPreset.IsEnabled=ready;importPreset.IsEnabled=ready;
        exportPreset.ToolTip=ready?"Export the current preset’s name, effect chain and complete settings.":"Connect and refresh a supported amplifier; apply or clear pending inputs first.";
        importPreset.ToolTip=ready?"Import a complete preset into the active slot, with overwrite confirmation.":exportPreset.ToolTip;
        UpdateImportRecoveryControls();
    }

    private static SaveFileDialog CreatePresetExportPicker(string fileName)=>new(){Title="Export preset",Filter="iAMP preset JSON (*.json)|*.json",DefaultExt=".json",AddExtension=true,OverwritePrompt=true,FileName=fileName};

    private async Task ChooseExportPresetAsync()
    {
        UpdatePresetTransferState();if(!exportPreset.IsEnabled||currentSnapshot is not {} captured)return;
        var json=PresetJsonDocument.Create(captured,snapshotFirmware,snapshotTime,"amplifierSnapshot",true,false);
        // Validate the complete file before presenting the save location.
        try {_=PresetJsonDocument.ParseImport(json);}
        catch(Exception ex) when(ex is FormatException or NotSupportedException or ArgumentException){presetSaveNotice=ex.Message;UpdateSaveState();return;}
        presetSaveDialogOpen=true;UpdateSaveState();
        try {
            var picker=CreatePresetExportPicker(PresetJsonDocument.DefaultFileName(captured));
            if(picker.ShowDialog(this)!=true)return;
            await PresetJsonDocument.SaveAsync(picker.FileName,json);
            presetSaveNotice=$"Exported {captured.SlotLabel} · {captured.Name}.";
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException){presetSaveNotice="Could not export the preset. Check the file location and try again.";}
        finally {presetSaveDialogOpen=false;UpdateSaveState();}
    }

    private async Task ChooseImportPresetAsync()
    {
        UpdatePresetTransferState();if(!importPreset.IsEnabled||currentSnapshot is not {} expected||snapshotAddress is not {} target)return;
        byte[] detail;
        presetSaveNotice=null;presetSaveDialogOpen=true;UpdateSaveState();
        try {
            while(true){
                var picker=CreatePresetImportPicker();
                if((presetImportFilePicker is {} choose?choose(picker):picker.ShowDialog(this))!=true)return;
                try{
                    if(new FileInfo(picker.FileName).Length>1024*1024)throw new FormatException("Preset file is too large.");
                    detail=PresetJsonDocument.ParseImport(await File.ReadAllTextAsync(picker.FileName));
                    var original=PresetDataParser.Parse(Convert.FromHexString(expected.RawDetailPayloadHex));
                    var imported=PresetDataParser.Parse(detail);
                    if(!original.UnknownTrailer.AsSpan().SequenceEqual(imported.UnknownTrailer))throw new NotSupportedException("This preset has incompatible underlying data. Nothing was changed.");
                    break;
                }
                catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or FormatException or NotSupportedException or ArgumentException){
                    presetSaveNotice=$"Import stopped · {ex.Message}";
                    if(CreatePresetImportFileErrorDialog(Path.GetFileName(picker.FileName),ex.Message).ShowDialog()!=true)return;
                }
            }
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or FormatException or NotSupportedException or ArgumentException){presetSaveNotice=$"Import stopped · {ex.Message}";return;}
        finally {presetSaveDialogOpen=false;UpdateSaveState();}
        if(!ReferenceEquals(expected,currentSnapshot)||!importPreset.IsEnabled){presetSaveNotice="The active preset changed. Review it and import again; nothing was changed.";UpdateSaveState();return;}
        Begin("Preparing the preset import…");
        try {
            var proof=new PresetSaveDestination((byte)expected.WireIndex,expected.Name,expected.RawDetailPayloadHex);
            operation!.CancelAfter(Timeout.InfiniteTimeSpan);
            var dialog=CreatePresetImportDialog(expected,proof,PresetDataParser.Parse(detail));
            var controller=(PresetImportDialogController)dialog.Tag;
            activePresetImportDialog=controller;
            controller.Import=async(progress,cancellation)=>{
                operation.CancelAfter(TimeSpan.FromMinutes(10));
                var previousOriginal=originalSnapshotDetail;var previouslyUnsaved=liveEditUnsaved;
                try{
                    progress.Report(new("Checking the active preset and overwrite destination…"));
                    var freshProof=await UseSessionAsync(target,s=>s.ReadPresetSaveDestinationAsync(expected,(byte)expected.WireIndex,operation.Token));
                    if(freshProof!=proof)throw new PresetSavePreconditionException("The active preset changed after confirmation. Nothing was written.");
                    var outcome=await UseSessionAsync(target,s=>s.ImportPresetAsync(expected,freshProof,detail,operation.Token,cancellation,progress));
                    var restored=outcome.Preset;
                    if(!outcome.Cancelled){importRecoveryPanel.Visibility=Visibility.Collapsed;importRecoveryTarget=null;}
                    currentSnapshot=restored;originalSnapshotDetail=outcome.Cancelled&&!outcome.Saved?previousOriginal:restored.RawDetailPayloadHex;
                    liveEditUnsaved=outcome.Cancelled&&!outcome.Saved&&previouslyUnsaved;editSnapshotValid=true;snapshotTime=DateTimeOffset.Now;
                    if(!outcome.Cancelled||outcome.Saved)UpdateSavedPresetLibrary(restored);
                    currentPreset.Text=$"{restored.SlotLabel} · {restored.Name}";SetEffects(restored.Blocks);
                    UpdateMixerState(resetPending:true);Filter();snapshot.Text=SnapshotLabel();
                    presetSaveNotice=outcome.Cancelled?$"Import cancelled · Previous preset restored and verified: {restored.SlotLabel} · {restored.Name}.":
                        $"Import complete · {restored.SlotLabel} · {restored.Name}. Saved settings verified.";
                }
                catch(Exception ex){await HandlePresetImportFailureAsync(ex);controller.NeedsRecovery=true;}
                return controller.NeedsRecovery?importRecoverySummary.Text:presetSaveNotice??"Import finished.";
            };
            controller.Recover=async()=>{
                End();var message=await InspectFailedImportAsync();controller.NeedsRecovery=!importRecoveryReadConfirmed;return message;
            };
            try{dialog.ShowDialog();if(!controller.Completed)presetSaveNotice="Import cancelled · Nothing changed.";}
            finally{activePresetImportDialog=null;}
        }
        catch(Exception ex) {
            await HandlePresetImportFailureAsync(ex);
        }
        finally {End();UpdateSaveState();}
    }

    private static OpenFileDialog CreatePresetImportPicker()=>new(){Title="Import preset",Filter="iAMP presets (Preset-*.json)|Preset-*.json|All JSON files (*.json)|*.json",CheckFileExists=true,Multiselect=false};

    private Window CreatePresetImportFileErrorDialog(string fileName,string reason)
    {
        var dialog=new Window{Title="Cannot import this file",Owner=IsVisible?this:null,Width=580,Height=350,MinWidth=420,MinHeight=300,Background=BackgroundBrush,Foreground=Brushes.White,FontFamily=FontFamily,FontSize=14,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        NameScope.SetNameScope(dialog,new NameScope());var body=new StackPanel();body.Children.Add(Text("Cannot import this file",24));
        body.Children.Add(Text(fileName,14,BlueBrush,new Thickness(0,12,0,12)));body.Children.Add(Text(reason,14));
        body.Children.Add(Text("Nothing has been changed on the amplifier. Choose a Preset export file to continue.",13,MutedBrush,new Thickness(0,14,0,0)));
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0)};
        var cancel=Button("Cancel");cancel.IsCancel=true;cancel.IsDefault=true;cancel.Click+=(_,_)=>dialog.DialogResult=false;
        var choose=Button("Choose another file");choose.Margin=new Thickness(10,0,0,0);choose.Click+=(_,_)=>dialog.DialogResult=true;
        dialog.RegisterName("PresetFileErrorCancel",cancel);dialog.RegisterName("PresetFileErrorChoose",choose);actions.Children.Add(cancel);actions.Children.Add(choose);
        var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        layout.Children.Add(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Grid.SetRow(actions,1);layout.Children.Add(actions);
        dialog.Content=new Border{Padding=new Thickness(24),Child=layout};return dialog;
    }

    private async Task HandlePresetImportFailureAsync(Exception ex)
    {
        var changed=ex is PresetImportException;
        var saveStarted=ex is PresetImportException {SaveStarted:true};
        importRecoveryTarget=snapshotAddress;importRecoveryReadConfirmed=false;
        presetSaveNotice=ex is PresetImportException {RestorationFailed:true}?
            "Cancellation restoration could not be verified. The previous preset may not be fully restored. Refresh the amplifier before another action.":
            saveStarted?"Import save was not confirmed. Stored outcome may be unknown. Refresh before another action; do not retry automatically.":
            changed?"Import stopped. Live settings may be partially changed; the preset was not saved. Refresh to inspect the amplifier.":
            "Import stopped before writing. The amplifier changed or could not be checked; nothing was imported. Refresh and review the active preset.";
        importRecoverySummary.Text=presetSaveNotice+" Choose Reconnect and inspect to read what is on the amplifier now. This check does not save or repeat the import.";
        importRecoveryDetails.Text=ex.GetBaseException().Message;
        importRecoveryPanel.Visibility=Visibility.Visible;
        presetSaveNotice="Import did not complete. Use Reconnect and inspect below; no import is running.";
        editSnapshotValid=false;await ReleaseConnectionAsync();snapshot.Text=SnapshotLabel();UpdateImportRecoveryControls();
    }

    private Window CreatePresetImportDialog(CurrentPresetSnapshot expected,PresetSaveDestination proof,DecodedPreset imported)
    {
        var dialog=new Window{Title="Import preset",Owner=IsVisible?this:null,Width=600,Height=440,MinWidth=420,MinHeight=340,Background=BackgroundBrush,Foreground=Brushes.White,FontFamily=FontFamily,FontSize=14,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        NameScope.SetNameScope(dialog,new NameScope());
        var body=new StackPanel();body.Children.Add(Text("Import into active preset",24));
        body.Children.Add(Text($"Replace {expected.SlotLabel} · {proof.Name}",18,Brushes.White,new Thickness(0,16,0,12)));
        body.Children.Add(Text($"With {expected.SlotLabel} · {imported.Name}",18,BlueBrush));
        body.Children.Add(Text($"{EffectChainCommands.Active(imported.Blocks).Count} effects · All effect settings, order and enabled states will be restored.",13,MutedBrush,new Thickness(0,12,0,0)));
        body.Children.Add(Text("This overwrites the saved preset in the active slot and replaces any unsaved changes. Cancel or close during import to restore the previous preset. The dialog stays open until restoration is verified. Amplifier-wide settings are preserved.",13,MutedBrush,new Thickness(0,12,0,0)));
        var progress=new ProgressBar{Height=8,IsIndeterminate=false,Visibility=Visibility.Collapsed,Foreground=BlueBrush,Background=Brush("#343B46"),Margin=new Thickness(0,18,0,8)};
        var progressText=Text("",13,Brushes.White);progressText.Visibility=Visibility.Collapsed;
        dialog.RegisterName("PresetImportProgress",progress);dialog.RegisterName("PresetImportStatus",progressText);
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0)};
        var cancel=Button("Cancel");cancel.IsCancel=true;cancel.IsDefault=true;
        var confirm=Button("Import and overwrite");confirm.Margin=new Thickness(10,0,0,0);
        dialog.RegisterName("PresetImportCancel",cancel);dialog.RegisterName("PresetImportConfirm",confirm);actions.Children.Add(cancel);actions.Children.Add(confirm);
        var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        layout.Children.Add(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        var footer=new StackPanel();footer.Children.Add(progress);footer.Children.Add(progressText);footer.Children.Add(actions);Grid.SetRow(footer,1);layout.Children.Add(footer);
        dialog.Content=new Border{Padding=new Thickness(24),Child=layout};dialog.Tag=new PresetImportDialogController(dialog,confirm,cancel,progressText,progress);dialog.Loaded+=(_,_)=>cancel.Focus();return dialog;
    }
}
