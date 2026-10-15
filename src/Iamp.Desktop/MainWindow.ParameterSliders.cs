using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private Slider thresholdSlider = null!;
    private Slider toneSlider = null!;

    private readonly Dictionary<Slider,Func<Task>> sliderCommits=new();
    private sealed class HalfWidthConverter:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is double width?Math.Max(0,width/2):0d;
        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>Binding.DoNothing;
    }
    private static readonly ControlTemplate MobileSliderTemplate=(ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="Slider">
          <Grid Background="Transparent">
            <Border Height="10" Margin="14,0" CornerRadius="5" Background="#3D4249" VerticalAlignment="Center"/>
            <Track x:Name="PART_Track" Height="28" VerticalAlignment="Center" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}" Value="{Binding Value,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}" Orientation="{TemplateBinding Orientation}" IsDirectionReversed="{TemplateBinding IsDirectionReversed}">
              <Track.DecreaseRepeatButton>
                <RepeatButton Focusable="False" Foreground="{TemplateBinding Foreground}">
                  <RepeatButton.Template><ControlTemplate TargetType="RepeatButton"><Border Height="10" Margin="14,0,-14,0" CornerRadius="5" Background="{TemplateBinding Foreground}" VerticalAlignment="Center"/></ControlTemplate></RepeatButton.Template>
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
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.65"/></Trigger>
            <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter Property="Opacity" Value="0.9"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
        """);

    private Slider CreateParameterSlider(TextBox input, string label, ushort minimum, ushort maximum,Func<Task> commit)
    {
        var slider=new Slider {
            Minimum=minimum,Maximum=maximum,Value=minimum,
            TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=10,
            MinHeight=32,Margin=new Thickness(0,0,0,7),IsMoveToPointEnabled=true,
            HorizontalAlignment=HorizontalAlignment.Left,Template=MobileSliderTemplate,
            Foreground=Brushes.White,ToolTip=$"{label} · {minimum}–{maximum} · Drag or tap the track; release to apply. Arrow keys adjust by one."
        };
        slider.SetBinding(FrameworkElement.WidthProperty,new Binding("ActualWidth"){Source=effectDetails,Converter=new HalfWidthConverter()});
        sliderCommits.Add(slider,commit);
        slider.Tag=Brushes.White;
        slider.IsEnabledChanged+=(_,_)=>slider.Foreground=slider.IsEnabled?(Brush)slider.Tag:MutedBrush;
        AutomationProperties.SetName(slider,$"Pending {label}, {minimum} to {maximum}");
        var synchronizing=false;
        slider.ValueChanged+=(_,_)=> {
            if(synchronizing)return;
            synchronizing=true;
            try {
                var value=(ushort)Math.Round(slider.Value);
                slider.Value=value;
                input.Text=value.ToString(CultureInfo.InvariantCulture);
            }
            finally {synchronizing=false;}
        };
        input.TextChanged+=(_,_)=> {
            if(synchronizing || !ushort.TryParse(input.Text,NumberStyles.None,CultureInfo.InvariantCulture,out var value) || value<minimum || value>maximum)return;
            synchronizing=true;
            try {slider.Value=value;}
            finally {synchronizing=false;}
        };
        input.IsEnabledChanged+=(_,_)=>slider.IsEnabled=input.IsEnabled;
        slider.IsEnabled=input.IsEnabled;
        var pointerEditing=false;var keyboardEditing=false;double startingValue=0;object? startingSnapshot=null;
        void MovePointer(MouseEventArgs e) {
            slider.ApplyTemplate();
            if(slider.Template.FindName("PART_Track",slider) is Track track) {
                var value=track.ValueFromPoint(e.GetPosition(track));
                if(double.IsFinite(value))slider.Value=Math.Clamp(Math.Round(value),minimum,maximum);
            }
        }
        slider.PreviewMouseLeftButtonDown+=(_,e)=> {
            if(!slider.IsEnabled || !slider.CaptureMouse())return;
            pointerEditing=true;startingValue=slider.Value;startingSnapshot=currentSnapshot;
            slider.Focus();MovePointer(e);e.Handled=true;
        };
        slider.PreviewMouseMove+=(_,e)=> {if(pointerEditing && e.LeftButton==MouseButtonState.Pressed){MovePointer(e);e.Handled=true;}};
        slider.PreviewMouseLeftButtonUp+=async(_,e)=> {
            if(!pointerEditing)return;
            MovePointer(e);pointerEditing=false;slider.ReleaseMouseCapture();e.Handled=true;
            await CommitSliderValueAsync(slider);
        };
        slider.LostMouseCapture+=(_,_)=> {
            if(!pointerEditing)return;
            pointerEditing=false;
            if(ReferenceEquals(startingSnapshot,currentSnapshot) && editSnapshotValid && operation is null)slider.Value=startingValue;
        };
        bool AdjustmentKey(Key key)=>key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown;
        slider.PreviewKeyDown+=(_,e)=> {
            if(pointerEditing && e.Key==Key.Escape) {
                pointerEditing=false;slider.ReleaseMouseCapture();
                if(ReferenceEquals(startingSnapshot,currentSnapshot))slider.Value=startingValue;
                e.Handled=true;return;
            }
            if(pointerEditing && AdjustmentKey(e.Key)){e.Handled=true;return;}
            if(slider.IsEnabled && AdjustmentKey(e.Key))keyboardEditing=true;
        };
        slider.PreviewKeyUp+=async(_,e)=> {
            if(!keyboardEditing || !AdjustmentKey(e.Key))return;
            keyboardEditing=false;await CommitSliderValueAsync(slider);
        };
        slider.LostKeyboardFocus+=(_,_)=>keyboardEditing=false;
        return slider;
    }

    private async Task CommitSliderValueAsync(Slider slider)
    {
        if(preview || operation is not null || !slider.IsEnabled || slider.Parent is null || !editSnapshotValid)return;
        if(sliderCommits.TryGetValue(slider,out var commit))await commit();
    }

    private void AddMobileParameterSlider(Slider slider,TextBox input,string label,string color)
    {
        slider.Tag=Brush(color);slider.Foreground=slider.IsEnabled?(Brush)slider.Tag:MutedBrush;
        var unit=label.EndsWith(" (ms)",StringComparison.Ordinal)?"ms":"";
        var heading=new Grid {Margin=new Thickness(0,12,0,0),HorizontalAlignment=HorizontalAlignment.Left};
        heading.SetBinding(FrameworkElement.WidthProperty,new Binding("Width"){Source=slider});
        heading.ColumnDefinitions.Add(new ColumnDefinition());heading.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        heading.Children.Add(new TextBlock {Text=label.Replace(" (ms)",""),Foreground=Brushes.White,FontFamily=new FontFamily("Segoe UI"),FontSize=14,FontWeight=FontWeights.ExtraBold,TextTrimming=TextTrimming.CharacterEllipsis});
        var value=Text("",14);value.SetBinding(TextBlock.TextProperty,new Binding("Text"){Source=input,StringFormat="{0}"+unit});
        Grid.SetColumn(value,1);heading.Children.Add(value);effectDetails.Children.Add(heading);effectDetails.Children.Add(slider);
    }

    private static void DetachSlider(Slider slider)
    {
        if(slider.Parent is Panel parent)parent.Children.Remove(slider);
    }
}
