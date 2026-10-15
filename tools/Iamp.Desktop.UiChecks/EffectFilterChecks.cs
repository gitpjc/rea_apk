using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class EffectFilterChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    static void Set(object o,string n,object? v)=>o.GetType().GetField(n,Flags)!.SetValue(o,v);
    static object? Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,a);
    static void Require(bool c,string message){if(!c)throw new Exception(message);}
    internal static void Run()
    {
        var w=new MainWindow();
        try {
            var filter=Field<ComboBox>(w,"effectFilter");var presets=Field<ComboBox>(w,"presets");var search=Field<TextBox>(w,"search");
            var reset=Field<Button>(w,"resetEffectFilter");var refresh=Field<Button>(w,"refreshEffectNames");
            Require(filter.Items.Count==125 && filter.SelectedIndex==0 && !filter.IsEnabled,"Initial effect filter differs.");
            Require(!reset.IsEnabled && !refresh.IsEnabled,"Default Reset or disconnected refresh enabled.");
            var records=new[]{
                new DecodedPreset(1,"Clean",new[]{new PresetEffectBlock(1,4,new ushort[]{0,2,25,50,0,0})},Array.Empty<byte>()),
                new DecodedPreset(2,"Warm",new[]{new PresetEffectBlock(1,4,new ushort[]{1,0,25,50})},Array.Empty<byte>()),
                new DecodedPreset(4,"Lead",new[]{new PresetEffectBlock(2,4,new ushort[]{1,2,25,50,0,0})},Array.Empty<byte>()),
                new DecodedPreset(80,"Last",new[]{new PresetEffectBlock(1,5,new ushort[]{0,0,60,45,55,70})},Array.Empty<byte>())
            };
            Set(w,"names",records.Select(p=>new PresetName(p.WireIndex,p.Name)).ToArray());
            var snapshot=new CurrentPresetSnapshot(4,"Lead","00","00",records[2].Blocks);Set(w,"currentSnapshot",snapshot);
            Call(w,"SetPresetEffectIndex",(object)records);Call(w,"Filter");
            Require(filter.IsEnabled && presets.Items.Count==4,"Loaded filter not available.");
            object Choice(byte type,ushort model)=>filter.Items.Cast<object>().Single(o=>o.GetType().GetProperty("TypeCode")!.GetValue(o) is byte t && t==type && o.GetType().GetProperty("Model")!.GetValue(o) is ushort m && m==model);
            filter.SelectedItem=Choice(4,2);
            Require(reset.IsEnabled,"Selected effect did not enable Reset.");
            Require(presets.Items.Cast<PresetName>().Select(p=>p.WireIndex).SequenceEqual(new[]{1,4}),"Disabled effect excluded or wrong model included.");
            Require(((PresetName)presets.SelectedItem).WireIndex==4 && ((PresetName)presets.SelectedItem).IsCurrent,"Active preset marker lost.");
            search.Text="clean";Require(presets.Items.Count==1&&((PresetName)presets.Items[0]).WireIndex==1,"Name and effect filters did not intersect.");
            search.Text="0d";Require(presets.Items.Count==1&&((PresetName)presets.Items[0]).WireIndex==4,"Bank and effect filters did not intersect.");
            search.Text="not present";Require(presets.Items.Count==0 && Field<TextBlock>(w,"count").Text=="0 of 4 presets","Empty result/count differs.");
            Field<Button>(w,"clearSearch").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(presets.Items.Count==2,"Search clear lost the effect filter.");
            filter.SelectedItem=Choice(5,0);Require(presets.Items.Count==1&&((PresetName)presets.Items[0]).WireIndex==80,"Filter category model did not match.");
            reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(presets.Items.Count==4 && filter.SelectedIndex==0 && !reset.IsEnabled,"Reset did not select All effects and disable itself.");
            filter.SelectedItem=Choice(4,2);search.Text="clean";reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(search.Text=="clean" && presets.Items.Count==1,"Reset cleared the independent text search.");search.Clear();
            Require(ReferenceEquals(snapshot,Field<CurrentPresetSnapshot>(w,"currentSnapshot")) && Field<F15ReadSession?>(w,"connectedSession") is null && Field<CancellationTokenSource?>(w,"operation") is null,"Filtering started device work or changed confirmed state.");
            var root=(FrameworkElement)w.Content;var scroll=Field<ScrollViewer>(w,"libraryScroll");
            foreach(var size in new[]{new Size(1366,768),new Size(911,512),new Size(683,384),new Size(620,320),new Size(1920,1080)}) {
                for(var pass=0;pass<4;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                foreach(var c in new FrameworkElement[]{filter,search,presets,reset,refresh}) {
                    var point=c.TransformToAncestor(scroll).Transform(new Point());
                    Require(c.ActualWidth>0 && point.X>=0 && point.X+c.ActualWidth<=scroll.ViewportWidth+1,"Preset filter exceeds viewport.");
                }
                var a=search.TransformToAncestor(root).Transform(new Point());var b=filter.TransformToAncestor(root).Transform(new Point());
                Require(b.X>=a.X+search.ActualWidth || b.Y>=a.Y+search.ActualHeight,"Search and effect inputs overlap.");
            }
            Call(w,"Begin","Offline filter busy check");Require(!filter.IsEnabled,"Busy filter remained enabled.");Call(w,"End");Require(filter.IsEnabled,"Filter did not re-enable.");
            filter.SelectedItem=Choice(4,2);
            Call(w,"SetPresetEffectIndex",(object)Array.Empty<DecodedPreset>());
            Require(!filter.IsEnabled && filter.SelectedIndex==0 && presets.Items.Count==4,"Empty index stranded a selected effect filter.");
            Console.WriteLine("PASS: effect dropdown catalog, disabled-effect inclusion, combined name/bank search, All effects/search reset, active marker, empty results, no device work, busy/empty index and five-size header layout.");
        }
        finally {w.Close();}
    }
}
