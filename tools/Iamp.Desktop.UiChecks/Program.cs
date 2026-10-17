using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

// Offline WPF interactions only: no window capture, phone access or amplifier commands.
internal static class Program
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    static T Property<T>(object o,string n)=>(T)o.GetType().GetProperty(n,Flags)!.GetValue(o)!;
    static void Set(object o,string n,object? v)=>o.GetType().GetField(n,Flags)!.SetValue(o,v);
    static object? Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,args);
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    [STAThread]
    static int Main(string[] args)
    {
        var app=new Application();var window=new MainWindow();
        try {
            if(args.Contains("--preset-sync")) {PresetSyncChecks.Run();return 0;}
            if(args.Contains("--save")) {PresetSaveChecks.Run();EffectCacheStoreChecks.Run();return 0;}
            if(args.Contains("--mixer")) {MixerChecks.Run(args.Contains("--render")?System.IO.Path.GetFullPath("analysis/private/mixer-desktop-preview.png"):null);return 0;}
            if(args.Contains("--eq")) {EqModelChecks.Run();PresetJsonChecks.Run();DynaModelChecks.Run();DynaModelChecks.RunFilter();ReorderedParameterChecks.Run();return 0;}
            if(args.Contains("--amp")) {AmpModelChecks.Run();PresetJsonChecks.Run();ReorderedParameterChecks.Run();return 0;}
            if(args.Contains("--od")) {OdModelChecks.Run();PresetJsonChecks.Run();ReorderedParameterChecks.Run();return 0;}
            if(args.Contains("--delay")) {DelayModelChecks.Run();PresetJsonChecks.Run();ReorderedParameterChecks.Run();return 0;}
            if(args.Contains("--mod")) {ModModelChecks.Run();PresetJsonChecks.Run();ReorderedParameterChecks.Run();return 0;}
            if(args.Contains("--reverb")) {ReverbModelChecks.Run();PresetJsonChecks.Run();ReorderedParameterChecks.Run();return 0;}
            var blocks=new PresetEffectBlock[]{
                new(1,4,new ushort[]{1,2,25,50,0,0}),new(2,1,new ushort[]{1,3,39,100,100}),
                new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1}),new(4,7,new ushort[]{1,1,17,25,500,0,0,0}),
                new(5,8,new ushort[]{1,1,20,24,35,0,0,0,0,0})
            };
            var snapshot=new CurrentPresetSnapshot(4,"Offline slider test","00","00",blocks);
            Set(window,"currentSnapshot",snapshot);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);
            Call(window,"SetEffects",(object)blocks);
            var controls=new List<(Slider Slider,TextBox Input,Button Apply,byte Position,int Index)> {
                (Field<Slider>(window,"thresholdSlider"),Field<TextBox>(window,"thresholdInput"),Field<Button>(window,"applyThreshold"),1,2),
                (Field<Slider>(window,"toneSlider"),Field<TextBox>(window,"toneInput"),Field<Button>(window,"applyTone"),2,3)
            };
            foreach(var editor in (System.Collections.IEnumerable)window.GetType().GetProperty("AdditionalEditors",Flags)!.GetValue(window)!)
                if(Property<byte>(editor,"ProfilePosition")<=5)controls.Add((Property<Slider>(editor,"Slider"),Property<TextBox>(editor,"Input"),Property<Button>(editor,"Apply"),Property<byte>(editor,"Position"),Property<int>(editor,"ParameterIndex")));
            void Select(byte position)=>((Button)Field<WrapPanel>(window,"effectChain").Children[position-1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            void Update()=>Call(window,"UpdateThresholdState");
            var sizes=new[]{new Size(1366,768),new Size(911,512),new Size(683,384),new Size(620,320),new Size(1920,1080)};
            foreach(var c in controls) {
                Select(c.Position);var original=blocks[c.Position-1].Values[c.Index];
                Require(c.Slider.Parent is not null && c.Slider.Value==original && !c.Apply.IsEnabled,"Initial slider/value state differs.");
                Require(c.Slider.SmallChange==1 && c.Slider.IsSnapToTickEnabled && c.Slider.TickFrequency==1,"Whole-number keyboard/drag settings differ.");
                Require(AutomationProperties.GetName(c.Slider).StartsWith("Pending "),"Slider has no accessible name.");
                for(var v=c.Slider.Minimum;v<=c.Slider.Maximum;v++) {
                    c.Slider.Value=v;Require(c.Input.Text==v.ToString(),"Slider/input synchronization differs.");
                    Require(c.Apply.IsEnabled==(v!=original),"Slider pending Apply state differs.");
                    Require((bool)Call(window,"HasPendingInputs")! == (v!=original),"Pending guard differs.");
                    Require(ReferenceEquals(snapshot,Field<CurrentPresetSnapshot>(window,"currentSnapshot")),"Slider changed confirmed amplifier state.");
                    Require(Field<CancellationTokenSource?>(window,"operation") is null && Field<F15ReadSession?>(window,"connectedSession") is null,"Slider started device work.");
                }
                c.Input.Text=(c.Slider.Minimum+1).ToString();Require(c.Slider.Value==c.Slider.Minimum+1,"Numeric edit did not update slider.");
                var retained=c.Slider.Value;
                foreach(var invalid in new[]{"", "-1", "text", (c.Slider.Maximum+1).ToString()}) {
                    c.Input.Text=invalid;Require(c.Slider.Value==retained && !c.Apply.IsEnabled,"Invalid numeric input was overwritten or accepted.");
                }
                c.Slider.Value=c.Slider.Minimum+2.4;Require(c.Input.Text==(c.Slider.Minimum+2).ToString() && c.Slider.Value==c.Slider.Minimum+2,"Fractional automation value was not normalized.");
                var pending=c.Input.Text;
                Select((byte)(c.Position==5?1:c.Position+1));Select(c.Position);
                Require(c.Input.Text==pending,"Browsing effect cards discarded pending input.");
                c.Input.Text="";Select((byte)(c.Position==5?1:c.Position+1));
                Require((bool)Call(window,"HasPendingInputs")! && !Field<Button>(window,"effectPowerButton").IsEnabled,"Hidden invalid pending input lost its guard.");
                Select(c.Position);Require(c.Input.Text=="","Browsing discarded invalid pending input.");
                c.Input.Text=original.ToString();Update();
                Call(window,"Begin","Offline slider check");Require(!c.Slider.IsEnabled,"Busy slider enabled.");Call(window,"End");
                Set(window,"snapshotFirmware","V1.1.2");Update();Require(!c.Slider.IsEnabled,"Unsupported firmware slider enabled.");Set(window,"snapshotFirmware","V1.1.9");Update();
                Set(window,"editSnapshotValid",false);Update();Require(!c.Slider.IsEnabled,"Invalid snapshot slider enabled.");Set(window,"editSnapshotValid",true);Update();
                var root=(FrameworkElement)window.Content;var scroll=Field<ScrollViewer>(window,"libraryScroll");
                foreach(var size in sizes) {
                    for(var pass=0;pass<3;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                    foreach(var control in new FrameworkElement[]{c.Slider,c.Input,c.Apply}) {
                        var point=control.TransformToAncestor(scroll).Transform(new Point());
                        Require(control.ActualWidth>0 && point.X>=0 && point.X+control.ActualWidth<=scroll.ViewportWidth+1,"Parameter control exceeds horizontal viewport.");
                    }
                }
            }
            Select(1);Field<TextBox>(window,"thresholdInput").Text="26";
            var od=controls.Single(c=>c.Position==2 && c.Index==2);Select(2);od.Input.Text="40";
            var time=controls.Single(c=>c.Position==4 && c.Index==4);Select(4);time.Input.Text="1000";
            var hall=controls.Single(c=>c.Position==5 && c.Index==5);Select(5);hall.Input.Text="bad";
            var confirmed=blocks.ToArray();var noise=confirmed[0].Values.ToArray();noise[2]=26;confirmed[0]=confirmed[0] with {Values=noise};
            Set(window,"currentSnapshot",snapshot with {Blocks=confirmed});Call(window,"SetEffectsPreservingPending",(object)confirmed);
            Require(!Field<Button>(window,"applyThreshold").IsEnabled,"Applied parameter remains pending.");
            Select(2);Require(od.Input.Text=="40" && od.Apply.IsEnabled,"Applying a parameter discarded another effect's pending edit.");
            Select(4);Require(time.Input.Text=="1000" && time.Slider.Value==1000,"Four-digit pending Time was discarded.");
            Select(5);Require(hall.Input.Text=="bad" && !hall.Apply.IsEnabled,"Invalid pending input was discarded.");
            Call(window,"SetEffectsPreservingPending",(object)confirmed);Select(2);Require(od.Input.Text=="40","Unchanged refresh discarded pending input.");
            Set(window,"currentSnapshot",snapshot);Call(window,"SetEffects",(object)blocks);
            foreach(var c in controls){Select(c.Position);Require(c.Input.Text==blocks[c.Position-1].Values[c.Index].ToString(),"A new snapshot reused stale pending input.");}
            Require(!(bool)Call(window,"HasPendingInputs")!,"New snapshot retained hidden pending state.");
            MobileSliderChecks.Run(window,controls);
            var disabled=blocks.Select(b=>b with {Values=b.Values.Select((v,i)=>i==0?(ushort)0:v).ToArray()}).ToArray();
            Set(window,"currentSnapshot",snapshot with {Blocks=disabled});Call(window,"SetEffects",(object)disabled);
            foreach(var c in controls){Select(c.Position);Update();Require(!c.Slider.IsEnabled,"Disabled effect slider enabled.");}
            EffectFilterChecks.Run();
            ReorderedParameterChecks.Run();
            DynaModelChecks.Run();
            DynaModelChecks.RunFilter();
            EqModelChecks.Run();
            AmpModelChecks.Run();
            EffectChainUiChecks.Run();
            EffectCacheStoreChecks.Run();
            PresetJsonChecks.Run();
            var preview=new MainWindow(true);Require(!Field<Slider>(preview,"thresholdSlider").IsEnabled,"Preview slider enabled.");preview.Close();
            Console.WriteLine("PASS: all 18 sliders; every integer target; numeric/invalid/fractional input; pending/no-write guards; busy/firmware/invalid/disabled/preview states; five viewport sizes; valid/invalid pending navigation, hidden guards, partial Apply/unchanged refresh preservation and new-snapshot reset.");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
        finally {window.Close();app.Shutdown();}
    }
}
