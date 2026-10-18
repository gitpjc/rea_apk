using System.IO;
using System.Reflection;
using Path=System.IO.Path;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class TunerChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run(string? renderPath=null)
    {
        var window=new MainWindow();
        T Field<T>(string name)=>(T)typeof(MainWindow).GetField(name,Flags)!.GetValue(window)!;
        void Set(string name,object? value)=>typeof(MainWindow).GetField(name,Flags)!.SetValue(window,value);
        object? Call(string name,params object[] args)=>typeof(MainWindow).GetMethod(name,Flags)!.Invoke(window,args);
        void Require(bool success,string message){if(!success)throw new InvalidOperationException(message);}
        void NoWork()
        {
            Require(Field<CancellationTokenSource?>("operation") is null && Field<F15ReadSession?>("connectedSession") is null &&
                Field<F15ReadSession?>("tunerSession") is null,"Offline tuner check initiated device work.");
        }
        void Feed(params byte[] payload)=>Call("HandleTunerPayload",(object)payload);
        var segments=Field<List<Polygon>>("tunerSegments");
        int[] LitSegments()=>segments.Select((segment,index)=>(segment,index))
            .Where(pair=>pair.segment.Fill is SolidColorBrush color && color.Color!=Color.FromRgb(0x34,0x39,0x3F)).Select(pair=>pair.index).ToArray();
        void Cleared()
        {
            Require(Field<TextBlock>("tunerNote").Text=="—" && Field<TextBlock>("tunerDeviation").Text=="No signal" && LitSegments().Length==0,
                "No-signal or invalid tuner payload retained a stale note/meter.");
        }
        void DisconnectedControls()
        {
            foreach(var name in new[]{"tunerStart","tunerStop","tunerLower","tunerHigher","tunerOutput"})
                Require(!Field<Button>(name).IsEnabled,$"Disconnected tuner enabled {name}.");
        }
        void Navigate(string name)=>Field<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var state=new byte[92];state[0]=0x12;state[1]=4;state[44]=10;state[45]=1;
        var blocks=new PresetEffectBlock[]{new(1,4,new ushort[]{1,2,25,50,0,0})};
        var original=new CurrentPresetSnapshot(4,"Offline tuner test",Convert.ToHexString(state),"00",blocks);
        try {
            DisconnectedControls();
            // Raise routed events even for disabled buttons to exercise the handler guards.
            foreach(var name in new[]{"tunerStart","tunerStop","tunerLower","tunerHigher","tunerOutput"}) {
                Field<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));NoWork();
            }
            Navigate("tunerNavigation");
            var scroll=Field<ScrollViewer>("tunerScroll");
            Require(scroll.Visibility==Visibility.Visible && Field<ScrollViewer>("libraryScroll").Visibility==Visibility.Collapsed &&
                window.Title=="iAMP Desktop · Tuner","Sidebar did not open the Tuner page.");
            NoWork();Cleared();
            Set("currentSnapshot",original);Set("snapshotFirmware","V1.1.9");Set("snapshotAddress",(ulong)1);Set("editSnapshotValid",true);
            Call("SetEffects",(object)blocks);Field<TextBox>("thresholdInput").Text="26";Call("CaptureParameterInputs");
            Call("UpdateTunerState");
            Require(Field<TextBlock>("tunerReference").Text=="440 Hz","Tuner reference is not read from the current snapshot.");
            Require(AutomationProperties.GetName(Field<Button>("tunerOutput")).Contains("muted"),"Tuner output accessibility name lost its captured Mute state.");
            DisconnectedControls();

            // Synthetic active state supplies display input only; no session is fabricated.
            Set("tunerActive",true);
            Feed(0x45,0x06,0x0C);
            Require(Field<TextBlock>("tunerNote").Text=="E" && Field<TextBlock>("tunerDeviation").Text=="Low!" && LitSegments().SequenceEqual(new[]{11}),
                "Raw E/12 did not project to the lower visible meter segment 11.");
            Feed(0x45,0x0B,0x0F);
            Require(Field<TextBlock>("tunerDeviation").Text=="Perfect!" && LitSegments().SequenceEqual(new[]{8,9}),
                "Captured mobile Perfect raw15 was incorrectly classified as Low.");
            Feed(0x45,0x0B,0x11);
            Require(Field<TextBlock>("tunerDeviation").Text=="Perfect!" && LitSegments().SequenceEqual(new[]{7,8}),
                "Upper Perfect neighbor did not mirror the center band.");
            Feed(0x45,0x0B,0x12);
            Require(Field<TextBlock>("tunerDeviation").Text=="High!" && LitSegments().SequenceEqual(new[]{6}),
                "Sharp raw18 reading did not project above the center.");
            Require(((SolidColorBrush)segments[6].Fill).Color==Color.FromRgb(0xFF,0xC0,0x01),"Observed mobile yellow differs.");
            Feed(0x45,0x0B,0x0E);
            Require(Field<TextBlock>("tunerDeviation").Text=="Low!" && LitSegments().SequenceEqual(new[]{10}),
                "Observed raw14 Low display lost its lower segment10.");
            Feed(0x45,0x0B,0x1D);
            Require(Field<TextBlock>("tunerDeviation").Text=="High!" && LitSegments().SequenceEqual(new[]{1}) &&
                ((SolidColorBrush)segments[1].Fill).Color==Color.FromRgb(0xE9,0x00,0x00),"Observed red High display differs.");
            Feed(0x45,0x06,0x09);
            Require(Field<TextBlock>("tunerDeviation").Text=="Low!" && LitSegments().SequenceEqual(new[]{12}) &&
                ((SolidColorBrush)segments[12].Fill).Color==Color.FromRgb(0xED,0x7D,0x31),"Observed orange Low display differs.");
            Feed(0x45,0x0B,0x10);
            Require(Field<TextBlock>("tunerNote").Text=="A" && Field<TextBlock>("tunerDeviation").Text=="Perfect!" && LitSegments().SequenceEqual(Enumerable.Range(0,17)) &&
                ((SolidColorBrush)segments[8].Fill).Color==Color.FromRgb(0x5C,0xE6,0x00) &&
                ((SolidColorBrush)Field<TextBlock>("tunerNote").Foreground).Color==Color.FromRgb(0x5C,0xE6,0x00),
                "Captured centered A did not match mobile's green note and full meter.");
            // Captures contain change-only readings and a 2711 ms gap between valid notes.
            // Exercise healthy watchdog checks beyond the former two-second expiry without another packet.
            var steady=System.Diagnostics.Stopwatch.StartNew();
            while(steady.Elapsed<TimeSpan.FromSeconds(3)) {
                Require((bool)Call("UpdateTunerConnectionStatus",true)!,"Healthy tuner connection was rejected.");
                System.Threading.Thread.Sleep(50);
            }
            Require(Field<TextBlock>("tunerNote").Text=="A" && LitSegments().SequenceEqual(Enumerable.Range(0,17)),
                "An unchanged valid note expired despite a healthy connection and no no-signal message.");
            Require(AutomationProperties.GetName(Field<TextBlock>("tunerNote"))=="Detected tuner note: A",
                "Accessible live note does not expose the displayed value.");
            Feed(0x45,0x00,0x00);Cleared();
            Feed(0x43,0x00);Cleared();
            Require(Field<bool>("tunerAmpInactive") && Field<TextBlock>("tunerSummary").Text.Contains("left tuner mode"),
                "An amplifier-reported tuner exit still claims active tuning.");
            Feed(0x45,0x0B,0x08);Cleared();
            Feed(0x43,0x02);Feed(0x45,0x0B,0x08);
            Require(!Field<bool>("tunerAmpInactive") && Field<TextBlock>("tunerNote").Text=="A",
                "A reported active tuner did not accept a fresh reading.");
            Feed(0x45,0x00,0x00);Cleared();
            foreach(var invalid in new byte[][]{Array.Empty<byte>(),[0x45],[0x45,0x06],[0x45,0x06,0x08,0x00],[0x45,0xFF,0x08],[0x51,0x06,0x08]}) {
                Feed(0x45,0x06,0x0C);Feed(invalid);Cleared();
            }
            Feed(0x45,0x06,0x0C);Call("UpdateTunerConnectionStatus",false);Cleared();
            Feed(0x45,0x0B,0x08);Cleared();
            Require(!Field<bool>("tunerActive"),"Disconnect reset left tuner active.");
            Require(ReferenceEquals(original,Field<CurrentPresetSnapshot>("currentSnapshot")) && Field<TextBox>("thresholdInput").Text=="26",
                "Tuner reset changed the confirmed snapshot or pending threshold.");

            Navigate("libraryNavigation");Navigate("tunerNavigation");Navigate("mixerNavigation");Navigate("tunerNavigation");
            Require(ReferenceEquals(original,Field<CurrentPresetSnapshot>("currentSnapshot")) && Field<TextBox>("thresholdInput").Text=="26" &&
                (bool)Call("HasPendingInputs")!,"Tuner navigation discarded pending effect input.");
            NoWork();
            Field<TextBox>("address").Text="000000000001";
            Call("UpdateConnectionControls");Require(Field<Button>("read").IsEnabled,"Offline address did not establish the read-button guard baseline.");
            Set("tunerActive",true);Set("editSnapshotValid",false);Call("UpdateThresholdState");Call("UpdateConnectionControls");
            foreach(var name in new[]{"read","scan","savePreset","applyThreshold","selectPreset"})Require(!Field<Button>(name).IsEnabled,$"Active tuner enabled competing action {name}.");
            Require(!Field<TextBox>("address").IsEnabled && !Field<TextBox>("thresholdInput").IsEnabled && !Field<ComboBox>("devices").IsEnabled,
                "Active tuner left connection or effect editing enabled.");
            Call("ResetTunerUiForDisconnect");Call("UpdateConnectionControls");

            var root=(Grid)window.Content;
            foreach(var size in new[]{new Size(1366,768),new Size(911,512),new Size(683,384),new Size(620,320),new Size(1920,1080)}) {
                Layout(root,size);
                foreach(var name in new[]{"tunerNote","tunerReference","tunerOutput","tunerLower","tunerHigher","tunerMeter","tunerStart","tunerStop"}) {
                    var control=Field<FrameworkElement>(name);var point=control.TransformToAncestor(scroll).Transform(new Point());
                    Require(control.ActualWidth>0 && point.X>=-1 && point.X+control.ActualWidth<=scroll.ViewportWidth+1,
                        $"Tuner {name} exceeds horizontal viewport at {size.Width}×{size.Height}.");
                }
                Require(scroll.ExtentWidth<=scroll.ViewportWidth+1,"Tuner layout requires horizontal scrolling.");
                if(size.Height<=384)Require(scroll.ScrollableHeight>0,"Small tuner viewport lost its vertical scroll range.");
            }
            NoWork();
            if(renderPath is not null) {
                Set("tunerActive",true);Feed(0x45,0x0B,0x08);Call("UpdateTunerState");root.Background=window.Background;
                root.Width=1280;root.Height=900;window.Show();Layout(root,new Size(1280,900));
                root.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                var bitmap=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(root);
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));Directory.CreateDirectory(Path.GetDirectoryName(renderPath)!);
                using var output=File.Create(renderPath);encoder.Save(output);Console.WriteLine("Rendered own-app synthetic Tuner preview: "+renderPath);
            }
            Console.WriteLine("PASS: offline Tuner navigation; disconnected handler guards; captured reference/output display; E/low and A/center segments; unchanged note held beyond two seconds; live accessible note; explicit/invalid/no-signal and connection-loss clearing; stale-after-reset rejection; pending snapshot/input preservation; competing-action guards; five viewport sizes. No amplifier or phone work.");
        }
        finally {Call("ResetTunerUiForDisconnect");window.Close();}
    }

    private static void Layout(Grid root,Size size)
    {
        for(var pass=0;pass<3;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
    }
}
