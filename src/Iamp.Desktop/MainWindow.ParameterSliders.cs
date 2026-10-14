using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private Slider thresholdSlider = null!;
    private Slider toneSlider = null!;

    // Sliders change pending input only. The existing Apply handlers own all device writes.
    private static Slider CreateParameterSlider(TextBox input, string label, ushort minimum, ushort maximum)
    {
        var slider=new Slider {
            Minimum=minimum,Maximum=maximum,Value=minimum,
            TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=10,
            MinHeight=28,Margin=new Thickness(0,0,0,7),
            HorizontalAlignment=HorizontalAlignment.Stretch,
            Foreground=Brushes.White,ToolTip=$"Pending {label} · {minimum}–{maximum} · Choose Apply to send to the amplifier"
        };
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
        return slider;
    }

    private static void DetachSlider(Slider slider)
    {
        if(slider.Parent is Panel parent)parent.Children.Remove(slider);
    }
}
