using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class DynaModelChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    private static T Property<T>(object o,string n)=>(T)o.GetType().GetProperty(n,Flags)!.GetValue(o)!;
    private static void Set(object o,string n,object? v)=>o.GetType().GetField(n,Flags)!.SetValue(o,v);
    private static object? Call(object o,string n,params object?[] args)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,args);
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    internal static void Run()
        =>RunFamily("Dyna","dynaEditors",4,[
            (6,new ushort[]{1,0,20,70,0,0},new[]{"Sensitivity","Level"}),
            (7,new ushort[]{1,1,30,15,45,75},new[]{"Attack","Ratio","Threshold","Level"}),
            (8,new ushort[]{1,3,10,15,50,0},new[]{"Threshold","Depth","Level"})]);
    internal static void RunFilter()
        =>RunFamily("Filter","filterEditors",5,[
            (9,new ushort[]{1,0,60,45,55,70},new[]{"Attack","Peak","Sensitivity","Level"}),
            (10,new ushort[]{1,1,20,45,50,70},new[]{"Rate","Peak","Range","Level"}),
            (11,new ushort[]{1,2,12,65,60,0},new[]{"Rate","Q","Mix"})]);
    private static void RunFamily(string family,string editorField,byte typeCode,(byte Profile,ushort[] Values,string[] Labels)[] cases)
    {
        var window=new MainWindow();
        try {
            var editors=Field<Array>(window,editorField).Cast<object>().ToArray();
            var jsonType=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetJsonDocument")!;
            foreach(var item in cases) {
                var blocks=Enumerable.Range(1,20).Select(i=>new PresetEffectBlock((byte)i,0,Array.Empty<ushort>())).ToArray();
                blocks[0]=new(1,typeCode,item.Values);blocks[1]=new(2,typeCode,item.Values.ToArray());
                var snapshot=new CurrentPresetSnapshot(4,$"Offline {family}","00","00",blocks);
                void Load(CurrentPresetSnapshot value) {
                    Set(window,"currentSnapshot",value);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);Set(window,"operation",null);
                    Call(window,"SetEffects",(object)value.Blocks);
                }
                void Select(byte position)=>((Button)Field<WrapPanel>(window,"effectChain").Children[position-1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Load(snapshot);
                var active=editors.Where(e=>Property<byte>(e,"ProfilePosition")==item.Profile).OrderBy(e=>Property<int>(e,"ParameterIndex")).ToArray();
                Require(active.Select(e=>Property<string>(e,"Label")).SequenceEqual(item.Labels),"Dyna label/order differs from mobile.");
                Require(Field<StackPanel>(window,"effectDetails").Children.OfType<Slider>().SequenceEqual(active.Select(e=>Property<Slider>(e,"Slider"))),"Dyna sliders do not follow mobile order.");
                Require(Field<Button>(window,"effectPowerButton").IsEnabled,"Verified Dyna on/off unavailable.");
                foreach(var editor in active) {
                    var slider=Property<Slider>(editor,"Slider");var input=Property<TextBox>(editor,"Input");var apply=Property<Button>(editor,"Apply");var index=Property<int>(editor,"ParameterIndex");
                    Require(slider.IsEnabled && slider.Minimum==0 && slider.Maximum==100 && !apply.IsEnabled,"Dyna initial range/state incorrect.");
                    Require(((SolidColorBrush)slider.Foreground).Color.ToString()==(typeCode==5?"#FFF58CEB":"#FFFFA45C"),"Model slider fill does not match its effect category colour.");
                    for(var value=0;value<=100;value++) {slider.Value=value;Require(input.Text==value.ToString() && apply.IsEnabled==(value!=item.Values[index]),"Dyna value/Apply synchronization failed.");}
                    input.Text="bad";Require(!apply.IsEnabled,"Invalid Dyna input can apply.");
                    input.Text=item.Values[index].ToString();
                }
                var firstInput=Property<TextBox>(active[0],"Input");
                firstInput.Text="21";Select(2);Require(firstInput.Text==item.Values[2].ToString(),"Second identical Dyna reused the first pending value.");
                firstInput.Text="22";Select(1);Require(firstInput.Text=="21","First Dyna pending value lost after browsing duplicate.");
                Call(window,"SetEffectsPreservingPending",(object)blocks);Require(firstInput.Text=="21","Unchanged refresh lost Dyna pending value.");
                Select(2);Require(firstInput.Text=="22","Refresh shared duplicate pending inputs.");
                Require(!Field<Button>(window,"changeEffectModel").IsEnabled && !Field<Button>(window,"effectPowerButton").IsEnabled,"Hidden pending Dyna edits permit chain/power changes.");
                Load(snapshot);Require(!(bool)Call(window,"HasPendingInputs")!,"Fresh Dyna snapshot retained pending values.");
                var json=(string)jsonType.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public)!.Invoke(null,new object?[]{snapshot,"V1.1.9",DateTimeOffset.Now,"amplifierSnapshot",true,false})!;
                using(var document=JsonDocument.Parse(json))Require(document.RootElement.GetProperty("effects")[0].GetProperty("settings").EnumerateArray().Select(p=>p.GetProperty("name").GetString()).SequenceEqual(item.Labels),"Dyna JSON settings differ from mobile order.");
                var disabled=item.Values.ToArray();disabled[0]=0;var disabledBlocks=blocks.ToArray();disabledBlocks[0]=blocks[0] with {Values=disabled};Load(snapshot with {Blocks=disabledBlocks});
                Require(active.All(e=>!Property<Slider>(e,"Slider").IsEnabled) && Field<Button>(window,"effectPowerButton").IsEnabled && (string)Field<Button>(window,"effectPowerButton").Content=="Enable effect","Disabled Dyna is not grey/re-enableable with parameters blocked.");
                var content=(StackPanel)((Button)Field<WrapPanel>(window,"effectChain").Children[0]).Content;
                Require(content.Children.OfType<TextBlock>().All(t=>t.Text!="State unknown") && ((SolidColorBrush)content.Children.OfType<TextBlock>().First().Foreground).Color.ToString()=="#FFA3ADBD","Disabled Dyna category is not confirmed grey.");
                Set(window,"snapshotFirmware","unsupported");Call(window,"UpdateThresholdState");Require(!Field<Button>(window,"effectPowerButton").IsEnabled,"Unsupported firmware allows Dyna power writes.");
                Require(Field<F15ReadSession?>(window,"connectedSession") is null && Field<CancellationTokenSource?>(window,"operation") is null,"Offline Dyna checks performed device work.");
            }
            Console.WriteLine($"PASS: all {cases.Sum(c=>c.Labels.Length)} {family} sliders/ranges/order; invalid and duplicate-instance pending inputs; grey disabled/re-enable states; firmware/no-write guards and mobile-order JSON.");
        }
        finally {window.Close();}
    }
}
