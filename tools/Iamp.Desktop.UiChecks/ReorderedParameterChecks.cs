using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class ReorderedParameterChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags)!.GetValue(target)!;
    private static T Property<T>(object target,string name)=>(T)target.GetType().GetProperty(name,Flags)!.GetValue(target)!;
    private static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags)!.SetValue(target,value);
    private static object? Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Flags)!.Invoke(target,args);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}

    internal static void Run()
    {
        var window=new MainWindow();
        try {
            var original=new PresetEffectBlock[]{
                new(1,4,new ushort[]{1,2,25,50,0,0}),new(2,1,new ushort[]{1,3,39,100,100}),
                new(3,2,new ushort[]{1,31,100,35,43,40,45,75,1}),new(4,7,new ushort[]{1,1,17,25,500,0,0,0}),
                new(5,8,new ushort[]{1,1,20,24,35,0,0,0,0,0})
            };
            var reversed=original.Reverse().Select((block,index)=>block with {Position=(byte)(index+1)}).ToArray();
            var snapshot=new CurrentPresetSnapshot(4,"Reordered offline editors","00","00",reversed);
            Set(window,"currentSnapshot",snapshot);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);
            Call(window,"SetEffects",(object)reversed);
            void Select(byte position)=>((Button)Field<WrapPanel>(window,"effectChain").Children[position-1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var inputs=new List<(byte Position,int Index,TextBox Input,Slider Slider,Button Apply)> {
                (5,2,Field<TextBox>(window,"thresholdInput"),Field<Slider>(window,"thresholdSlider"),Field<Button>(window,"applyThreshold")),
                (4,3,Field<TextBox>(window,"toneInput"),Field<Slider>(window,"toneSlider"),Field<Button>(window,"applyTone"))
            };
            foreach(var editor in (System.Collections.IEnumerable)window.GetType().GetProperty("AdditionalEditors",Flags)!.GetValue(window)!)
                inputs.Add((Property<byte>(editor,"Position"),Property<int>(editor,"ParameterIndex"),Property<TextBox>(editor,"Input"),Property<Slider>(editor,"Slider"),Property<Button>(editor,"Apply")));
            foreach(var input in inputs) {
                Select(input.Position);
                var value=reversed[input.Position-1].Values[input.Index];
                Require(input.Input.Text==value.ToString() && input.Slider.IsEnabled && !input.Apply.IsEnabled,"Moved effect has stale or disabled parameter controls.");
                input.Input.Text=(value==input.Slider.Minimum?value+1:value-1).ToString();
                Require(input.Apply.IsEnabled && (bool)Call(window,"HasPendingInputs")!,"Moved effect lost its pending edit guard.");
                Select((byte)(input.Position==1?2:1));Select(input.Position);
                Require(input.Apply.IsEnabled,"Browsing moved effects discarded pending inputs.");
                input.Input.Text=value.ToString();
            }
            Require(!(bool)Call(window,"HasPendingInputs")!,"Restored inputs remain pending after reorder.");
            Select(5);Field<TextBox>(window,"thresholdInput").Text="26";
            Set(window,"currentSnapshot",snapshot with {Blocks=original});Call(window,"SetEffects",(object)original);
            Select(1);Require(Field<TextBox>(window,"thresholdInput").Text=="25" && !(bool)Call(window,"HasPendingInputs")!,"A new chain reused a stale pending value at the previous position.");
            Console.WriteLine("PASS: all 18 known parameter editors follow reversed effect positions; hidden pending guards and new-chain reset remain correct.");
        }
        finally {window.Close();}
    }
}
