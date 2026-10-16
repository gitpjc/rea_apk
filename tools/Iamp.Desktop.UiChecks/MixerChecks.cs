using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class MixerChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Flags)!.GetValue(owner)!;
    private static T Property<T>(object owner,string name)=>(T)owner.GetType().GetProperty(name,Flags)!.GetValue(owner)!;
    private static void Set(object owner,string name,object? value)=>owner.GetType().GetField(name,Flags)!.SetValue(owner,value);
    private static object? Call(object owner,string name,params object?[] args)=>owner.GetType().GetMethod(name,Flags)!.Invoke(owner,args);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private sealed record Editor(object Source,MixerChannelSpec Spec,Slider Fader,TextBox Input,Button Apply,Button Mute,TextBlock Speaker);
    private static Editor[] Editors(MainWindow window)=>((IDictionary)Field<object>(window,"mixerEditors")).Values.Cast<object>()
        .Select(source=>new Editor(source,Property<MixerChannelSpec>(source,"Spec"),Property<Slider>(source,"Fader"),Property<TextBox>(source,"Input"),
            Property<Button>(source,"Apply"),Property<Button>(source,"Mute"),Property<TextBlock>(source,"Speaker"))).ToArray();
    private static void NoWork(MainWindow window,object? originalOperation=null)
        =>Require(Field<F15ReadSession?>(window,"connectedSession") is null && ReferenceEquals(Field<CancellationTokenSource?>(window,"operation"),originalOperation),"Offline mixer check started device work.");
    private static string ColorOf(Brush brush)=>((SolidColorBrush)brush).Color.ToString();

    internal static void Run(string? renderPath=null)
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-mixer-observations.json")));
        var raw=Convert.FromHexString(fixture.RootElement.GetProperty("baselineStateHex").GetString()!);
        var blocks=new PresetEffectBlock[]{
            new(1,4,new ushort[]{1,2,25,50,0,0}),new(2,1,new ushort[]{1,3,39,100,100}),
            new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1}),new(4,7,new ushort[]{1,1,17,25,500,0,0,0}),
            new(5,8,new ushort[]{1,1,20,24,35,0,0,0,0,0})
        }.Concat(Enumerable.Range(6,15).Select(i=>new PresetEffectBlock((byte)i,0,Array.Empty<ushort>()))).ToArray();
        var original=new CurrentPresetSnapshot(raw[1],"Offline Mixer",Convert.ToHexString(raw),"00",blocks);
        var window=new MainWindow();
        try {
            var editors=Editors(window);Require(editors.Length==4,"Mixer must have four channels.");
            Require(editors.Select(e=>e.Spec.Name).SequenceEqual(new[]{"Bluetooth","USB REC","Instrument","Master"}),"Mixer channel ordering or names differ.");
            void Update(bool reset=false)=>Call(window,"UpdateMixerState",reset);
            void Load(CurrentPresetSnapshot value) {
                Set(window,"currentSnapshot",value);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");
                Set(window,"editSnapshotValid",true);Set(window,"operation",null);Set(window,"polling",false);Set(window,"localSnapshotSource",false);
                Call(window,"SetEffects",(object)value.Blocks);Update(true);
            }
            void Navigate(string name)=>Field<Button>(window,name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            void Blocked(string label,object? operation=null) {
                Update();
                foreach(var editor in editors) {
                    Require(!editor.Fader.IsEnabled && !editor.Input.IsEnabled && !editor.Apply.IsEnabled && !editor.Mute.IsEnabled,$"Mixer mutation enabled during {label}.");
                    Require(((Task)Call(window,"ApplyMixerLevelAsync",editor.Spec.Channel)!).IsCompletedSuccessfully,"Blocked mixer level action started asynchronous work.");
                    Require(((Task)Call(window,"ToggleMixerMuteAsync",editor.Spec.Channel)!).IsCompletedSuccessfully,"Blocked mixer mute action started asynchronous work.");
                    Require(((Task)Call(window,"CommitMixerFaderAsync",editor.Source)!).IsCompletedSuccessfully,"Blocked mixer fader action started asynchronous work.");
                    NoWork(window,operation);
                }
            }
            Load(original);Navigate("mixerNavigation");
            foreach(var editor in editors) {
                var channel=fixture.RootElement.GetProperty("channels").EnumerateArray().Single(c=>c.GetProperty("channel").GetString()==editor.Spec.Channel.ToString());
                var offset=channel.GetProperty("offset").GetDecimal();var scale=channel.GetProperty("scale").GetDecimal();
                Require(editor.Fader.Orientation==Orientation.Vertical && editor.Fader.Minimum==0 && editor.Fader.Maximum==72 &&
                    editor.Fader.TickFrequency==1 && editor.Fader.SmallChange==1 && editor.Fader.IsSnapToTickEnabled,"Mixer fader step/range/orientation differs.");
                Require(editor.Fader.IsEnabled && editor.Mute.IsEnabled && editor.Input.IsEnabled,"Live mixer controls disabled.");
                Require(AutomationProperties.GetName(editor.Fader).Contains(editor.Spec.Name) && AutomationProperties.GetName(editor.Input).Contains("dB"),"Mixer controls lack accessible labels.");
                Require(ColorOf(editor.Fader.Foreground)==(editor.Spec.Channel==MixerChannel.Master?"#FFFFA45C":"#FF4385FF"),"Mixer channel colour differs.");
                for(byte value=0;value<=72;value++) {
                    editor.Fader.Value=value;
                    var expected=(value*scale+offset).ToString("F1",CultureInfo.InvariantCulture);
                    Require(editor.Input.Text==expected,"Mixer fader displays raw units or wrong dB value.");
                    Require(editor.Apply.IsEnabled==(value!=MixerState.Parse(raw).Get(editor.Spec.Channel).RawLevel),"Pending mixer Apply guard differs.");
                    Require((bool)Call(window,"HasPendingMixerInputs")! == (value!=MixerState.Parse(raw).Get(editor.Spec.Channel).RawLevel),"Mixer pending-value guard differs.");
                    Require(ReferenceEquals(original,Field<CurrentPresetSnapshot>(window,"currentSnapshot")),"Programmatic mixer fader changed the confirmed snapshot.");NoWork(window);
                }
                var before=editor.Fader.Value;
                foreach(var invalid in new[]{"","text","NaN","-36.25","6.25","100"}) {
                    editor.Input.Text=invalid;Require(editor.Fader.Value==before && !editor.Apply.IsEnabled && (bool)Call(window,"HasPendingMixerInputs")!,"Invalid mixer dB input was accepted or lost its guard.");
                }
                editor.Input.Text=(offset+0.5m).ToString("F1",CultureInfo.InvariantCulture);Require(editor.Fader.Value==1,"Exact numeric dB input did not move the fader.");
                editor.Input.Text=editor.Spec.Format(MixerState.Parse(raw).Get(editor.Spec.Channel).RawLevel);Update();
                var muted=raw.ToArray();muted[editor.Spec.MuteOffset]=1;Load(original with {RawStatePayloadHex=Convert.ToHexString(muted)});
                Require(editor.Fader.IsEnabled && editor.Input.IsEnabled && editor.Mute.IsEnabled && ColorOf(editor.Fader.Foreground)=="#FFA3ADBD","Muted fader must stay interactive and turn grey.");
                Require(AutomationProperties.GetName(editor.Mute).StartsWith("Unmute"),"Muted speaker action does not offer unmute.");
                editor.Fader.Value=36;Require(editor.Apply.IsEnabled && editor.Input.Text==(36*scale+offset).ToString("F1",CultureInfo.InvariantCulture),"Muted fader cannot stage a level.");NoWork(window);
                Load(original);
            }
            Set(window,"snapshotAddress",null);Blocked("offline/import target");Set(window,"snapshotAddress",(ulong)1);
            Set(window,"snapshotFirmware","V1.1.2");Blocked("unsupported firmware");Set(window,"snapshotFirmware","V1.1.9");
            Set(window,"editSnapshotValid",false);Blocked("invalid snapshot");Set(window,"editSnapshotValid",true);
            Set(window,"localSnapshotSource",true);Blocked("imported snapshot despite stale edit/target flags");Set(window,"localSnapshotSource",false);
            Set(window,"polling",true);Update();
            foreach(var editor in editors)Require(editor.Fader.IsEnabled && (bool)Call(window,"CanEditMixer",editor.Spec.Channel)!,"Read-only polling interrupted mixer gesture eligibility.");
            Set(window,"polling",false);
            using(var busy=new CancellationTokenSource()){Set(window,"operation",busy);Blocked("busy",busy);Set(window,"operation",null);}
            Load(original with {RawStatePayloadHex="00"});Blocked("unknown state schema");Load(original);
            var asymmetric=raw.ToArray();asymmetric[37]=71;Load(original with {RawStatePayloadHex=Convert.ToHexString(asymmetric)});
            foreach(var editor in editors)Require(editor.Fader.IsEnabled==(editor.Spec.Channel!=MixerChannel.Master),"Asymmetric Master must disable Master alone.");
            Require(((Task)Call(window,"ToggleMixerMuteAsync",MixerChannel.Master)!).IsCompletedSuccessfully,"Asymmetric Master mute started work.");NoWork(window);Load(original);

            Field<TextBox>(window,"thresholdInput").Text="26";Call(window,"CaptureParameterInputs");
            Set(window,"liveEditUnsaved",true);Set(window,"originalSnapshotDetail","retained-effect-baseline");
            editors[0].Input.Text="-0.5";
            foreach(var name in new[]{"libraryNavigation","informationNavigation","historyNavigation","mixerNavigation"})Navigate(name);
            Require(ReferenceEquals(original,Field<CurrentPresetSnapshot>(window,"currentSnapshot")) && Field<TextBox>(window,"thresholdInput").Text=="26" &&
                editors[0].Input.Text=="-0.5" && Field<bool>(window,"liveEditUnsaved") && Field<string>(window,"originalSnapshotDetail")=="retained-effect-baseline","Mixer navigation changed unsaved/pending effect or mixer state.");
            Require((bool)Call(window,"HasPendingInputs")!,"Mixer or effect pending guard did not survive navigation.");
            var changed=raw.ToArray();changed[32]=70;var updated=original with {RawStatePayloadHex=Convert.ToHexString(changed)};
            Set(window,"currentSnapshot",updated);Update();
            Require(editors[0].Input.Text=="-0.5" && editors[1].Input.Text=="-1.0" && Field<TextBox>(window,"thresholdInput").Text=="26","Mixer readback refresh discarded another pending channel/effect input.");
            editors[1].Input.Text="-1";changed[32]=68;Set(window,"currentSnapshot",original with {RawStatePayloadHex=Convert.ToHexString(changed)});Update();
            Require(editors[1].Input.Text=="-2.0","Equivalent numeric dB formatting blocked a confirmed refresh.");
            Update(true);Require(editors[0].Input.Text=="0.0","Explicit fresh read did not clear mixer pending input.");

            Load(original);Navigate("mixerNavigation");
            var root=(Grid)window.Content;var scroll=Field<ScrollViewer>(window,"mixerScroll");
            foreach(var size in new[]{new Size(1366,768),new Size(911,512),new Size(683,384),new Size(620,320),new Size(1920,1080)}) {
                Layout(root,size);
                foreach(var editor in editors)foreach(var control in new FrameworkElement[]{editor.Fader,editor.Input,editor.Apply,editor.Mute}) {
                    var point=control.TransformToAncestor(scroll).Transform(new Point());
                    Require(control.ActualWidth>0 && point.X>=0 && point.X+control.ActualWidth<=scroll.ViewportWidth+1,"Mixer control exceeds horizontal viewport.");
                }
                foreach(var editor in editors) {
                    editor.Fader.ApplyTemplate();var track=(Track)editor.Fader.Template.FindName("PART_Track",editor.Fader);
                    Require(track.ActualHeight>200 && Math.Abs(track.ValueFromPoint(new Point(14,14))-72)<0.1 &&
                        Math.Abs(track.ValueFromPoint(new Point(14,track.ActualHeight-14)))<0.1,"Vertical fader endpoint mapping differs.");
                }
            }
            NoWork(window);
            if(renderPath is not null) {
                var visualState=raw.ToArray();visualState[30]=57;visualState[31]=1;visualState[32]=36;visualState[34]=60;visualState[37]=visualState[38]=60;
                root.Background=window.Background;Load(original with {RawStatePayloadHex=Convert.ToHexString(visualState)});Navigate("mixerNavigation");Layout(root,new Size(1280,900));
                root.Width=1280;root.Height=900;window.Show();root.UpdateLayout();
                root.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                var bitmap=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(root);
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));Directory.CreateDirectory(Path.GetDirectoryName(renderPath)!);
                using var output=File.Create(renderPath);encoder.Save(output);Console.WriteLine("Rendered own-app offline Mixer preview: "+renderPath);
            }
            Console.WriteLine("PASS: four vertical Mixer faders; all 73 physical half-dB values per channel; muted interaction/colour; pending and navigation preservation; polling gesture continuity; busy/fw/import/schema/paired-Master guards; five sizes; programmatic changes perform no device work.");
        }
        finally {Set(window,"operation",null);window.Close();}
        var preview=new MainWindow(true);
        try {
            Set(preview,"currentSnapshot",original);Set(preview,"snapshotAddress",(ulong)1);Set(preview,"snapshotFirmware","V1.1.9");Set(preview,"editSnapshotValid",true);Call(preview,"UpdateMixerState",true);
            foreach(var editor in Editors(preview)) {
                Require(!editor.Fader.IsEnabled && !editor.Apply.IsEnabled && !editor.Mute.IsEnabled,"Preview enabled mixer hardware controls.");
                Require(((Task)Call(preview,"ToggleMixerMuteAsync",editor.Spec.Channel)!).IsCompletedSuccessfully,"Preview mute started work.");NoWork(preview);
            }
        }
        finally {preview.Close();}
    }
    private static void Layout(Grid root,Size size)
    {
        for(var pass=0;pass<3;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
    }
}
