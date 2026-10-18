using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button drumNavigation=Button("Drum"),drumLower=Button("❮"),drumHigher=Button("❯"),
        drumPlay=Button("▶"),drumSpeaker=Button(""),drumRefresh=Button("Read Drum");
    private readonly ScrollViewer drumScroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed};
    private readonly DrumTempoDial drumDial=new();
    private readonly TextBlock drumSummary=Text("Connect the amplifier from Amplifier Settings to use Drum.",13,MutedBrush),
        drumVolumeLabel=Text("—",20);
    private readonly Slider drumVolume=new(){Orientation=Orientation.Vertical,Minimum=0,Maximum=DrumSettings.MaximumVolume,
        Width=48,Height=240,SmallChange=1,LargeChange=5,TickFrequency=1,IsSnapToTickEnabled=true,
        IsMoveToPointEnabled=true,Foreground=BlueBrush,Template=MixerFaderTemplate};
    private static readonly string[] DrumStyles=["Pop","Rock","Blues","Funk","Jazz","Metal","Metronome"];
    private readonly ComboBox drumStyle=new(){ItemsSource=DrumStyles,MinWidth=180,HorizontalContentAlignment=HorizontalAlignment.Left};
    private readonly System.Windows.Controls.Primitives.UniformGrid drumPatternGrid=new(){Columns=5,Rows=2,Margin=new Thickness(0,10,0,0)};
    private readonly List<Button> drumPatternButtons=new();
    private readonly TextBlock drumPatternLabel=Text("",13,MutedBrush,new Thickness(0,8,0,18));
    private readonly System.Windows.Documents.Run drumPatternValue=new("—"){Foreground=Brush("#4183d1")};
    private readonly StackPanel drumPauseIcon=new(){Orientation=Orientation.Horizontal,Children={
        new Border{Width=7,Height=24,Background=BlueBrush,CornerRadius=new CornerRadius(1),Margin=new Thickness(3,0,3,0)},
        new Border{Width=7,Height=24,Background=BlueBrush,CornerRadius=new CornerRadius(1),Margin=new Thickness(3,0,3,0)}}};
    private byte? drumDisplayedPattern;
    private readonly Popup drumVolumePopup=new(){StaysOpen=false,AllowsTransparency=true,Placement=PlacementMode.Top};
    private bool drumStarting,drumReady,drumWriting,drumUpdating,drumPlaying;
    private readonly Dictionary<string,int> drumPending=new();
    private readonly System.Windows.Threading.DispatcherTimer drumWatchdog=new(){Interval=TimeSpan.FromMilliseconds(500)};
    private long? drumLastTap;
    private string? drumNotice;

    private void BuildDrumPage()
    {
        var page=new StackPanel{Margin=new Thickness(32,20,32,24)};
        page.Children.Add(Text("Drum",30));page.Children.Add(drumSummary);
        drumSummary.Margin=new Thickness(0,10,0,12);
        drumRefresh.HorizontalAlignment=HorizontalAlignment.Left;page.Children.Add(drumRefresh);
        var surface=new StackPanel{MaxWidth=480,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,22,0,0)};
        var dialRow=new Grid();dialRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        dialRow.ColumnDefinitions.Add(new());dialRow.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        drumLower.Width=drumHigher.Width=40;drumLower.Padding=drumHigher.Padding=new Thickness(5);
        drumLower.VerticalAlignment=drumHigher.VerticalAlignment=VerticalAlignment.Center;
        var scaledDial=new Viewbox{Child=drumDial,MaxWidth=320,Stretch=Stretch.Uniform};
        Grid.SetColumn(scaledDial,1);Grid.SetColumn(drumHigher,2);
        dialRow.Children.Add(drumLower);dialRow.Children.Add(scaledDial);dialRow.Children.Add(drumHigher);surface.Children.Add(dialRow);
        surface.Children.Add(Text("Style",13,MutedBrush,new Thickness(0,22,0,6)));
        drumStyle.Background=PanelBrush;drumStyle.Foreground=Brushes.White;drumStyle.Padding=new Thickness(10,7,10,7);
        drumStyle.MinHeight=38;
        drumStyle.Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="ComboBox">
              <Grid>
                <ToggleButton Focusable="False" IsChecked="{Binding IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}" ClickMode="Press">
                  <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border Background="#20242B" BorderBrush="#46505E" BorderThickness="1" CornerRadius="5"><TextBlock Text="▾" Foreground="White" HorizontalAlignment="Right" Margin="0,0,12,0" VerticalAlignment="Center"/></Border></ControlTemplate></ToggleButton.Template>
                </ToggleButton>
                <ContentPresenter Content="{TemplateBinding SelectionBoxItem}" Margin="12,4,36,4" VerticalAlignment="Center" IsHitTestVisible="False"/>
                <Popup x:Name="PART_Popup" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False">
                  <Border Background="#20242B" BorderBrush="#46505E" BorderThickness="1" MinWidth="{Binding ActualWidth,RelativeSource={RelativeSource TemplatedParent}}"><ScrollViewer VerticalScrollBarVisibility="Auto" MaxHeight="300"><ItemsPresenter/></ScrollViewer></Border>
                </Popup>
              </Grid>
              <ControlTemplate.Triggers><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.55"/></Trigger></ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        var styleRow=new Style(typeof(ComboBoxItem));styleRow.Setters.Add(new Setter(Control.ForegroundProperty,Brushes.White));
        styleRow.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(10,7,10,7)));
        styleRow.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)System.Windows.Markup.XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ComboBoxItem">
              <Border x:Name="Row" Background="Transparent" Padding="{TemplateBinding Padding}"><ContentPresenter/></Border>
              <ControlTemplate.Triggers><Trigger Property="IsHighlighted" Value="True"><Setter TargetName="Row" Property="Background" Value="#34485F"/></Trigger></ControlTemplate.Triggers>
            </ControlTemplate>
            """)));
        drumStyle.ItemContainerStyle=styleRow;
        surface.Children.Add(drumStyle);
        for(var i=0;i<10;i++)
        {
            var patternButton=Button($"{i+1:00}");patternButton.Margin=new Thickness(2);patternButton.MinHeight=38;
            patternButton.Click+=(_,_)=>{if(patternButton.IsEnabled&&patternButton.Tag is byte id)QueueDrumChange("pattern",id);};
            drumPatternButtons.Add(patternButton);drumPatternGrid.Children.Add(patternButton);
        }
        drumPatternLabel.FontWeight=FontWeights.Bold;
        drumPatternLabel.Inlines.Add(new System.Windows.Documents.Run("Current rhythm: "));
        drumPatternLabel.Inlines.Add(drumPatternValue);
        surface.Children.Add(drumPatternGrid);surface.Children.Add(drumPatternLabel);
        var actions=new Grid();actions.ColumnDefinitions.Add(new());actions.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        drumPlay.FontSize=24;drumPlay.Foreground=BlueBrush;drumPlay.MinHeight=54;
        drumSpeaker.Content=new TextBlock{Text="\uE767",FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=26};
        drumSpeaker.Margin=new Thickness(16,0,0,0);drumSpeaker.Width=60;Grid.SetColumn(drumSpeaker,1);
        actions.Children.Add(drumPlay);actions.Children.Add(drumSpeaker);surface.Children.Add(actions);page.Children.Add(surface);
        drumScroll.Content=page;
        var volumePanel=new StackPanel{Width=70,Margin=new Thickness(10)};
        drumVolumeLabel.TextAlignment=TextAlignment.Center;volumePanel.Children.Add(drumVolumeLabel);volumePanel.Children.Add(drumVolume);
        drumVolumePopup.Child=new Border{Background=PanelBrush,CornerRadius=new CornerRadius(8),Child=volumePanel};drumVolumePopup.PlacementTarget=drumSpeaker;
        drumSpeaker.Click+=(_,_)=>drumVolumePopup.IsOpen=!drumVolumePopup.IsOpen;
        drumStyle.SelectionChanged+=(_,_)=>UpdateDrumPatternButtons();
        drumStyle.SelectedIndex=0;
        drumVolume.ValueChanged+=(_,_)=>{if(!drumUpdating){drumVolumeLabel.Text=((int)drumVolume.Value).ToString();QueueDrumChange("volume",(int)drumVolume.Value);}};
        drumDial.TempoChanged+=value=>QueueDrumChange("tempo",value);
        drumDial.Tapped+=TapDrum;
        drumLower.Click+=(_,_)=>QueueDrumChange("tempo",Math.Max(40,drumDial.Tempo-1));
        drumHigher.Click+=(_,_)=>QueueDrumChange("tempo",Math.Min(260,drumDial.Tempo+1));
        drumPlay.Click+=(_,_)=>QueueDrumChange("play",drumPlaying?0:1);
        drumRefresh.Click+=async(_,_)=>await StartDrumAsync();
        drumWatchdog.Tick+=(_,_)=>CheckDrumConnection();
        AutomationProperties.SetName(drumLower,"Lower Drum tempo by one BPM");AutomationProperties.SetName(drumHigher,"Raise Drum tempo by one BPM");
        AutomationProperties.SetName(drumStyle,"Drum rhythm style");
        AutomationProperties.SetName(drumVolume,"Drum volume");AutomationProperties.SetName(drumSpeaker,"Show Drum volume");
        UpdateDrumState();
    }

    private void UpdateDrumPatternButtons()
    {
        var style=Math.Max(0,drumStyle.SelectedIndex);
        int? selected=drumPending.TryGetValue("pattern",out var pending)?pending:ReadDisplayedDrum()?.Pattern.Id;
        for(var i=0;i<drumPatternButtons.Count;i++)
        {
            var button=drumPatternButtons[i];var id=(byte)(style*10+i);var active=selected==id;
            button.Tag=id;button.Background=active?Brush("#203B5C"):Brush("#343B46");
            button.BorderBrush=active?BlueBrush:Brushes.Transparent;button.Foreground=active?BlueBrush:Brushes.White;
            var label=$"{DrumStyles[style]} pattern {i+1:00}";button.ToolTip=DrumPatternCatalog.Get(id).DisplayName;
            AutomationProperties.SetName(button,label+(active?", selected":""));
            AutomationProperties.SetItemStatus(button,active?"Selected":"");
        }
    }

    private DrumSettings? ReadDisplayedDrum()
    {
        try{return currentSnapshot is {} s?DrumSettings.Parse(Convert.FromHexString(s.RawStatePayloadHex)):null;}
        catch(Exception e)when(e is FormatException or NotSupportedException){return null;}
    }

    private void CheckDrumConnection()
    {
        if(!drumReady||connectedSession is {IsAvailable:true})return;
        drumWatchdog.Stop();drumReady=false;drumPending.Clear();editSnapshotValid=false;
        drumNotice="Drum connection lost. Reconnect to read playback and settings; pending commands will not be replayed.";
        UpdateDrumState();UpdateConnectionControls();
    }

    private void UpdateDrumState()
    {
        if(drumUpdating)return;
        drumUpdating=true;
        try
        {
            var settings=ReadDisplayedDrum();
            var connected=!preview&&!localSnapshotSource&&connectedSession is {IsAvailable:true}&&snapshotFirmware=="V1.1.9"&&editSnapshotValid;
            var enabled=connected&&drumReady&&!drumStarting&&!tunerActive&&!tunerStarting&&settings is not null&&(operation is null||drumWriting);
            drumDial.IsEnabled=drumSpeaker.IsEnabled=drumVolume.IsEnabled=enabled;
            drumStyle.IsEnabled=settings is not null;
            foreach(var button in drumPatternButtons)button.IsEnabled=enabled;
            if(!enabled)drumVolumePopup.IsOpen=false;
            drumPlay.IsEnabled=enabled&&!drumWriting;
            drumRefresh.IsEnabled=connected&&operation is null&&!drumStarting&&!tunerActive;
            drumRefresh.Visibility=connected&&!drumReady&&!drumStarting?Visibility.Visible:Visibility.Collapsed;
            if(settings is not null)
            {
                if(!drumDial.IsDragging&&!drumPending.ContainsKey("tempo"))drumDial.Tempo=settings.Tempo;
                if(!drumPending.ContainsKey("volume")){drumVolume.Value=settings.Volume;drumVolumeLabel.Text=settings.Volume.ToString();}
                if(drumDisplayedPattern!=settings.Pattern.Id)
                {
                    if(drumDisplayedPattern is null||drumStyle.SelectedIndex==drumDisplayedPattern.Value/10)
                        drumStyle.SelectedIndex=settings.Pattern.Id/10;
                    drumDisplayedPattern=settings.Pattern.Id;
                }
                drumPatternValue.Text=settings.Pattern.DisplayName;
            }
            UpdateDrumPatternButtons();
            drumLower.IsEnabled=enabled&&drumDial.Tempo>40;drumHigher.IsEnabled=enabled&&drumDial.Tempo<260;
            drumPlay.Content=drumPlaying?drumPauseIcon:"▶";
            drumPlay.ToolTip=drumPlaying?"Pause Drum":"Play Drum";AutomationProperties.SetName(drumPlay,drumPlaying?"Pause Drum":"Play Drum");
            drumSummary.Text=drumNotice??(drumStarting?"Reading Drum settings…":!connected?"Connect a supported amplifier from Amplifier Settings to use Drum.":
                !drumReady?"Read Drum to synchronize playback and settings.":"Choose a pattern, set the tempo or tap a beat. Drum keeps playing when you leave this page.");
        }
        finally{drumUpdating=false;}
    }

    private async Task StartDrumAsync()
    {
        if(operation is not null||tunerActive||drumStarting||connectedSession is not {} session||currentSnapshot is null||!editSnapshotValid)return;
        drumStarting=true;drumReady=false;drumNotice=null;drumLastTap=null;drumDisplayedPattern=null;refreshTimer.Stop();
        Begin("Reading Drum…");UpdateDrumState();
        try
        {
            while(polling)await Task.Delay(40,operation!.Token);
            var result=await session.EnterDrumAsync(currentSnapshot,operation!.Token);
            currentSnapshot=result.Current;drumPlaying=result.Playing;drumReady=true;drumWatchdog.Start();status.Text="Drum ready";
        }
        catch(Exception){drumNotice="Drum could not be synchronized. Refresh the amplifier before trying again.";editSnapshotValid=false;}
        finally{drumStarting=false;End();UpdateDrumState();}
    }

    private void TapDrum()
    {
        if(!drumDial.IsEnabled)return;
        var now=Stopwatch.GetTimestamp();
        if(drumLastTap is {} previous)
        {
            var elapsed=(long)Stopwatch.GetElapsedTime(previous,now).TotalMilliseconds;
            if(elapsed is > 0 and <= ushort.MaxValue)QueueDrumChange("tap",(int)elapsed);
        }
        drumLastTap=now;
    }

    private async void QueueDrumChange(string kind,int value)
    {
        if(drumUpdating||!drumReady||drumStarting||connectedSession is null||!editSnapshotValid||tunerActive||(!drumWriting&&operation is not null))return;
        if(kind=="tempo")drumDial.Tempo=value;
        drumPending[kind]=value;
        if(drumWriting)return;
        drumWriting=true;Begin("Updating Drum…");UpdateDrumState();
        try
        {
            while(drumPending.Count>0)
            {
                var next=drumPending.First();drumPending.Remove(next.Key);
                var session=connectedSession!;var expected=currentSnapshot!;
                if(next.Key is "play" or "tap")
                {
                    var result=next.Key=="play"?await session.SetDrumPlaybackAsync(expected,next.Value==1,operation!.Token):
                        await session.TapDrumAsync(expected,drumPlaying,(ushort)next.Value,operation!.Token);
                    currentSnapshot=result.Current;drumPlaying=result.Playing;
                }
                else currentSnapshot=next.Key switch
                {
                    "tempo"=>await session.SetDrumTempoAsync(expected,next.Value,operation!.Token),
                    "volume"=>await session.SetDrumVolumeAsync(expected,next.Value,operation!.Token),
                    "pattern"=>await session.SetDrumPatternAsync(expected,(byte)next.Value,operation!.Token),
                    _=>throw new InvalidOperationException("Unknown Drum edit")
                };
                snapshotTime=DateTimeOffset.Now;snapshot.Text=SnapshotLabel();
                UpdateDrumState();
            }
            status.Text="Drum change verified";
        }
        catch(Exception){drumWatchdog.Stop();drumPending.Clear();drumReady=false;editSnapshotValid=false;drumNotice="Drum change not confirmed. Refresh the amplifier; the command will not be replayed.";}
        finally{drumWriting=false;End();UpdateDrumState();}
    }

    private async Task<bool> LeaveDrumAsync()
    {
        drumVolumePopup.IsOpen=false;
        if(!drumReady||connectedSession is not {} session)return true;
        if(drumWriting||drumStarting||operation is not null)return false;
        drumWatchdog.Stop();
        Begin("Leaving Drum…");
        try{await session.ExitDrumAsync(operation!.Token);drumReady=false;drumLastTap=null;refreshTimer.Start();return true;}
        catch(Exception){drumNotice="Drum Tools exit was not confirmed. Reconnect before continuing.";editSnapshotValid=false;drumReady=false;return false;}
        finally{End();UpdateDrumState();}
    }

    private void ResetDrumForDisconnect()
    {drumWatchdog.Stop();drumVolumePopup.IsOpen=false;drumReady=false;drumPending.Clear();drumLastTap=null;UpdateDrumState();}
}
