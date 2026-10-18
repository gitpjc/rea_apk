using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class DrumChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run(string? renderPath=null)
    {
        var window=new MainWindow();
        T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,Flags)!.GetValue(window)!;
        void Set(string n,object? value)=>typeof(MainWindow).GetField(n,Flags)!.SetValue(window,value);
        object? Call(string n,params object[] a)=>typeof(MainWindow).GetMethod(n,Flags)!.Invoke(window,a);
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        void NoWork()=>Require(Field<CancellationTokenSource?>("operation") is null&&Field<F15ReadSession?>("connectedSession") is null,"Offline Drum initiated device work");
        var bytes=new byte[92];bytes[0]=0x12;bytes[1]=1;bytes[40]=60;bytes[41]=100;
        var original=new CurrentPresetSnapshot(1,"Offline Drum test",Convert.ToHexString(bytes),"00",[]);
        try
        {
            foreach(var n in new[]{"drumPlay","drumLower","drumHigher","drumRefresh"})
            {
                var b=Field<Button>(n);Require(!b.IsEnabled,"Disconnected Drum control enabled: "+n);
                b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));NoWork();
            }
            Set("currentSnapshot",original);Set("snapshotFirmware","V1.1.9");Set("snapshotAddress",(ulong)1);Set("editSnapshotValid",true);
            Call("UpdateDrumState");
            var dial=Field<DrumTempoDial>("drumDial");
            Require(dial.Tempo==100&&!dial.IsEnabled,"Disconnected dial did not retain read value or allowed editing");
            Require(Field<Slider>("drumVolume").Value==60,"Drum volume not read from snapshot");
            Require(Field<TextBlock>("drumPatternLabel").Text=="Current rhythm: 01 POP01","Pattern label differs");
            var style=Field<ComboBox>("drumStyle");var patterns=Field<List<Button>>("drumPatternButtons");
            Require(style.Items.Cast<string>().SequenceEqual(new[]{"Pop","Rock","Blues","Funk","Jazz","Metal","Metronome"})&&patterns.Count==10,"Style/pattern choices differ");
            for(var group=0;group<7;group++)
            {
                style.SelectedIndex=group;
                for(var number=0;number<10;number++)
                {
                    var button=patterns[number];var id=(byte)(group*10+number);
                    Require((byte)button.Tag==id&&button.Content.ToString()==$"{number+1:00}"&&button.ToolTip.ToString()==DrumPatternCatalog.Get(id).DisplayName,"Style/button mapping differs");
                    Require(AutomationProperties.GetItemStatus(button)==(id==0?"Selected":""),"Current pattern highlight differs");
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));NoWork();
                }
                Call("UpdateDrumState");Require(style.SelectedIndex==group,"State refresh reset the browsed style");
            }
            style.SelectedIndex=0;
            Require(patterns[9].ToolTip.ToString()=="10 POP010","Mobile tenth label changed");
            Require(Field<Slider>("drumVolume").Orientation==Orientation.Vertical,"Volume popup is not vertical");
            Require(AutomationProperties.GetName(Field<Button>("drumSpeaker"))=="Show Drum volume","Speaker was exposed as a mute toggle");
            var tools=(StackPanel)Field<Button>("drumNavigation").Parent;
            Require(AutomationProperties.GetName(tools)=="Tools"&&tools.Children.IndexOf(Field<Button>("tunerNavigation"))<tools.Children.IndexOf(Field<Button>("drumNavigation")),"Tools navigation order differs");
            Field<TextBox>("thresholdInput").Text="26";
            Field<Button>("drumNavigation").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var scroll=Field<ScrollViewer>("drumScroll");
            Require(scroll.Visibility==Visibility.Visible&&window.Title=="iAMP Desktop · Drum","Drum navigation failed");
            Set("drumPlaying",true);Call("UpdateDrumState");
            Require(AutomationProperties.GetName(Field<Button>("drumPlay"))=="Pause Drum","Playback icon accessibility differs");
            var peer=UIElementAutomationPeer.CreatePeerForElement(dial)!;
            var range=(IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;
            Require(range.Value==100&&range.Minimum==40&&range.Maximum==260&&range.IsReadOnly,"Dial range/accessibility differs");
            var root=(Grid)window.Content;
            foreach(var size in new[]{new Size(1280,900),new Size(911,512),new Size(683,384),new Size(620,320)})
            {
                root.Width=size.Width;root.Height=size.Height;
                for(var i=0;i<3;i++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                Require(scroll.ExtentWidth<=scroll.ViewportWidth+1,"Drum requires horizontal scrolling");
                Require(Field<Button>("libraryNavigation").ActualHeight<=40&&Field<Button>("tunerNavigation").ActualHeight<=40,"Sidebar rows are not compact");
                foreach(var n in new[]{"drumStyle","drumPatternGrid","drumPlay","drumSpeaker","drumLower","drumHigher"})
                {
                    var control=Field<FrameworkElement>(n);var p=control.TransformToAncestor(scroll).Transform(new Point());
                    Require(p.X>=-1&&p.X+control.ActualWidth<=scroll.ViewportWidth+1,"Drum control exceeds viewport: "+n);
                }
            }
            Field<Button>("tunerNavigation").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Field<ScrollViewer>("tunerScroll").Visibility==Visibility.Visible,"Tuner under Tools no longer navigates");
            Field<Button>("drumNavigation").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Field<TextBox>("thresholdInput").Text=="26"&&ReferenceEquals(Field<CurrentPresetSnapshot>("currentSnapshot"),original),"Tools navigation changed pending input or snapshot");
            NoWork();
            Set("drumReady",true);Call("CheckDrumConnection");
            Require(!Field<bool>("drumReady")&&!Field<bool>("editSnapshotValid")&&!Field<Button>("drumPlay").IsEnabled,
                "Drum connection loss retained editable stale playback");
            if(renderPath is not null)
            {
                root.Width=1280;root.Height=900;root.Background=window.Background;window.Show();
                for(var i=0;i<3;i++){root.Measure(new Size(1280,900));root.Arrange(new Rect(0,0,1280,900));root.UpdateLayout();}
                var bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var output=System.IO.File.Create(renderPath);encoder.Save(output);
            }
            Console.WriteLine("PASS: Drum Tools navigation, captured settings/70 labels, vertical volume, accessible dial/playback, disconnected guards, pending input preservation and four viewport sizes. No device commands.");
        }
        finally{Call("ResetDrumForDisconnect");window.Close();}
    }
}
