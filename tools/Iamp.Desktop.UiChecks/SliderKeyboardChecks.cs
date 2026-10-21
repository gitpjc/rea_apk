using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class SliderKeyboardChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var previousContext=SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        var window=new MainWindow();var host=new Window {Width=460,Height=160};
        Slider? slider=null;var commits=0;var moveFocus=false;
        var input=new TextBox();var anotherControl=new TextBox();var panel=new StackPanel();
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        void Set(string name,object value)=>typeof(MainWindow).GetField(name,flags)!.SetValue(window,value);
        void Pump(){Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);}
        try {
            Set("currentSnapshot",new CurrentPresetSnapshot(1,"Keyboard check","00","00",Array.Empty<PresetEffectBlock>()));Set("editSnapshotValid",true);
            Func<Task> commit=async()=>{
                commits++;slider!.IsEnabled=false;await Task.Yield();
                panel.Children.Remove(slider);panel.Children.Add(slider);slider.IsEnabled=true;
                if(moveFocus)anotherControl.Focus();
            };
            slider=(Slider)typeof(MainWindow).GetMethod("CreateParameterSlider",flags)!.Invoke(window,[input,"Threshold",(ushort)0,(ushort)100,commit,null,null])!;
            slider.ClearValue(FrameworkElement.WidthProperty);slider.Width=350;slider.Value=50;
            panel.Children.Add(slider);panel.Children.Add(anotherControl);host.Content=panel;host.Show();host.Activate();Pump();
            slider.Focus();Require(slider.IsKeyboardFocused,"Slider cannot receive keyboard focus");
            var task=(Task)typeof(MainWindow).GetMethod("CommitSliderValueAsync",flags)!.Invoke(window,[slider])!;Pump();task.GetAwaiter().GetResult();
            Require(slider.IsKeyboardFocused,$"Live apply loses focus: focused={Keyboard.FocusedElement?.GetType().Name??"null"}, active={host.IsActive}, enabled={slider.IsEnabled}, visible={slider.IsVisible}");
            void Key(Key key,bool down){
                var source=PresentationSource.FromVisual(slider)!;
                var e=new KeyEventArgs(Keyboard.PrimaryDevice,source,Environment.TickCount,key){RoutedEvent=down?Keyboard.PreviewKeyDownEvent:Keyboard.PreviewKeyUpEvent};
                slider.RaiseEvent(e);Pump();
            }
            var before=commits;Key(System.Windows.Input.Key.Right,true);Require(slider.Value==51,"Right arrow did not advance one step");
            Key(System.Windows.Input.Key.Right,false);Require(commits==before+1,"Key release did not apply once");Require(slider.IsKeyboardFocused,"Keyboard apply lost slider focus");
            Key(System.Windows.Input.Key.Left,true);Key(System.Windows.Input.Key.Left,false);Require(slider.Value==50,"Left arrow did not reverse one step");
            slider.Value=100;Key(System.Windows.Input.Key.Right,true);Key(System.Windows.Input.Key.Right,false);Require(slider.Value==100,"Arrow exceeds slider maximum");
            Key(System.Windows.Input.Key.Home,true);Key(System.Windows.Input.Key.Home,false);Require(slider.Value==0,"Home did not select minimum");
            Key(System.Windows.Input.Key.PageUp,true);Key(System.Windows.Input.Key.PageUp,false);Require(slider.Value==10,"Page Up did not advance ten steps");
            var beforeFocusLoss=commits;Key(System.Windows.Input.Key.Right,true);anotherControl.Focus();Pump();
            Require(commits==beforeFocusLoss+1,"Leaving a keyboard adjustment unapplied requires a manual Apply button");
            slider.Focus();moveFocus=true;Key(System.Windows.Input.Key.Right,true);Key(System.Windows.Input.Key.Right,false);
            Require(anotherControl.IsKeyboardFocused,"Live apply stole focus back from another control");
            Console.WriteLine("PASS: slider focus survives live apply and rebuild; arrow steps, key-release commit and range bounds. No hardware commands.");
        }
        finally {host.Close();window.Close();SynchronizationContext.SetSynchronizationContext(previousContext);}
    }
}
