using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button tunerNavigation=Button("Tuner");
    private readonly ScrollViewer tunerScroll=new() {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed};
    private readonly Button tunerStart=Button("Start tuner"),tunerStop=Button("Stop tuner"),
        tunerLower=Button("−"),tunerHigher=Button("+"),tunerOutput=Button("");
    private readonly TextBlock tunerSummary=Text("",13,MutedBrush),
        tunerNote=Text("—",72),tunerDeviation=Text("No signal",16,MutedBrush),tunerReference=Text("— Hz",22);
    private readonly Canvas tunerMeter=new() {Width=112,Height=280,HorizontalAlignment=HorizontalAlignment.Center};
    private readonly List<Polygon> tunerSegments=new();
    private readonly DispatcherTimer tunerWatchdog=new() {Interval=TimeSpan.FromMilliseconds(250)};
    private F15ReadSession? tunerSession;
    private Action<byte[]>? tunerPayloadHandler;
    private bool tunerStarting,tunerActive,tunerPreviousEditValid,tunerCloseRequested,tunerAmpInactive;
    private string? tunerNotice;
    private int tunerGeneration;
    private DateTimeOffset tunerLastPollAt;
    private bool tunerPolling;

    private void BuildTunerPage()
    {
        if(tunerScroll.Content is not null)return;
        var page=new StackPanel {Margin=new Thickness(32,0,32,24)};
        page.Children.Add(Text("Tuner",30));
        page.Children.Add(tunerSummary);
        var actions=new WrapPanel {Margin=new Thickness(0,14,0,18)};
        actions.Children.Add(tunerStart);tunerStop.Margin=new Thickness(10,0,0,0);actions.Children.Add(tunerStop);page.Children.Add(actions);
        var display=new Grid {MaxWidth=500,HorizontalAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,8,0,20)};
        display.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(112)});
        display.ColumnDefinitions.Add(new ColumnDefinition());
        for(var i=0;i<17;i++) {
            var width=54-Math.Abs(i-8)*2;var left=(112-width)/2d;var y=10+i*15.8;
            var segment=new Polygon {Points=new PointCollection {new(left,y),new(left+width,y),new(left+width-1,y+11),new(left+1,y+11)},Fill=Brush("#34393F")};
            tunerSegments.Add(segment);tunerMeter.Children.Add(segment);
        }
        AutomationProperties.SetName(tunerMeter,"Tuning deviation meter; sharp above center, flat below center");
        display.Children.Add(tunerMeter);
        var notes=new StackPanel {VerticalAlignment=VerticalAlignment.Center};
        tunerNote.TextAlignment=TextAlignment.Center;tunerDeviation.TextAlignment=TextAlignment.Center;
        notes.Children.Add(tunerNote);notes.Children.Add(tunerDeviation);Grid.SetColumn(notes,1);display.Children.Add(notes);
        page.Children.Add(new Border {Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(20),MaxWidth=500,
            HorizontalAlignment=HorizontalAlignment.Stretch,Child=display});
        page.Children.Add(Text("Reference pitch",14,MutedBrush,new Thickness(0,22,0,8)));
        var reference=new StackPanel {Orientation=Orientation.Horizontal};
        tunerLower.Width=tunerHigher.Width=48;tunerReference.Width=120;tunerReference.TextAlignment=TextAlignment.Center;
        tunerReference.VerticalAlignment=VerticalAlignment.Center;reference.Children.Add(tunerLower);reference.Children.Add(tunerReference);reference.Children.Add(tunerHigher);page.Children.Add(reference);
        tunerOutput.HorizontalAlignment=HorizontalAlignment.Left;tunerOutput.Margin=new Thickness(0,22,0,0);page.Children.Add(tunerOutput);
        tunerStart.Click+=async(_,_)=>await StartTunerAsync();tunerStop.Click+=async(_,_)=>await StopTunerAsync();
        tunerLower.Click+=async(_,_)=>await ChangeTunerReferenceAsync(-1);tunerHigher.Click+=async(_,_)=>await ChangeTunerReferenceAsync(1);
        tunerOutput.Click+=async(_,_)=> {
            if(ReadTunerSettings() is not {} settings)return;
            var next=settings.OutputMode==TunerOutputMode.Mute?TunerOutputMode.Bypass:TunerOutputMode.Mute;
            await ChangeTunerSettingAsync(session=>session.SetTunerOutputModeAsync(next,operation!.Token));
        };
        AutomationProperties.SetName(tunerLower,"Lower reference pitch by one hertz");AutomationProperties.SetName(tunerHigher,"Raise reference pitch by one hertz");
        AutomationProperties.SetName(tunerNote,"Detected tuner note: no signal");AutomationProperties.SetName(tunerDeviation,"Amplifier tuning indicator: no signal");
        tunerWatchdog.Tick+=async(_,_)=> {
            if(!UpdateTunerConnectionStatus(tunerSession is not null && tunerSession==connectedSession && tunerSession.IsAvailable))return;
            if(!tunerPolling && operation is null && DateTimeOffset.UtcNow-tunerLastPollAt>=TimeSpan.FromSeconds(15)) {
                var session=tunerSession!;var generation=tunerGeneration;tunerPolling=true;tunerLastPollAt=DateTimeOffset.UtcNow;
                try {await session.PollTunerAsync(CancellationToken.None);}
                catch(Exception) {
                    if(generation==tunerGeneration && tunerSession==session) {
                        tunerNotice="Tuner connection lost · Check the amplifier’s tuner state, then reconnect.";
                        ResetTunerUiForDisconnect();editSnapshotValid=false;UpdateThresholdState();UpdateConnectionControls();
                    }
                }
                finally {tunerPolling=false;}
            }
        };
        tunerScroll.Content=page;UpdateTunerState();
    }

    private bool UpdateTunerConnectionStatus(bool connected)
    {
        if(!tunerActive)return false;
        if(connected)return true;
        tunerNotice="Tuner connection lost · Check the amplifier’s tuner state, then reconnect.";
        ResetTunerUiForDisconnect();editSnapshotValid=false;UpdateThresholdState();UpdateConnectionControls();return false;
    }

    private TunerSettings? ReadTunerSettings()
    {
        try {return currentSnapshot is {} current?TunerSettings.Parse(Convert.FromHexString(current.RawStatePayloadHex)):null;}
        catch(Exception ex) when(ex is FormatException or NotSupportedException){return null;}
    }

    private void UpdateTunerState()
    {
        var settings=ReadTunerSettings();var busy=operation is not null || tunerStarting;
        var connected=!preview && !localSnapshotSource && connectedSession is {IsAvailable:true} && snapshotFirmware=="V1.1.9";
        tunerStart.IsEnabled=connected && editSnapshotValid && settings is not null && !busy && !tunerActive;
        tunerStop.IsEnabled=tunerActive && !busy;
        var adjustable=connected && tunerActive && !tunerAmpInactive && !busy && settings is not null;
        tunerLower.IsEnabled=adjustable && settings!.ReferenceHertz>TunerSettings.MinimumHertz;
        tunerHigher.IsEnabled=adjustable && settings!.ReferenceHertz<TunerSettings.MaximumHertz;
        tunerOutput.IsEnabled=adjustable;tunerReference.Text=settings is null?"— Hz":$"{settings.ReferenceHertz} Hz";
        var muted=settings?.OutputMode==TunerOutputMode.Mute;
        tunerOutput.Content=new StackPanel {Orientation=Orientation.Horizontal,Children={
            new TextBlock {Text=muted?"\uE74F":"\uE767",FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=20,VerticalAlignment=VerticalAlignment.Center},
            Text(muted?"Mute":"Bypass",14,null,new Thickness(10,0,0,0))}};
        tunerOutput.ToolTip=muted?"Amplifier output muted · Switch to bypass":"Amplifier output bypassed · Switch to mute";
        AutomationProperties.SetName(tunerOutput,muted?"Tuner output muted; switch to bypass":"Tuner output bypassed; switch to mute");
        tunerSummary.Text=preview?"Preview only · No amplifier commands":busy?"Tuner operation in progress…":tunerNotice is not null?tunerNotice:
            tunerActive?"Tuner active · Play a single note. Stop the tuner to return to normal amplifier operation.":
            !connected?"":
            !editSnapshotValid?"Refresh the amplifier before starting the tuner. Your pending inputs are preserved.":
            "Ready to tune · Reference pitch and output mode are read from the amplifier.";
        tunerSummary.Visibility=string.IsNullOrWhiteSpace(tunerSummary.Text)?Visibility.Collapsed:Visibility.Visible;
    }

    private async Task StartTunerAsync()
    {
        if(!tunerStart.IsEnabled || connectedSession is not {} session || currentSnapshot is null)return;
        tunerNotice=null;tunerAmpInactive=false;tunerPreviousEditValid=editSnapshotValid;tunerStarting=true;refreshTimer.Stop();
        Begin("Starting tuner…");UpdateTunerState();
        try {
            // Let the bounded in-flight read finish; cancelling it can terminate the shared session.
            while(polling)await Task.Delay(40,operation!.Token);
            if(connectedSession!=session || !session.IsAvailable)throw new IOException("Amplifier connection changed before tuner entry.");
            var generation=++tunerGeneration;tunerSession=session;
            tunerPayloadHandler=payload=> {
                var copy=payload.ToArray();
                _=Dispatcher.InvokeAsync(()=> {if(generation==tunerGeneration && tunerActive && tunerSession==session && connectedSession==session)HandleTunerPayload(copy);});
            };
            session.TunerPayloadReceived+=tunerPayloadHandler;
            editSnapshotValid=false;UpdateThresholdState();
            currentSnapshot=await session.EnterTunerAsync(currentSnapshot,operation!.Token);
            tunerActive=true;tunerLastPollAt=DateTimeOffset.MinValue;ClearTunerReading();tunerWatchdog.Start();status.Text="Tuner active";
        }
        catch(Exception) {
            editSnapshotValid=false;tunerActive=session.IsTunerActive;
            if(tunerActive)tunerWatchdog.Start();else ResetTunerUiForDisconnect();
            tunerNotice=status.Text="Tuner start was not confirmed · Check the amplifier’s tuner state and reconnect before editing.";
        }
        finally {tunerStarting=false;End();UpdateTunerState();}
    }

    private async Task ChangeTunerReferenceAsync(int step)
    {
        if(ReadTunerSettings() is not {} settings)return;
        var value=settings.ReferenceHertz+step;
        if(value<TunerSettings.MinimumHertz || value>TunerSettings.MaximumHertz)return;
        await ChangeTunerSettingAsync(session=>session.SetTunerReferenceAsync(value,operation!.Token));
    }

    private async Task ChangeTunerSettingAsync(Func<F15ReadSession,Task<CurrentPresetSnapshot>> change)
    {
        if(!tunerActive || tunerAmpInactive || operation is not null || tunerSession is not {} session || session!=connectedSession || !session.IsAvailable)return;
        Begin("Updating tuner…");UpdateTunerState();
        try {currentSnapshot=await change(session);snapshotTime=DateTimeOffset.Now;snapshot.Text=SnapshotLabel();tunerNotice=null;}
        catch(Exception) {editSnapshotValid=false;tunerNotice=status.Text="Tuner setting not confirmed · Check the amplifier’s tuner state and reconnect before editing.";}
        finally {End();UpdateTunerState();}
    }

    private async Task<bool> StopTunerAsync()
    {
        if(tunerStarting || operation is not null)return false;
        if(!tunerActive){ClearTunerReading();return true;}
        if(tunerSession is not {} session || session!=connectedSession || !session.IsAvailable) {
            ResetTunerUiForDisconnect();editSnapshotValid=false;return true;
        }
        Begin("Stopping tuner…");UpdateTunerState();
        try {
            var before=currentSnapshot;var confirmed=await session.ExitTunerAsync(operation!.Token);
            var samePreset=before?.WireIndex==confirmed.WireIndex && before.RawDetailPayloadHex==confirmed.RawDetailPayloadHex;
            currentSnapshot=confirmed;snapshotTime=DateTimeOffset.Now;tunerNotice=null;
            ResetTunerUiForDisconnect();editSnapshotValid=tunerPreviousEditValid && samePreset;
            snapshot.Text=SnapshotLabel();status.Text=samePreset?"Tuner stopped":
                "Amplifier changed while tuning · Pending inputs preserved. Refresh amplifier before editing.";
            if(editSnapshotValid)refreshTimer.Start();
            return true;
        }
        catch(Exception) {editSnapshotValid=false;ClearTunerReading();tunerNotice=status.Text="Tuner exit not confirmed · Check the amplifier’s tuner state, then disconnect and reconnect.";return false;}
        finally {End();UpdateTunerState();}
    }

    private void ResetTunerUiForDisconnect()
    {
        tunerWatchdog.Stop();++tunerGeneration;
        if(tunerSession is {} session && tunerPayloadHandler is {} handler)session.TunerPayloadReceived-=handler;
        tunerSession=null;tunerPayloadHandler=null;tunerActive=false;tunerAmpInactive=false;ClearTunerReading();UpdateTunerState();
    }

    private void ClearTunerReading()
    {
        diagnostics.Record(DiagnosticOperation.TunerUiNoSignal,DiagnosticOutcome.Completed);
        tunerNote.Text="—";tunerNote.Foreground=Brushes.White;tunerDeviation.Text="No signal";
        AutomationProperties.SetName(tunerNote,"Detected tuner note: no signal");AutomationProperties.SetName(tunerDeviation,"Amplifier tuning indicator: no signal");
        tunerDeviation.Foreground=MutedBrush;
        foreach(var segment in tunerSegments)segment.Fill=Brush("#34393F");
    }

    private void SetTunerReading(TunerReading reading)
    {
        if(!tunerActive || tunerAmpInactive || !reading.HasSignal){ClearTunerReading();return;}
        diagnostics.Record(DiagnosticOperation.TunerUiSignal,DiagnosticOutcome.Completed);
        tunerNote.Text=reading.NoteName;
        AutomationProperties.SetName(tunerNote,$"Detected tuner note: {reading.NoteName}");
        var distance=Math.Abs(reading.DisplayPosition-8);
        var color=reading.IsInTune?Brush("#5CE600"):distance<=3?Brush("#FFC001"):distance<=5?Brush("#ED7D31"):Brush("#E90000");
        tunerNote.Foreground=reading.IsInTune?color:Brushes.White;
        tunerDeviation.Foreground=color;tunerDeviation.Text=reading.Direction;
        AutomationProperties.SetName(tunerDeviation,$"Amplifier tuning indicator: {reading.Direction}");
        foreach(var segment in tunerSegments)segment.Fill=Brush("#34393F");
        foreach(var position in reading.DisplayPositions)tunerSegments[position].Fill=color;
    }

    private void HandleTunerPayload(byte[] payload)
    {
        if(payload.AsSpan().SequenceEqual(new byte[]{0x43,0x00})) {
            tunerAmpInactive=true;ClearTunerReading();
            tunerNotice="The amplifier left tuner mode · Stop the tuner, then start it again.";
            UpdateTunerState();return;
        }
        if(payload.AsSpan().SequenceEqual(new byte[]{0x43,0x02})) {
            tunerAmpInactive=false;tunerNotice=null;UpdateTunerState();return;
        }
        if(TunerReading.TryParse(payload,out var reading))SetTunerReading(reading!.Value);
        else ClearTunerReading();
    }
}
