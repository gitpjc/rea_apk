using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class EqModelChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    private static T Property<T>(object o,string n)=>(T)o.GetType().GetProperty(n,Flags)!.GetValue(o)!;
    private static void Set(object o,string n,object? v)=>o.GetType().GetField(n,Flags)!.SetValue(o,v);
    private static object? Call(object o,string n,params object?[] args)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,args);
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static IEnumerable<Slider> Sliders(Panel panel)
    {
        foreach(var child in panel.Children) {
            if(child is Slider s)yield return s;
            if(child is Expander {Content:Panel content})foreach(var nested in Sliders(content))yield return nested;
        }
    }
    internal static void Run()
    {
        var window=new MainWindow();
        try {
            foreach(var model in new[]{0,1}) {
                ushort[] values=model==0?[1,0,32,32,32,32,32,50,0,0,0,0,0,0,0,0,0]:[1,1,32,100,8,32,500,8,32,1600,8,32,6000,8,0,0,50];
                var blocks=Enumerable.Range(1,20).Select(i=>new PresetEffectBlock((byte)i,0,Array.Empty<ushort>())).ToArray();
                blocks[0]=new(1,9,values);blocks[1]=new(2,9,values.ToArray());
                var snapshot=new CurrentPresetSnapshot(4,"Offline EQ","00","00",blocks);
                void Load(CurrentPresetSnapshot s) {
                    Set(window,"currentSnapshot",s);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);Set(window,"operation",null);
                    Call(window,"SetEffects",(object)s.Blocks);
                }
                void Select(byte p)=>((Button)Field<WrapPanel>(window,"effectChain").Children[p-1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Load(snapshot);var specs=EqParameters.For(blocks[0]);
                var editors=Field<Array>(window,"eqEditors").Cast<object>().Where(e=>Property<byte>(e,"ProfilePosition")==12+model).OrderBy(e=>Property<int>(e,"ParameterIndex")).ToArray();
                var details=Field<StackPanel>(window,"effectDetails");
                Require(Sliders(details).SequenceEqual(editors.Select(e=>Property<Slider>(e,"Slider"))),"EQ controls not in mobile order.");
                Require(details.Children.OfType<Expander>().Select(e=>(string)e.Header).SequenceEqual(model==0?Array.Empty<string>():new[]{"Low","Low Mid","High Mid","High"}),"Custom EQ sections differ.");
                foreach(var group in details.Children.OfType<Expander>()) {group.IsExpanded=false;group.IsExpanded=true;}
                var root=(FrameworkElement)window.Content;var scroll=Field<ScrollViewer>(window,"libraryScroll");
                foreach(var size in new[]{new Size(1366,768),new Size(911,512),new Size(683,384),new Size(620,320),new Size(1920,1080)}) {
                    for(var pass=0;pass<3;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                    foreach(var slider in Sliders(details)) {
                        var point=slider.TransformToAncestor(scroll).Transform(new Point());
                        Require(slider.ActualWidth>0 && point.X>=0 && point.X+slider.ActualWidth<=scroll.ViewportWidth+1,"Grouped EQ slider exceeds viewport.");
                    }
                }
                foreach(var editor in editors) {
                    var spec=specs.Single(s=>s.ValueIndex==Property<int>(editor,"ParameterIndex"));
                    var slider=Property<Slider>(editor,"Slider");var input=Property<TextBox>(editor,"Input");var apply=Property<Button>(editor,"Apply");
                    Require(slider.IsEnabled && input.Text==spec.Format(values[spec.ValueIndex]) && !apply.IsEnabled,"EQ initial physical display incorrect.");
                    Require(((SolidColorBrush)slider.Foreground).Color.ToString()=="#FF20D5D2","EQ fill colour differs.");
                    for(var index=0;index<spec.WireValues.Count;index++) {
                        slider.Value=index;var raw=spec.WireValues[index];
                        Require(input.Text==spec.Format(raw) && apply.IsEnabled==(raw!=values[spec.ValueIndex]),"EQ slider physical value/Apply mismatch.");
                        Require((bool)Call(window,"HasPendingInputs")! == (raw!=values[spec.ValueIndex]),"EQ pending parser differs.");
                        input.Text=spec.Format(raw);Require(slider.Value==index,"EQ input to slider conversion differs.");
                    }
                    var previous=slider.Value;input.Text="bad";Require(!apply.IsEnabled && slider.Value==previous,"Invalid EQ input accepted.");
                    input.Text=spec.Format(values[spec.ValueIndex]);
                }
                var first=Property<TextBox>(editors[0],"Input");first.Text="-0.5";Select(2);Require(first.Text=="0.0","Duplicate EQ shares pending input.");
                first.Text="0.5";Select(1);Require(first.Text=="-0.5","EQ signed pending value lost.");
                Call(window,"SetEffectsPreservingPending",(object)blocks);Require(first.Text=="-0.5","Refresh lost EQ pending value.");
                Select(2);Require(first.Text=="0.5","Refresh merged duplicate EQ instances.");
                Load(snapshot);Require(!(bool)Call(window,"HasPendingInputs")!,"EQ confirmed Off/decimals appear pending.");
                Call(window,"Begin","Offline EQ busy check");Require(editors.All(e=>!Property<Slider>(e,"Slider").IsEnabled),"Busy EQ sliders enabled.");Call(window,"End");
                var disabled=values.ToArray();disabled[0]=0;var off=blocks.ToArray();off[0]=blocks[0] with {Values=disabled};Load(snapshot with {Blocks=off});
                Require(editors.All(e=>!Property<Slider>(e,"Slider").IsEnabled) && Field<Button>(window,"effectPowerButton").IsEnabled,"Disabled EQ cannot be reenabled or controls remain active.");
                Set(window,"snapshotFirmware","unsupported");Call(window,"UpdateThresholdState");Require(!Field<Button>(window,"effectPowerButton").IsEnabled,"Unsupported EQ firmware permits writes.");
                Require(Field<F15ReadSession?>(window,"connectedSession") is null && Field<CancellationTokenSource?>(window,"operation") is null,"Offline EQ check performed hardware work.");
            }
            Console.WriteLine("PASS: all 21 EQ controls, every discrete physical step, four collapsible groups, signed/decimal/Off pending conversion, duplicate refresh isolation, invalid/busy/disabled/firmware/no-write guards.");
        }
        finally {window.Close();}
    }
}
