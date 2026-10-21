using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Iamp.Desktop;

public static class SteadyControlAppearance
{
    public static readonly DependencyProperty KeepEnabledAppearanceProperty=DependencyProperty.RegisterAttached(
        "KeepEnabledAppearance",typeof(bool),typeof(SteadyControlAppearance),new PropertyMetadata(false,(target,e)=>{
            if(target is Slider slider&&slider.Tag is Brush colour)
                slider.Foreground=slider.IsEnabled||(bool)e.NewValue?colour:new SolidColorBrush(Color.FromRgb(0xA3,0xAD,0xBD));
        }));
    public static bool GetKeepEnabledAppearance(DependencyObject element)=>(bool)element.GetValue(KeepEnabledAppearanceProperty);
    public static void SetKeepEnabledAppearance(DependencyObject element,bool value)=>element.SetValue(KeepEnabledAppearanceProperty,value);
}
