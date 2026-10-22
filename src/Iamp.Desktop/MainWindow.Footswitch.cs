using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button footswitchNavigation=Button("Footswitch"),pairFootswitch=Button("Pair F4…"),forgetFootswitch=Button("Forget F4…");
    private readonly ScrollViewer footswitchScroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed};
    private readonly TextBlock footswitchStatus=Text("Amplifier disconnected",20,MutedBrush),footswitchFeedback=Text("",13,MutedBrush,new Thickness(0,14,0,0));
    private readonly F4PresetPanel footswitchPresetPanel=new(){Margin=new Thickness(0,16,0,0)};
    private F15ReadSession? observedFootswitchSession;
    private Action<FootswitchConnectionStatus>? footswitchStatusHandler;
    private bool footswitchDialogOpen;

    private void BuildFootswitchPage()
    {
        var page=new StackPanel {Margin=new Thickness(32,0,32,24)};page.Children.Add(Text("Footswitch",30));
        page.Children.Add(footswitchPresetPanel);
        footswitchPresetPanel.PresetRequested+=async wire=>await RecallFootswitchPresetAsync(wire);
        var card=new StackPanel();card.Children.Add(Text("MOOER F4",18));footswitchStatus.Margin=new Thickness(0,10,0,14);card.Children.Add(footswitchStatus);
        var actions=new WrapPanel();actions.Children.Add(pairFootswitch);forgetFootswitch.Margin=new Thickness(10,0,0,0);actions.Children.Add(forgetFootswitch);card.Children.Add(actions);card.Children.Add(footswitchFeedback);
        page.Children.Add(new Border {Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(18),Margin=new Thickness(0,16,0,18),Child=card});
        page.Children.Add(Text("The F4 connects directly to your amplifier. It works without the desktop or mobile app once paired.",13,MutedBrush));
        page.Children.Add(Text("Preset changes made with the F4 appear automatically in Presets. Selecting a bank on the F4 takes effect when you choose A, B, C or D.",13,MutedBrush,new Thickness(0,10,0,0)));
        AutomationProperties.SetName(footswitchStatus,"F4 connection status");
        pairFootswitch.Click+=(_,_)=>ShowFootswitchDialog(true);forgetFootswitch.Click+=(_,_)=>ShowFootswitchDialog(false);
        footswitchScroll.Content=page;UpdateFootswitchState();
    }
    private bool CanUseFootswitchControls()=>!preview&&connectedSession is {IsAvailable:true}&&snapshotFirmware=="V1.1.9"&&
        operation is null&&!tunerActive&&!tunerStarting&&!drumReady&&!drumStarting&&!drumWriting;

    private void WatchFootswitchSession(F15ReadSession? session)
    {
        if(observedFootswitchSession is {} previous&&footswitchStatusHandler is {} handler)previous.FootswitchStatusChanged-=handler;
        observedFootswitchSession=session;footswitchStatusHandler=null;footswitchFeedback.Text="";
        if(session is not null){
            footswitchStatusHandler=_=>Dispatcher.BeginInvoke(()=>{if(ReferenceEquals(connectedSession,session)){if(!footswitchDialogOpen)footswitchFeedback.Text="";UpdateFootswitchState();}});
            session.FootswitchStatusChanged+=footswitchStatusHandler;
        }
        UpdateFootswitchState();
    }
    private void UpdateFootswitchState()
    {
        var connected=connectedSession is {IsAvailable:true};var state=connected?connectedSession!.FootswitchStatus:FootswitchConnectionStatus.Unknown;
        SetFootswitchPresentation(connected,state);
        var known=observedAmplifierPreset??(!localSnapshotSource?currentSnapshot:null);
        footswitchPresetPanel.Update(names,known,connected&&editSnapshotValid&&!localSnapshotSource,CanRecallFootswitchPreset());
        var ready=CanUseFootswitchControls()&&!footswitchDialogOpen;
        pairFootswitch.IsEnabled=ready&&state!=FootswitchConnectionStatus.Connected;
        forgetFootswitch.IsEnabled=ready&&state==FootswitchConnectionStatus.Connected;
    }
    private bool CanRecallFootswitchPreset()=>CanUseFootswitchControls()&&!footswitchDialogOpen&&editSnapshotValid&&!localSnapshotSource&&snapshotAddress.HasValue;
    private async Task RecallFootswitchPresetAsync(int wire)
    {
        if(!CanRecallFootswitchPreset()||names.FirstOrDefault(n=>n.WireIndex==wire) is not {} selected)return;
        await RecallPresetAsync(selected);
        ResetDropdownToCurrent();
        if(lastMutationSucceeded)footswitchFeedback.Text=$"Preset recalled · {currentSnapshot!.SlotLabel} · {currentSnapshot.Name}";
        else if(!editSnapshotValid)footswitchFeedback.Text="Preset change was not confirmed. Refresh the amplifier before trying again.";
        UpdateFootswitchState();
    }
    private void SetFootswitchPresentation(bool amplifierConnected,FootswitchConnectionStatus state)
    {
        footswitchStatus.Text=!amplifierConnected?"Amplifier disconnected":state switch {
            FootswitchConnectionStatus.Connected=>"F4 connected to amplifier",
            FootswitchConnectionStatus.Disconnected=>"F4 not connected",
            _=>"Waiting for amplifier footswitch status…"};
        footswitchStatus.Foreground=amplifierConnected&&state==FootswitchConnectionStatus.Connected?BlueBrush:MutedBrush;
        footswitchStatus.ToolTip=state==FootswitchConnectionStatus.Disconnected?"Not connected does not mean its saved pairing has been deleted.":null;
    }
    private void ShowFootswitchDialog(bool pairing)
    {
        UpdateFootswitchState();if(!(pairing?pairFootswitch:forgetFootswitch).IsEnabled)return;
        footswitchDialogOpen=true;UpdateFootswitchState();
        try {CreateFootswitchDialog(pairing).ShowDialog();}
        finally {footswitchDialogOpen=false;UpdateFootswitchState();}
    }
    private Window CreateFootswitchDialog(bool pairing)
    {
        var title=pairing?"Pair MOOER F4":"Forget MOOER F4";
        var dialog=new Window {Title=title,Owner=IsVisible?this:null,Width=560,Height=370,MinWidth=420,MinHeight=330,
            WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BackgroundBrush,Foreground=Brushes.White,FontFamily=FontFamily,FontSize=14};
        NameScope.SetNameScope(dialog,new NameScope());
        var body=new StackPanel();body.Children.Add(Text(title,24));
        body.Children.Add(Text(pairing?"Hold A+C together on the F4 for about 3 seconds until its LEDs flash, then choose Start pairing.":
            "Remove the amplifier’s F4 pairing? Footswitch control will stop, and you will need to pair it again. Your presets will not be changed.",14,MutedBrush,new Thickness(0,14,0,16)));
        var message=Text(pairing?"The F4 pairs with the amplifier, not with Windows.":"This removes the saved footswitch pairing on the amplifier.",13,MutedBrush);
        body.Children.Add(message);
        var progress=new ProgressBar {Height=6,Foreground=BlueBrush,Background=Brush("#343B46"),Margin=new Thickness(0,14,0,0),Visibility=Visibility.Collapsed};body.Children.Add(progress);
        var actions=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0)};
        var close=Button("Cancel");close.IsCancel=true;close.IsDefault=true;
        var start=Button(pairing?"Start pairing":"Forget F4");start.Margin=new Thickness(10,0,0,0);start.Background=BlueBrush;start.Foreground=Brush("#101C2B");actions.Children.Add(close);actions.Children.Add(start);
        dialog.RegisterName("FootswitchConfirm",start);dialog.RegisterName("FootswitchClose",close);dialog.RegisterName("FootswitchProgress",progress);dialog.RegisterName("FootswitchMessage",message);
        var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        layout.Children.Add(new ScrollViewer {Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Grid.SetRow(actions,1);layout.Children.Add(actions);
        dialog.Content=new Border {Padding=new Thickness(24),Background=BackgroundBrush,Child=layout};
        bool running=false,acknowledged=false;CancellationTokenSource? stopWaiting=null;
        close.Click+=(_,_)=>{if(running){if(acknowledged)stopWaiting?.Cancel();}else dialog.Close();};
        dialog.Closing+=(_,e)=>{if(running){e.Cancel=true;if(acknowledged)stopWaiting?.Cancel();}};
        dialog.Loaded+=(_,_)=>close.Focus();
        start.Click+=async(_,_)=>{
            if(running||!CanUseFootswitchControls()||connectedSession is not {} session)return;
            if(pairing&&session.FootswitchStatus==FootswitchConnectionStatus.Connected){message.Text="F4 is already connected.";return;}
            if(!pairing&&session.FootswitchStatus!=FootswitchConnectionStatus.Connected){message.Text="F4 connection changed. Close this dialog and check its status.";return;}
            running=true;start.IsEnabled=false;close.IsEnabled=false;close.IsCancel=false;acknowledged=false;
            progress.Visibility=Visibility.Visible;progress.IsIndeterminate=true;
            message.Text=pairing?"Starting amplifier pairing…":"Removing F4 pairing…";
            var desired=pairing?FootswitchConnectionStatus.Connected:FootswitchConnectionStatus.Disconnected;
            var observed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Observe(FootswitchConnectionStatus status){if(status==desired)observed.TrySetResult();}
            session.FootswitchStatusChanged+=Observe;
            Begin(pairing?"Pairing F4…":"Removing F4 pairing…");
            try {
                await session.SetFootswitchPairingAsync(pairing,operation!.Token);acknowledged=true;
                Observe(session.FootswitchStatus);
                close.Content="Stop waiting";close.IsEnabled=true;
                message.Text=pairing?"Waiting for the F4 connection (up to 30 seconds)…":"Waiting for the amplifier to confirm disconnection…";
                using var wait=CancellationTokenSource.CreateLinkedTokenSource(operation.Token);stopWaiting=wait;wait.CancelAfter(TimeSpan.FromSeconds(30));
                while(!observed.Task.IsCompleted){
                    await Task.WhenAny(observed.Task,Task.Delay(250,wait.Token));wait.Token.ThrowIfCancellationRequested();
                    if(!session.IsAvailable)throw new System.IO.IOException("Amplifier connection lost while waiting for footswitch status.");
                }
                await observed.Task;
                message.Text=pairing?"F4 connected. Check that its display shows a preset number.":"F4 pairing removed.";
                footswitchFeedback.Text=message.Text;
            }
            catch(OperationCanceledException) when(acknowledged){
                message.Text=pairing?"Connection was not confirmed. Pairing may still be active on the amplifier. You can check the F4 and try again; no unpair command was sent.":"Stopped waiting. Check the current F4 connection status.";
                footswitchFeedback.Text=message.Text;
            }
            catch(Exception){
                message.Text="The pairing operation was not confirmed. Reconnect the amplifier before trying again; the command will not be repeated automatically.";footswitchFeedback.Text=message.Text;
                editSnapshotValid=false;await ReleaseConnectionAsync();
                footswitchFeedback.Text=message.Text;
            }
            finally {
                stopWaiting=null;session.FootswitchStatusChanged-=Observe;running=false;progress.IsIndeterminate=false;progress.Visibility=Visibility.Collapsed;
                close.Content="Close";close.IsEnabled=true;close.IsCancel=true;start.Visibility=Visibility.Collapsed;End();UpdateFootswitchState();
            }
        };
        return dialog;
    }
}
