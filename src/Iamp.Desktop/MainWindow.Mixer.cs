using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button mixerNavigation=Button("Mixer");
    private readonly ScrollViewer mixerScroll=new() {
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed
    };
    private readonly TextBlock mixerSummary=Text("Connect the amplifier from Amplifier Settings to read the mixer.",13,MutedBrush);
    private readonly Dictionary<MixerChannel,MixerEditor> mixerEditors=new();
    private MixerState? displayedMixerState;
    private bool mixerUpdating;
    private bool mixerGestureActive=>mixerEditors.Values.Any(e=>e.PointerEditing || e.KeyboardEditing);

    private sealed class MixerEditor(MixerChannelSpec spec,Brush color)
    {
        internal MixerChannelSpec Spec {get;}=spec;
        internal Brush Color {get;}=color;
        internal Slider Fader {get;}=new() {
            Orientation=Orientation.Vertical,Minimum=0,Maximum=72,Value=0,
            TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=6,
            IsDirectionReversed=false,IsMoveToPointEnabled=true,Width=58,Height=260,
            HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,12,0,12),
            Template=MixerFaderTemplate
        };
        internal TextBox Input {get;}=new() {
            Width=80,MaxLength=6,HorizontalContentAlignment=HorizontalAlignment.Center,
            Background=Brush("#15191F"),Foreground=Brushes.White,BorderBrush=Brush("#46505E"),
            Padding=new Thickness(7),CaretBrush=Brushes.White,Text="—"
        };
        internal Button Apply {get;}=Button("Apply");
        internal Button Mute {get;}=Button("");
        internal TextBlock Speaker {get;}=new() {FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=24,Text="\uE767"};
        internal TextBlock Message {get;}=Text("Not read",11,MutedBrush,new Thickness(0,8,0,0));
        internal bool Synchronizing,PointerEditing,KeyboardEditing;
        internal double StartingValue;
        internal object? StartingSnapshot;
    }

    private static readonly ControlTemplate MixerFaderTemplate=(ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="Slider">
          <Grid Background="Transparent">
            <Border Width="10" Margin="0,14" CornerRadius="5" Background="#3D4249" HorizontalAlignment="Center"/>
            <Track x:Name="PART_Track" Width="28" HorizontalAlignment="Center" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}" Value="{Binding Value,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}" Orientation="Vertical" IsDirectionReversed="False">
              <Track.DecreaseRepeatButton>
                <RepeatButton Focusable="False" Foreground="{TemplateBinding Foreground}">
                  <RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Width="10" Margin="0,-14,0,14" CornerRadius="5" Background="{TemplateBinding Foreground}" HorizontalAlignment="Center"/></ControlTemplate></RepeatButton.Template>
                </RepeatButton>
              </Track.DecreaseRepeatButton>
              <Track.Thumb>
                <Thumb Width="28" Height="28" Foreground="{TemplateBinding Foreground}" Focusable="False">
                  <Thumb.Template><ControlTemplate TargetType="Thumb"><Grid><Ellipse Fill="White"/><Ellipse Width="8" Height="8" Fill="{TemplateBinding Foreground}"/></Grid></ControlTemplate></Thumb.Template>
                </Thumb>
              </Track.Thumb>
              <Track.IncreaseRepeatButton><RepeatButton Focusable="False"><RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton>
            </Track>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.55"/></Trigger>
            <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter Property="Opacity" Value="0.9"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
        """);

    private void BuildMixerPage()
    {
        if(mixerScroll.Content is not null)return;
        var page=new StackPanel {Margin=new Thickness(32,20,32,24)};
        page.Children.Add(Text("Mixer",30));
        page.Children.Add(Text("Adjust Bluetooth, USB recording, instrument and master levels.",14,MutedBrush,new Thickness(0,7,0,18)));
        page.Children.Add(mixerSummary);
        page.Children.Add(Text("Drag a fader and release to apply, or enter a level and choose Apply. Arrow keys change 0.5 dB; release the key to apply. Use the speaker button to mute or unmute.",12,MutedBrush,new Thickness(0,10,0,18)));
        var channels=new UniformGrid {Columns=4};page.Children.Add(channels);
        page.SizeChanged+=(_,_)=>channels.Columns=page.ActualWidth<360?1:page.ActualWidth<720?2:4;
        foreach(var spec in MixerState.Specs) {
            var editor=new MixerEditor(spec,spec.Channel==MixerChannel.Master?Brush("#FFA45C"):Brush("#4385ff"));
            mixerEditors.Add(spec.Channel,editor);
            var card=new StackPanel {HorizontalAlignment=HorizontalAlignment.Stretch};
            var title=Text(spec.Name,16);title.FontWeight=FontWeights.Bold;title.TextAlignment=TextAlignment.Center;card.Children.Add(title);
            var limits=Text($"{spec.Format(spec.Maximum)} dB",12,MutedBrush,new Thickness(0,12,0,0));limits.TextAlignment=TextAlignment.Center;card.Children.Add(limits);
            card.Children.Add(editor.Fader);
            var minimum=Text($"{spec.Format(spec.Minimum)} dB",12,MutedBrush);minimum.TextAlignment=TextAlignment.Center;card.Children.Add(minimum);
            var entry=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,14,0,0)};
            entry.Children.Add(editor.Input);entry.Children.Add(Text("dB",12,MutedBrush,new Thickness(6,8,0,0)));card.Children.Add(entry);
            editor.Apply.HorizontalAlignment=HorizontalAlignment.Center;editor.Apply.Margin=new Thickness(0,8,0,0);card.Children.Add(editor.Apply);
            editor.Mute.Content=editor.Speaker;editor.Mute.Padding=new Thickness(12,8,12,8);editor.Mute.HorizontalAlignment=HorizontalAlignment.Center;editor.Mute.Margin=new Thickness(0,12,0,0);card.Children.Add(editor.Mute);
            editor.Message.TextAlignment=TextAlignment.Center;card.Children.Add(editor.Message);
            channels.Children.Add(new Border {Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(10,16,10,16),Margin=new Thickness(0,0,10,12),Child=card});
            InitializeMixerEditor(editor);
        }
        mixerScroll.Content=page;UpdateMixerState();
    }

    private void InitializeMixerEditor(MixerEditor editor)
    {
        var name=editor.Spec.Name;
        AutomationProperties.SetName(editor.Fader,$"{name} mixer level, 0.5 dB steps");
        AutomationProperties.SetName(editor.Input,$"{name} level in dB");
        AutomationProperties.SetName(editor.Apply,$"Apply {name} level");
        editor.Fader.ToolTip=$"{name} · {editor.Spec.Format(0)} to {editor.Spec.Format(72)} dB · Release to apply";
        editor.Input.ToolTip="Enter a level in 0.5 dB steps. Press Enter or choose Apply.";
        editor.Fader.ValueChanged+=(_,_)=> {
            if(editor.Synchronizing)return;
            editor.Synchronizing=true;
            try {editor.Fader.Value=Math.Clamp(Math.Round(editor.Fader.Value),0,72);editor.Input.Text=editor.Spec.Format((byte)editor.Fader.Value);}
            finally {editor.Synchronizing=false;}
            UpdateMixerState();
        };
        editor.Input.TextChanged+=(_,_)=> {
            if(editor.Synchronizing)return;
            editor.Synchronizing=true;
            try {if(editor.Spec.TryParse(editor.Input.Text,out var raw))editor.Fader.Value=raw;}
            finally {editor.Synchronizing=false;}
            UpdateMixerState();
        };
        editor.Apply.Click+=async(_,_)=>await ApplyMixerLevelAsync(editor.Spec.Channel);
        editor.Mute.Click+=async(_,_)=>await ToggleMixerMuteAsync(editor.Spec.Channel);
        editor.Input.PreviewKeyDown+=async(_,e)=> {
            if(e.Key!=Key.Enter)return;e.Handled=true;await ApplyMixerLevelAsync(editor.Spec.Channel);
        };
        void MovePointer(MouseEventArgs e) {
            editor.Fader.ApplyTemplate();
            if(editor.Fader.Template.FindName("PART_Track",editor.Fader) is Track track) {
                var value=track.ValueFromPoint(e.GetPosition(track));
                if(double.IsFinite(value))editor.Fader.Value=Math.Clamp(Math.Round(value),0,72);
            }
        }
        editor.Fader.PreviewMouseLeftButtonDown+=(_,e)=> {
            if(!CanEditMixer(editor.Spec.Channel) || !editor.Fader.IsEnabled || !editor.Fader.CaptureMouse())return;
            editor.PointerEditing=true;editor.StartingValue=editor.Fader.Value;editor.StartingSnapshot=currentSnapshot;
            editor.Fader.Focus();MovePointer(e);e.Handled=true;
        };
        editor.Fader.PreviewMouseMove+=(_,e)=> {if(editor.PointerEditing && e.LeftButton==MouseButtonState.Pressed){MovePointer(e);e.Handled=true;}};
        editor.Fader.PreviewMouseLeftButtonUp+=async(_,e)=> {
            if(!editor.PointerEditing)return;
            MovePointer(e);editor.PointerEditing=false;editor.Fader.ReleaseMouseCapture();e.Handled=true;
            await CommitMixerFaderAsync(editor);
        };
        editor.Fader.LostMouseCapture+=(_,_)=> {
            if(!editor.PointerEditing)return;editor.PointerEditing=false;
            if(ReferenceEquals(editor.StartingSnapshot,currentSnapshot) && editSnapshotValid && operation is null)editor.Fader.Value=editor.StartingValue;
        };
        static bool AdjustmentKey(Key key)=>key is Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown;
        editor.Fader.PreviewKeyDown+=(_,e)=> {
            if(editor.PointerEditing && e.Key==Key.Escape) {
                editor.PointerEditing=false;editor.Fader.ReleaseMouseCapture();
                if(ReferenceEquals(editor.StartingSnapshot,currentSnapshot))editor.Fader.Value=editor.StartingValue;
                e.Handled=true;return;
            }
            if(editor.PointerEditing && AdjustmentKey(e.Key)){e.Handled=true;return;}
            if(editor.Fader.IsEnabled && AdjustmentKey(e.Key))editor.KeyboardEditing=true;
        };
        editor.Fader.PreviewKeyUp+=async(_,e)=> {
            if(!editor.KeyboardEditing || !AdjustmentKey(e.Key))return;
            editor.KeyboardEditing=false;await CommitMixerFaderAsync(editor);
        };
        editor.Fader.LostKeyboardFocus+=(_,_)=>editor.KeyboardEditing=false;
    }

    private MixerState? ReadDisplayedMixer()
    {
        if(currentSnapshot is null)return null;
        try {return MixerState.Parse(Convert.FromHexString(currentSnapshot.RawStatePayloadHex));}
        catch(Exception ex) when(ex is FormatException or NotSupportedException){return null;}
    }
    private bool CanEditMixer(MixerChannel channel)
        =>!preview && !localSnapshotSource && operation is null && editSnapshotValid && snapshotAddress.HasValue && snapshotFirmware=="V1.1.9" &&
            ReadDisplayedMixer()?.Get(channel) is {} state && (channel!=MixerChannel.Master || state.HasMatchingLevels);

    private void UpdateMixerState(bool resetPending=false)
    {
        if(mixerUpdating || mixerEditors.Count==0)return;
        mixerUpdating=true;
        try {
            var state=ReadDisplayedMixer();
            mixerSummary.Text=preview?"Preview only · No amplifier commands":operation is not null?"Mixer operation in progress…":
                !editSnapshotValid || snapshotAddress is null?"Connect or refresh from Amplifier Settings before changing mixer levels.":
                snapshotFirmware!="V1.1.9" || state is null?"Mixer controls require a supported F15i V1.1.9 state.":
                "Live mixer · Muted channels remain adjustable. Effect settings and unsaved preset edits are preserved.";
            foreach(var editor in mixerEditors.Values) {
                var confirmed=state?.Get(editor.Spec.Channel);var previous=displayedMixerState?.Get(editor.Spec.Channel);
                if(confirmed is not null && (resetPending || previous is null || state!.WireIndex!=displayedMixerState!.WireIndex ||
                    (!editor.PointerEditing && !editor.KeyboardEditing && previous.Spec.TryParse(editor.Input.Text,out var previousRaw) && previousRaw==previous.RawLevel))) {
                    editor.Synchronizing=true;
                    try {editor.Input.Text=editor.Spec.Format(confirmed.RawLevel);editor.Fader.Value=confirmed.RawLevel;}
                    finally {editor.Synchronizing=false;}
                }
                var supported=CanEditMixer(editor.Spec.Channel);
                editor.Input.IsEnabled=editor.Fader.IsEnabled=editor.Mute.IsEnabled=supported;
                var valid=editor.Spec.TryParse(editor.Input.Text,out var pending);
                editor.Apply.IsEnabled=supported && valid && pending!=confirmed!.RawLevel;
                var muted=confirmed?.Muted==true;
                editor.Fader.Foreground=muted?MutedBrush:editor.Color;
                editor.Speaker.Foreground=muted?MutedBrush:editor.Color;editor.Speaker.Text=muted?"\uE74F":"\uE767";
                editor.Mute.ToolTip=$"{(muted?"Unmute":"Mute")} {editor.Spec.Name}";
                AutomationProperties.SetName(editor.Mute,$"{(muted?"Unmute":"Mute")} {editor.Spec.Name}");
                editor.Message.Text=confirmed is null?"Not read":!confirmed.HasMatchingLevels?"Paired Master levels differ · Editing unavailable":
                    !valid?$"Enter {editor.Spec.Format(0)} to {editor.Spec.Format(72)} in 0.5 dB steps":
                    pending!=confirmed.RawLevel?$"Pending {editor.Spec.Format(pending)} dB":muted?"Muted":"";
            }
            if(state is not null)displayedMixerState=state;
        }
        finally {mixerUpdating=false;}
        UpdateSaveState();
    }

    private bool HasPendingMixerInputs()
    {
        if(ReadDisplayedMixer() is not {} state)return false;
        return mixerEditors.Values.Any(editor=>state.Get(editor.Spec.Channel) is {} confirmed && confirmed.HasMatchingLevels &&
            (!editor.Spec.TryParse(editor.Input.Text,out var value) || value!=confirmed.RawLevel));
    }

    private Task CommitMixerFaderAsync(MixerEditor editor)
        =>editor.Fader.IsEnabled && editor.Fader.Parent is not null?ApplyMixerLevelAsync(editor.Spec.Channel):Task.CompletedTask;
    private Task ApplyMixerLevelAsync(MixerChannel channel)
    {
        lastMutationSucceeded=false;
        if(!CanEditMixer(channel) || !mixerEditors.TryGetValue(channel,out var editor) ||
            !editor.Spec.TryParse(editor.Input.Text,out var raw) || ReadDisplayedMixer() is not {} state || raw==state.Get(channel).RawLevel)
            return Task.CompletedTask;
        return ChangeMixerAsync(channel,raw,null);
    }
    private Task ToggleMixerMuteAsync(MixerChannel channel)
    {
        lastMutationSucceeded=false;
        if(!CanEditMixer(channel) || mixerGestureActive || ReadDisplayedMixer() is not {} state)return Task.CompletedTask;
        return ChangeMixerAsync(channel,null,!state.Get(channel).Muted);
    }
    private async Task ChangeMixerAsync(MixerChannel channel,byte? level,bool? muted)
    {
        if(!CanEditMixer(channel) || currentSnapshot is not {} expected || snapshotAddress is not {} target)return;
        var name=MixerState.GetSpec(channel).Name;
        Begin($"Checking amplifier before changing {name} mixer…");UpdateMixerState();
        try {
            var confirmed=await UseSessionAsync(target,session=>level is {} raw?session.SetMixerLevelAsync(expected,channel,raw,operation!.Token):
                session.SetMixerMuteAsync(expected,channel,muted!.Value,operation!.Token));
            // Mixer writes alter only whole-state fields. Keep effect controls, their pending
            // inputs and the unsaved-preset baseline exactly as they were.
            currentSnapshot=confirmed;editSnapshotValid=true;snapshotTime=DateTimeOffset.Now;
            snapshot.Text=SnapshotLabel();status.Text=level is {} applied?$"{name} {MixerState.GetSpec(channel).Format(applied)} dB verified":
                $"{name} {(muted==true?"muted":"unmuted")} verified";
            lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {
            editSnapshotValid=false;status.Text="Mixer change interrupted · Outcome may be unknown. Refresh the amplifier before another change.";snapshot.Text=SnapshotLabel();
        }
        catch(Exception) {
            editSnapshotValid=false;status.Text="Mixer change not confirmed. Refresh the amplifier before another change.";snapshot.Text=SnapshotLabel();
        }
        finally {status.Foreground=MutedBrush;End();UpdateMixerState();}
    }
}
