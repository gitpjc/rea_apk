using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;
using Microsoft.Win32;

internal static class PresetJsonChecks
{
    private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    private static readonly Type Codec=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetJsonDocument")!;
    private static object? Invoke(object target,string name,params object?[] args)=>target.GetType().GetMethod(name,Instance)!.Invoke(target,args);
    private static object? Encode(string name,params object?[] args)=>Codec.GetMethod(name,Static)!.Invoke(null,args);
    private static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Instance)!.GetValue(target)!;
    private static void Set(object target,string name,object? value)=>target.GetType().GetField(name,Instance)!.SetValue(target,value);
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static T Named<T>(DependencyObject root,string name) where T:FrameworkElement
    {
        if(root is T match && match.Name==name)return match;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())try{return Named<T>(child,name);}catch(KeyNotFoundException){}
        throw new KeyNotFoundException(name);
    }
    internal static void Run()
    {
        var blocks=new PresetEffectBlock[]{
            new(1,4,new ushort[]{0,2,25,50,0,0}),new(2,1,new ushort[]{1,3,39,100,100}),
            new(3,2,new ushort[]{1,31,100,35,43,40,45,75,0}),new(4,7,new ushort[]{1,1,17,25,500,0,0,0}),
            new(5,8,new ushort[]{1,1,20,24,35,0,0,0,0,0}),new(6,2,new ushort[]{1,0,75,50,50,50,50,55,1})
        };
        var preset=new CurrentPresetSnapshot(4,"British Lead Solo","00","00",blocks);
        var readAt=new DateTimeOffset(2026,10,15,15,0,0,TimeSpan.FromHours(11));
        string Json(CurrentPresetSnapshot p,bool pending=false)=>(string)Encode("Create",p,"V1.1.9",readAt,"amplifierSnapshot",true,pending)!;
        var json=Json(preset);using var doc=JsonDocument.Parse(json);var root=doc.RootElement;var effects=root.GetProperty("effects").EnumerateArray().ToArray();
        Require(root.GetProperty("schemaVersion").GetString()=="2.0.0","Preset JSON schema version missing.");
        Require(effects.Select(e=>e.GetProperty("position").GetInt32()).SequenceEqual(new[]{1,2,3,4,5,6}),"Effect order changed or duplicates were removed.");
        Require(!effects[0].GetProperty("enabled").GetBoolean(),"Disabled effects lost their status.");
        var order=new[]{new[]{"Threshold","Level"},new[]{"Gain","Tone","Vol"},new[]{"Gain","Bass","Mid","Treble","Presence","Master"},new[]{"Level","Feedback","Time"},new[]{"Pre Delay","Level","Decay","Tone"},Array.Empty<string>()};
        for(var i=0;i<effects.Length;i++) {
            Require(effects[i].GetProperty("settings").EnumerateArray().Select(s=>s.GetProperty("name").GetString()).SequenceEqual(order[i]),"JSON settings are not in UI/mobile order.");
            var values=effects[i].GetProperty("settings").EnumerateArray().Select(s=>s.GetProperty("value").GetUInt16());
            Require(values.SequenceEqual(blocks[i].Values.Skip(2).Take(order[i].Length)),"Displayed setting values changed during schema cleanup.");
        }
        void CheckRemovedProperties(JsonElement element) {
            if(element.ValueKind==JsonValueKind.Object)foreach(var property in element.EnumerateObject()) {
                Require(!new[]{"unmappedValues","rawValues","rawValueIndex","typeCode","modelId"}.Contains(property.Name),"An unwanted property remains in JSON.");CheckRemovedProperties(property.Value);
            }
            else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray())CheckRemovedProperties(item);
        }
        CheckRemovedProperties(root);
        Require(effects[5].GetProperty("enabled").ValueKind==JsonValueKind.Null && effects[5].GetProperty("settings").GetArrayLength()==0,"Unknown model settings were guessed.");
        Require(effects[3].GetProperty("settings")[2].GetProperty("unit").GetString()=="ms","Time unit missing.");
        Require(effects[4].GetProperty("settings")[0].GetProperty("name").GetString()=="Pre Delay" && effects[4].GetProperty("settings")[0].GetProperty("unit").GetString()=="ms","Pre Delay repeats or loses its unit.");
        var reordered=blocks.Reverse().Select((b,i)=>b with {Position=(byte)(i+1)}).ToArray();using(var reverse=JsonDocument.Parse(Json(preset with {Blocks=reordered})))Require(reverse.RootElement.GetProperty("effects")[0].GetProperty("name").GetString()==effects[5].GetProperty("name").GetString(),"JSON did not follow reordered chain.");
        for(var slot=1;slot<=80;slot++)using(var parsed=JsonDocument.Parse(Json(preset with {WireIndex=slot,Name="Tone \"quoted\" • 日本語"}))) {
            Require(parsed.RootElement.EnumerateObject().Select(p=>p.Name).SequenceEqual(new[]{"schemaVersion","preset","source","effects"}),"Schema differs between presets.");
            Require(parsed.RootElement.GetProperty("preset").GetProperty("slot").GetString()==PresetAddress.FromWireIndex(slot).Label,"Bank prefix mismatch.");
        }
        Require((string)Encode("DefaultFileName",preset)! == "0D British Lead Solo.json","Default file name must include the preset prefix and name.");
        var invalid=(string)Encode("DefaultFileName",preset with {Name="Bad:/\\*?\"<>|\n name."})!;
        Require(!invalid.Any(c=>System.IO.Path.GetInvalidFileNameChars().Contains(c)) && invalid.StartsWith("0D ") && invalid.EndsWith(".json"),"Default name contains invalid Windows filename characters.");
        var window=new MainWindow();
        try {
            Invoke(window,"UpdateSnapshotArchiveState");Require(!Field<Button>(window,"showPresetJson").IsEnabled,"JSON button enabled without a snapshot.");
            Set(window,"currentSnapshot",preset);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);Invoke(window,"SetEffects",(object)blocks);Invoke(window,"UpdateSnapshotArchiveState");
            Require(Field<Button>(window,"showPresetJson").IsEnabled,"Snapshot JSON button unavailable.");
            using(var busy=new CancellationTokenSource()){Set(window,"operation",busy);Invoke(window,"UpdateSnapshotArchiveState");Require(!Field<Button>(window,"showPresetJson").IsEnabled,"JSON button enabled during mutation.");Set(window,"operation",null);}
            ((Button)Field<WrapPanel>(window,"effectChain").Children[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Field<StackPanel>(window,"effectDetails").Children.OfType<Slider>().Select(s=>AutomationProperties.GetName(s).Split(',')[0]).SequenceEqual(new[]{"Pending Gain","Pending OD808 Tone","Pending Vol"}),"OD808 desktop order does not match JSON/mobile.");
            var dialog=(Window)Invoke(window,"CreatePresetJsonDialog",json,"0D British Lead Solo.json",false)!;
            try {
                var list=Named<ListView>(dialog,"PresetJsonLines");Require(string.Join("\n",list.Items.Cast<string>())==json,"Dialog does not show complete exact JSON.");
                Require(ScrollViewer.GetVerticalScrollBarVisibility(list)==ScrollBarVisibility.Auto && ScrollViewer.GetHorizontalScrollBarVisibility(list)==ScrollBarVisibility.Auto,"JSON is not scrollable in both directions.");
                Require(Named<Button>(dialog,"CopyPresetJson").Content is StackPanel copyContent && copyContent.Children.OfType<Grid>().Any(),"Copy icon missing.");
                Require(Named<Button>(dialog,"SavePresetJson").Content.ToString()=="Save JSON…","Save JSON button missing.");
                var feedback=Named<TextBlock>(dialog,"PresetJsonFeedback");string? copied=null;
                typeof(MainWindow).GetMethod("CopyPresetJson",Static)!.Invoke(null,new object[]{json,(Action<string>)(text=>copied=text),feedback});
                Require(copied==json && feedback.Text.Contains("copied"),"Copy path did not copy the whole JSON.");
                typeof(MainWindow).GetMethod("CopyPresetJson",Static)!.Invoke(null,new object[]{json,(Action<string>)(_=>throw new COMException("busy")),feedback});
                Require(feedback.Text.Contains("busy"),"Clipboard busy handling failed.");
                var picker=(SaveFileDialog)typeof(MainWindow).GetMethod("CreatePresetJsonSavePicker",Static)!.Invoke(null,new object[]{"0D British Lead Solo.json"})!;
                Require(picker.FileName=="0D British Lead Solo.json" && picker.AddExtension && picker.OverwritePrompt && picker.DefaultExt.TrimStart('.')=="json","Native Save As defaults differ.");
                var view=(FrameworkElement)dialog.Content;foreach(var size in new[]{new Size(760,580),new Size(400,280)}){view.Measure(size);view.Arrange(new Rect(size));view.UpdateLayout();Require(list.ActualHeight>70 && list.ActualWidth>300,"JSON viewer collapses at small dialog sizes.");}
            }finally {dialog.Close();}
            Require(Field<F15ReadSession?>(window,"connectedSession") is null && Field<CurrentPresetSnapshot>(window,"currentSnapshot")==preset,"Viewing JSON changed the snapshot or opened a hardware session.");
        }finally {Set(window,"operation",null);window.Close();}
        var directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"IampPresetJsonChecks-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(directory);
        var file=System.IO.Path.Combine(directory,"0D British Lead Solo.json");
        try {
            ((Task)Encode("SaveAsync",file,json)!).GetAwaiter().GetResult();Require(System.IO.File.ReadAllText(file)==json,"Saved JSON differs from copied/viewed JSON.");
            Require(!System.IO.File.ReadAllBytes(file).Take(3).SequenceEqual(new byte[]{0xEF,0xBB,0xBF}),"JSON unexpectedly contains a UTF-8 BOM.");
            ((Task)Encode("SaveAsync",file,json+"\n")!).GetAwaiter().GetResult();Require(System.IO.File.ReadAllText(file)==json+"\n" && System.IO.Directory.GetFiles(directory).Length==1,"Overwrite or temporary-file cleanup failed.");
        }finally {if(System.IO.File.Exists(file))System.IO.File.Delete(file);System.IO.Directory.Delete(directory);}
        Console.WriteLine("PASS: simplified ordered preset JSON across 80 slots, removed properties/clean names/separate units, disabled/duplicate/unknown models, scrolling/copy/Save As and UTF-8 files without device work.");
    }
}
