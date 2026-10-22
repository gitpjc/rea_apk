using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Iamp.Desktop;
using Iamp.Protocol;

internal static class FootswitchChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var window=new MainWindow();
        T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,flags)!.GetValue(window)!;
        object? Call(string n,params object[] args)=>typeof(MainWindow).GetMethod(n,flags)!.Invoke(window,args);
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        try {
            Call("NavigateToPageAsync",Field<ScrollViewer>("footswitchScroll"));
            Require(window.Title.EndsWith("Footswitch")&&Field<FrameworkElement>("sharedHeader").Visibility==Visibility.Visible,"Footswitch navigation/shared header differs");
            var tools=(StackPanel)Field<Button>("footswitchNavigation").Parent;
            Require(tools.Children.IndexOf(Field<Button>("footswitchNavigation"))==tools.Children.IndexOf(Field<Button>("drumNavigation"))+1,"Footswitch is not under Drum in Tools");
            Require(!Field<Button>("pairFootswitch").IsEnabled&&!Field<Button>("forgetFootswitch").IsEnabled,"Disconnected app permits pairing writes");
            Require(Field<F4PresetPanel>("footswitchPresetPanel").Parent is StackPanel,"F4 preset diagram is missing from Footswitch page");
            foreach(var (state,text) in new[]{(FootswitchConnectionStatus.Unknown,"Waiting"),(FootswitchConnectionStatus.Disconnected,"not connected"),(FootswitchConnectionStatus.Connected,"connected to amplifier")}){
                Call("SetFootswitchPresentation",true,state);Require(Field<TextBlock>("footswitchStatus").Text.Contains(text),"Footswitch status differs for "+state);
            }
            Call("SetFootswitchPresentation",false,FootswitchConnectionStatus.Connected);
            Require(Field<TextBlock>("footswitchStatus").Text=="Amplifier disconnected","Old footswitch state survives amplifier disconnection");
            var root=(FrameworkElement)window.Content;
            foreach(var size in new[]{new Size(1280,900),new Size(911,512),new Size(620,320)}){
                root.Width=size.Width;root.Height=size.Height;root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                foreach(var name in new[]{"pairFootswitch","forgetFootswitch"}){
                    var button=Field<Button>(name);var point=button.TranslatePoint(new Point(),root);
                    Require(point.X>=212&&point.X+button.ActualWidth<=size.Width+1,"Footswitch action overflows compact width");
                }
            }
            foreach(var pairing in new[]{true,false}){
                var dialog=(Window)Call("CreateFootswitchDialog",pairing)!;
                var close=(Button)dialog.FindName("FootswitchClose");var confirm=(Button)dialog.FindName("FootswitchConfirm");
                Require(close.IsDefault&&close.IsCancel&&!confirm.IsDefault,"Pair/unpair confirmation has an unsafe default");
                Require(dialog.Background==window.Background&&((ProgressBar)dialog.FindName("FootswitchProgress")).Visibility==Visibility.Collapsed,"Pairing dialog styling/initial progress differs");
                dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(()=>{
                    var content=(FrameworkElement)dialog.Content;content.UpdateLayout();
                    var bitmap=new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));Directory.CreateDirectory("analysis/private");using(var output=File.Create("analysis/private/f4-"+(pairing?"pair":"forget")+"-dialog-1.52.0.png"))encoder.Save(output);
                    // Even a stale synthetic click cannot send commands while disconnected.
                    confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                });dialog.ShowDialog();
            }
            var full=new Size(1280,900);root.Width=full.Width;root.Height=full.Height;root.Measure(full);root.Arrange(new Rect(full));root.UpdateLayout();
            var pageBitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);pageBitmap.Render(root);var pageEncoder=new PngBitmapEncoder();pageEncoder.Frames.Add(BitmapFrame.Create(pageBitmap));using(var output=File.Create("analysis/private/f4-page-offline-1.52.0.png"))pageEncoder.Save(output);
            Require(Field<Button>("cancel").Visibility==Visibility.Collapsed,"Footswitch navigation shows the header Cancel button");
            Console.WriteLine("PASS: F4 Tools navigation, distinct unknown/disconnected/connected statuses, disconnected write guards, styled pair/forget confirmations, safe defaults and compact layout. No hardware commands.");
        }
        finally{window.Close();}
    }
}
