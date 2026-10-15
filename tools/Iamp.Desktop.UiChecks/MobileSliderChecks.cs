using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Iamp.Desktop;

internal static class MobileSliderChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    static void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,args);
    private sealed class KeySource:PresentationSource
    {
        public override Visual RootVisual {get;set;}=null!;
        public override bool IsDisposed=>false;
        protected override CompositionTarget GetCompositionTargetCore()=>null!;
    }
    internal static void Run(MainWindow window,IEnumerable<(Slider Slider,TextBox Input,Button Apply,byte Position,int Index)> controls)
    {
        void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        var commits=Field<Dictionary<Slider,Func<Task>>>(window,"sliderCommits");var keySource=new KeySource();
        var root=(FrameworkElement)window.Content;var details=Field<StackPanel>(window,"effectDetails");
        void Select(byte p)=>((Button)Field<WrapPanel>(window,"effectChain").Children[p-1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        foreach(var c in controls) {
            Select(c.Position);var original=c.Slider.Value;var saved=commits[c.Slider];var calls=0;
            commits[c.Slider]=()=>{calls++;return Task.CompletedTask;};
            try {
                foreach(var size in new[]{new Size(1366,768),new Size(911,512),new Size(683,384),new Size(620,320),new Size(1920,1080)}) {
                    for(var pass=0;pass<4;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                    Require(Math.Abs(c.Slider.ActualWidth-details.ActualWidth/2)<0.75,"Slider is not half its effect-panel width.");
                    c.Slider.ApplyTemplate();var track=(Track)c.Slider.Template.FindName("PART_Track",c.Slider);
                    Require(track.Thumb.ActualWidth==28 && track.Thumb.ActualHeight==28,"Mobile thumb geometry differs.");
                    Require(((SolidColorBrush)track.DecreaseRepeatButton.Foreground).Color==((SolidColorBrush)c.Slider.Foreground).Color,"Filled track does not use effect colour.");
                    var button=(Button)Field<WrapPanel>(window,"effectChain").Children[c.Position-1];var category=((StackPanel)button.Content).Children.OfType<TextBlock>().First();
                    Require(((SolidColorBrush)category.Foreground).Color==((SolidColorBrush)c.Slider.Foreground).Color,"Slider and effect icon colours differ.");
                    var thumbGrid=(Grid)VisualTreeHelper.GetChild(track.Thumb,0);var circles=thumbGrid.Children.OfType<Ellipse>().ToArray();
                    Require(circles.Length==2 && ((SolidColorBrush)circles[0].Fill).Color==Colors.White && circles[1].Width==8 && ((SolidColorBrush)circles[1].Fill).Color==((SolidColorBrush)c.Slider.Foreground).Color,"White thumb/coloured centre differs.");
                    var min=Math.Clamp(Math.Round(track.ValueFromPoint(new Point(0,14))),c.Slider.Minimum,c.Slider.Maximum);
                    var max=Math.Clamp(Math.Round(track.ValueFromPoint(new Point(track.ActualWidth,14))),c.Slider.Minimum,c.Slider.Maximum);
                    Require(min==c.Slider.Minimum && max==c.Slider.Maximum,"Tap/drag endpoint mapping differs.");
                }
                c.Slider.Value=original==c.Slider.Maximum?original-1:original+1;
                Require(calls==0,"Programmatic synchronization automatically applied a value.");
                c.Slider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,keySource,0,Key.Right){RoutedEvent=Keyboard.PreviewKeyDownEvent});
                c.Slider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,keySource,1,Key.Right){RoutedEvent=Keyboard.PreviewKeyUpEvent});
                Require(calls==1,"Keyboard release did not commit once.");
                c.Slider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,keySource,2,Key.Right){RoutedEvent=Keyboard.PreviewKeyUpEvent});
                Require(calls==1,"Keyboard release repeated a completed gesture.");
                c.Slider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,keySource,3,Key.Right){RoutedEvent=Keyboard.PreviewKeyDownEvent});
                c.Slider.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice,4,c.Slider,null){RoutedEvent=Keyboard.LostKeyboardFocusEvent});
                c.Slider.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,keySource,5,Key.Right){RoutedEvent=Keyboard.PreviewKeyUpEvent});
                Require(calls==1,"Cancelled keyboard gesture replayed after focus loss.");
                var heading=(Grid)details.Children[details.Children.IndexOf(c.Slider)-1];var value=heading.Children.OfType<TextBlock>().Last();
                Require(value.Text==c.Input.Text+((c.Position==4&&c.Index==4)||(c.Position==5&&c.Index==2)?"ms":""),"Live value/unit did not follow slider input.");
                c.Slider.Value=original;Call(window,"Begin","Offline mobile slider guard");
                Call(window,"CommitSliderValueAsync",c.Slider);Require(calls==1,"Busy slider committed.");Call(window,"End");
                Select((byte)(c.Position==5?1:c.Position+1));Call(window,"CommitSliderValueAsync",c.Slider);Require(calls==1,"Detached slider committed.");
            }
            finally {commits[c.Slider]=saved;c.Slider.Value=original;}
        }
        Console.WriteLine("PASS: all 18 half-width mobile sliders at five sizes; rounded-track/white-thumb/icon-colour/live-value geometry; tap/drag endpoint mapping; single keyboard-release commit; programmatic/busy/detached no-commit guards.");
    }
}
