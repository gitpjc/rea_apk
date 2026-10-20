using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Iamp.Protocol;

namespace Iamp.Desktop;

internal sealed class PresetImportDialogController
{
    private readonly Window dialog;
    private readonly Button confirm,cancel;
    private readonly TextBlock status;
    private readonly ProgressBar progress;
    private readonly CancellationTokenSource cancellation=new();
    public Func<IProgress<PresetImportProgress>,CancellationToken,Task<string>>? Import {get;set;}
    public Func<Task<string>>? Recover {get;set;}
    public bool NeedsRecovery {get;set;}
    public bool Recovering {get;private set;}
    public bool Running {get;private set;}
    public bool Completed {get;private set;}

    public PresetImportDialogController(Window dialog,Button confirm,Button cancel,TextBlock status,ProgressBar progress)
    {
        this.dialog=dialog;this.confirm=confirm;this.cancel=cancel;this.status=status;this.progress=progress;
        confirm.Click+=async(_,_)=>await StartAsync();
        cancel.Click+=(_,_)=>{if(Running)RequestCancel();else dialog.DialogResult=false;};
        dialog.Closing+=(_,e)=>{if(Running){e.Cancel=true;RequestCancel();}};
        dialog.PreviewKeyDown+=(_,e)=>{if(Running&&e.Key==Key.Escape){e.Handled=true;RequestCancel();}};
        dialog.Closed+=(_,_)=>cancellation.Dispose();
    }

    public void RequestCancel()
    {
        if(!Running||cancellation.IsCancellationRequested)return;
        if(Recovering){status.Text="Reading the amplifier’s current settings. Please wait for this check to finish…";return;}
        cancellation.Cancel();cancel.IsEnabled=false;
        status.Text="Cancelling import · Waiting for the current command, then restoring the previous preset…";
    }

    private async Task StartAsync()
    {
        if(Completed){
            if(NeedsRecovery&&Recover is not null)await RecoverAsync();else dialog.Close();
            return;
        }
        if(Running||Import is null)return;
        Running=true;confirm.IsEnabled=false;cancel.IsCancel=false;cancel.IsDefault=false;
        progress.Visibility=Visibility.Visible;progress.IsIndeterminate=true;status.Visibility=Visibility.Visible;
        status.Text="Checking the active preset…";
        var reporter=new DialogProgress(dialog,item=>{
            if(!Running)return;
            status.Text=cancellation.IsCancellationRequested&&!item.Restoring?
                "Cancelling import · Completing the current command before restoration…":item.Message;
        });
        try{status.Text=await Import(reporter,cancellation.Token);}
        catch(Exception){NeedsRecovery=Recover is not null;status.Text="Import could not be completed. Restoration was not verified. Choose Reconnect and inspect to read the amplifier’s actual settings.";}
        finally{
            Running=false;Completed=true;progress.IsIndeterminate=false;progress.Visibility=Visibility.Collapsed;
            SetCompletionActions();
            confirm.Focus();
        }
    }

    private void SetCompletionActions()
    {
        dialog.Title=NeedsRecovery?"Import stopped":"Import result";
        confirm.Content=NeedsRecovery?"Reconnect and inspect":"Close";confirm.IsEnabled=true;confirm.IsDefault=true;
        cancel.Content="Close";cancel.Visibility=NeedsRecovery?Visibility.Visible:Visibility.Collapsed;cancel.IsEnabled=true;cancel.IsCancel=true;
    }

    private async Task RecoverAsync()
    {
        Running=true;Recovering=true;confirm.IsEnabled=false;cancel.IsEnabled=false;cancel.IsCancel=false;
        progress.Visibility=Visibility.Visible;progress.IsIndeterminate=true;status.Text="Reconnecting and reading the actual amplifier settings. No preset will be saved…";
        try{status.Text=await Recover!();}
        catch(Exception){status.Text="Could not read the amplifier. Check that it is powered on and phone iAMP is disconnected, then choose Reconnect and inspect again.";NeedsRecovery=true;}
        finally{Running=false;Recovering=false;progress.IsIndeterminate=false;progress.Visibility=Visibility.Collapsed;SetCompletionActions();}
    }

    private sealed class DialogProgress(Window dialog,Action<PresetImportProgress> apply):IProgress<PresetImportProgress>
    {
        public void Report(PresetImportProgress value){if(dialog.Dispatcher.CheckAccess())apply(value);else dialog.Dispatcher.BeginInvoke(()=>apply(value));}
    }
}
