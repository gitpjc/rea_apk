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
        Require(root.GetProperty("schemaVersion").GetString()=="4.0.0","Preset JSON schema version missing.");
        Require(effects.Select(e=>e.GetProperty("position").GetInt32()).SequenceEqual(new[]{1,2,3,4,5,6}),"Effect order changed or duplicates were removed.");
        Require(!effects[0].GetProperty("enabled").GetBoolean(),"Disabled effects lost their status.");
        var order=new[]{new[]{"Threshold","Level"},new[]{"Gain","Tone","Vol"},new[]{"Gain","Bass","Mid","Treble","Presence","Master"},new[]{"Level","Feedback","Time"},new[]{"Pre Delay","Level","Decay","Tone"},new[]{"Gain","Bass","Mid","Treble","Presence","Master"}};
        for(var i=0;i<effects.Length;i++) {
            Require(effects[i].GetProperty("settings").EnumerateArray().Select(s=>s.GetProperty("name").GetString()).SequenceEqual(order[i]),"JSON settings are not in UI/mobile order.");
            var values=effects[i].GetProperty("settings").EnumerateArray().Select(s=>s.GetProperty("value").GetDecimal());
            Require(values.SequenceEqual(blocks[i].Values.Skip(2).Take(order[i].Length).Select(v=>(decimal)v)),"Displayed setting values changed during schema cleanup.");
            Require(effects[i].GetProperty("settings").EnumerateArray().All(s=>s.GetProperty("group").ValueKind==JsonValueKind.Null),"Ungrouped settings must explicitly export a null group.");
        }
        void CheckRemovedProperties(JsonElement element) {
            if(element.ValueKind==JsonValueKind.Object)foreach(var property in element.EnumerateObject()) {
                Require(!new[]{"unmappedValues","rawValues","rawValueIndex","typeCode","modelId"}.Contains(property.Name),"An unwanted property remains in JSON.");CheckRemovedProperties(property.Value);
            }
            else if(element.ValueKind==JsonValueKind.Array)foreach(var item in element.EnumerateArray())CheckRemovedProperties(item);
        }
        CheckRemovedProperties(root);
        var eqBlocks=new PresetEffectBlock[]{
            new(1,9,new ushort[]{1,0,32,33,0,64,31,50,0,0,0,0,0,0,0,0,0}),
            new(2,9,new ushort[]{1,1,32,100,8,32,500,8,32,1600,8,32,6000,8,0,0,50})
        };
        using(var eqDoc=JsonDocument.Parse(Json(preset with {Blocks=eqBlocks}))) {
            var eqEffects=eqDoc.RootElement.GetProperty("effects");
            Require(eqEffects[0].GetProperty("name").GetString()=="5 Bands EQ" && eqEffects[1].GetProperty("name").GetString()=="Custom EQ","EQ presentation titles differ from the phone.");
            var fixedSettings=eqEffects[0].GetProperty("settings").EnumerateArray().ToArray();
            Require(fixedSettings.Select(s=>s.GetProperty("name").GetString()).SequenceEqual(new[]{"100Hz","250Hz","630Hz","1.6kHz","4kHz","Level"}),"Fixed EQ settings order differs from the phone.");
            Require(fixedSettings.Select(s=>s.GetProperty("value").GetDecimal()).SequenceEqual(new decimal[]{0,0.5m,-16,16,-0.5m,50}),"Fixed EQ must export signed physical gains and unitless Level.");
            Require(fixedSettings.Select(s=>s.GetProperty("unit").GetString()).SequenceEqual(new string?[]{"dB","dB","dB","dB","dB",null}) && fixedSettings.All(s=>s.GetProperty("group").ValueKind==JsonValueKind.Null),"Fixed EQ units or null groups differ.");
            var customSettings=eqEffects[1].GetProperty("settings").EnumerateArray().ToArray();
            Require(customSettings.Select(s=>s.GetProperty("name").GetString()).SequenceEqual(new[]{"Band 1 Gain","Band 1 Freq","Band 1 Q","Band 2 Gain","Band 2 Freq","Band 2 Q","Band 3 Gain","Band 3 Freq","Band 3 Q","Band 4 Gain","Band 4 Freq","Band 4 Q","Low Cut","High Cut","Level"}),"Custom EQ settings order differs from the phone.");
            Require(customSettings.Select(s=>s.GetProperty("group").GetString()).SequenceEqual(new string?[]{"Low","Low","Low","Low Mid","Low Mid","Low Mid","High Mid","High Mid","High Mid","High","High","High",null,null,null}),"Custom EQ groups must be flat, ordered band metadata.");
            Require(customSettings.Take(12).Select(s=>s.GetProperty("value").GetDecimal()).SequenceEqual(new decimal[]{0,100,1,0,500,1,0,1600,1,0,6000,1}),"Custom EQ gains, Hz or Q conversion differs.");
            Require(customSettings.Take(12).Select(s=>s.GetProperty("unit").GetString()).SequenceEqual(Enumerable.Range(0,4).SelectMany(_=>new string?[]{"dB","Hz",null})),"Custom EQ band units differ.");
            Require(customSettings.Skip(12).Take(2).All(s=>s.GetProperty("value").GetString()=="Off" && s.GetProperty("unit").ValueKind==JsonValueKind.Null),"Off EQ cuts must export a string and no unit.");
            Require(customSettings[14].GetProperty("value").GetDecimal()==50 && customSettings[14].GetProperty("unit").ValueKind==JsonValueKind.Null,"Custom EQ Level must remain unitless.");
            CheckRemovedProperties(eqDoc.RootElement);
        }
        var activeCuts=eqBlocks[1].Values.ToArray();activeCuts[14]=80;activeCuts[15]=12000;
        using(var cutsDoc=JsonDocument.Parse(Json(preset with {Blocks=new[]{eqBlocks[1] with {Values=activeCuts}}}))) {
            var cuts=cutsDoc.RootElement.GetProperty("effects")[0].GetProperty("settings");
            Require(cuts[12].GetProperty("value").GetDecimal()==80 && cuts[13].GetProperty("value").GetDecimal()==12000 && cuts[12].GetProperty("unit").GetString()=="Hz" && cuts[13].GetProperty("unit").GetString()=="Hz","Active EQ cuts must export numeric frequencies in Hz.");
        }
        var unknownEq=new PresetEffectBlock[]{new(1,9,new ushort[]{1,2,100,100,100,100,100,100,100,100,100,100,100,100,100,0,0}),new(2,9,new ushort[]{1,0,100})};
        using(var unknownEqDoc=JsonDocument.Parse(Json(preset with {Blocks=unknownEq}))) {
            Require(unknownEqDoc.RootElement.GetProperty("effects").EnumerateArray().All(e=>e.GetProperty("settings").GetArrayLength()==0),"Unsupported EQ models or block shapes must not expose guessed settings.");
            CheckRemovedProperties(unknownEqDoc.RootElement);
        }
        Require(effects[5].GetProperty("enabled").GetBoolean() && effects[5].GetProperty("settings").GetArrayLength()==6,"Verified Amp model settings missing.");
        using(var unknownOd=JsonDocument.Parse(Json(preset with {Blocks=new[]{new PresetEffectBlock(1,1,new ushort[]{1,ushort.MaxValue,39,100,100})}})))
            Require(unknownOd.RootElement.GetProperty("effects")[0].GetProperty("enabled").ValueKind==JsonValueKind.Null && unknownOd.RootElement.GetProperty("effects")[0].GetProperty("settings").GetArrayLength()==0,"Unknown OD models must not expose guessed settings or enabled state.");
        using(var unknownAmp=JsonDocument.Parse(Json(preset with {Blocks=new[]{new PresetEffectBlock(1,2,new ushort[]{1,ushort.MaxValue,75,50,50,50,50,55,1})}})))
            Require(unknownAmp.RootElement.GetProperty("effects")[0].GetProperty("enabled").ValueKind==JsonValueKind.Null && unknownAmp.RootElement.GetProperty("effects")[0].GetProperty("settings").GetArrayLength()==0,"Unknown Amp model settings were guessed.");
        Require(effects[3].GetProperty("settings")[2].GetProperty("unit").GetString()=="ms","Time unit missing.");
        Require(effects[4].GetProperty("settings")[0].GetProperty("name").GetString()=="Pre Delay" && effects[4].GetProperty("settings")[0].GetProperty("unit").GetString()=="ms","Pre Delay repeats or loses its unit.");
        var reordered=blocks.Reverse().Select((b,i)=>b with {Position=(byte)(i+1)}).ToArray();using(var reverse=JsonDocument.Parse(Json(preset with {Blocks=reordered})))Require(reverse.RootElement.GetProperty("effects")[0].GetProperty("name").GetString()==effects[5].GetProperty("name").GetString(),"JSON did not follow reordered chain.");
        for(var slot=1;slot<=80;slot++)using(var parsed=JsonDocument.Parse(Json(preset with {WireIndex=slot,Name="Tone \"quoted\" • 日本語"}))) {
            Require(parsed.RootElement.EnumerateObject().Select(p=>p.Name).SequenceEqual(new[]{"schemaVersion","preset","source","effects","restore"}),"Schema differs between presets.");
            Require(parsed.RootElement.GetProperty("preset").GetProperty("slot").GetString()==PresetAddress.FromWireIndex(slot).Label,"Bank prefix mismatch.");
        }
        Require(System.Text.RegularExpressions.Regex.IsMatch((string)Encode("DefaultFileName",preset)!,@"^Preset-0D-British-Lead-Solo_\d{4}-\d{2}-\d{2}_\d{4}\.json$"),"Default file name must include Preset prefix, hyphenated slot/name and local timestamp.");
        var invalid=(string)Encode("DefaultFileName",preset with {Name="Bad:/\\*?\"<>|\n name."})!;
        Require(!invalid.Any(c=>System.IO.Path.GetInvalidFileNameChars().Contains(c)) && !invalid.Contains(' ') && invalid.StartsWith("Preset-0D-") && invalid.EndsWith(".json"),"Default name contains spaces or invalid Windows filename characters.");
        var window=new MainWindow();
        try {
            Invoke(window,"UpdateSnapshotArchiveState");Require(!Field<Button>(window,"showPresetJson").IsEnabled,"JSON button enabled without a snapshot.");
            Set(window,"currentSnapshot",preset);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);Invoke(window,"SetEffects",(object)blocks);Invoke(window,"UpdateSnapshotArchiveState");
            Require(Field<Button>(window,"showPresetJson").IsEnabled,"Snapshot JSON button unavailable.");
            using(var busy=new CancellationTokenSource()){Set(window,"operation",busy);Invoke(window,"UpdateSnapshotArchiveState");Require(!Field<Button>(window,"showPresetJson").IsEnabled,"JSON button enabled during mutation.");Set(window,"operation",null);}
            ((Button)Field<WrapPanel>(window,"effectChain").Children[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Field<StackPanel>(window,"effectDetails").Children.OfType<Slider>().Select(s=>AutomationProperties.GetName(s).Split(',')[0]).SequenceEqual(new[]{"Pending Gain","Pending OD Tone","Pending Vol"}),"OD desktop order does not match JSON/mobile.");
            var dialog=(Window)Invoke(window,"CreatePresetJsonDialog",json,"0D British Lead Solo.json",false)!;
            try {
                var list=Named<ListView>(dialog,"PresetJsonLines");Require(string.Join("\n",list.Items.Cast<string>())==json,"Dialog does not show complete exact JSON.");
                Require(ScrollViewer.GetVerticalScrollBarVisibility(list)==ScrollBarVisibility.Auto && ScrollViewer.GetHorizontalScrollBarVisibility(list)==ScrollBarVisibility.Auto,"JSON is not scrollable in both directions.");
                Require(Named<Button>(dialog,"CopyPresetJson").Content is StackPanel copyContent && copyContent.Children.OfType<Grid>().Any(),"Copy icon missing.");
                try {Named<Button>(dialog,"SavePresetJson");throw new InvalidOperationException("Save JSON remains in the viewer.");}catch(KeyNotFoundException){}
                var feedback=Named<TextBlock>(dialog,"PresetJsonFeedback");string? copied=null;
                typeof(MainWindow).GetMethod("CopyPresetJson",Static)!.Invoke(null,new object[]{json,(Action<string>)(text=>copied=text),feedback});
                Require(copied==json && feedback.Text.Contains("copied"),"Copy path did not copy the whole JSON.");
                typeof(MainWindow).GetMethod("CopyPresetJson",Static)!.Invoke(null,new object[]{json,(Action<string>)(_=>throw new COMException("busy")),feedback});
                Require(feedback.Text.Contains("busy"),"Clipboard busy handling failed.");
                var picker=(SaveFileDialog)typeof(MainWindow).GetMethod("CreatePresetExportPicker",Static)!.Invoke(null,new object[]{"0D British Lead Solo.json"})!;
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
        Console.WriteLine("PASS: ordered preset JSON schema 4 across 80 slots, EQ physical values/groups/Off cuts, disabled/duplicate/unknown models, scrolling/copy, export picker and UTF-8 files without device work.");
    }
}
