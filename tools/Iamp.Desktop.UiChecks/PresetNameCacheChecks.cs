using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Transport.Windows;

internal static class PresetNameCacheChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    internal static void Run()
    {
        var directory=Path.Combine(Path.GetTempPath(),"iamp-preset-name-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"names.json");
        var type=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetNameCacheStore")!;
        object Store(string file)=>Activator.CreateInstance(type,Flags,null,[file],null)!;
        object? Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags)!.Invoke(target,args);
        T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags)!.GetValue(target)!;
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        var names=Enumerable.Range(1,80).Select(i=>new PresetName(i,$"Original patch {i}"){IsCurrent=i==4}).ToArray();
        MainWindow? window=null;
        try
        {
            Require(Invoke(Store(path),"Load") is null,"Missing cache not empty");
            Require((bool)Invoke(Store(path),"Save",(object)names)!,"Cache write failed");
            var cached=Invoke(Store(path),"Load")!;Require(cached is not null,"Fresh store did not reload cache");
            Require(!File.ReadAllText(path).Contains("IsCurrent"),"Cache retained an active-preset claim");
            Require(!(bool)Invoke(Store(path),"Save",(object)names.Take(79).ToArray())!,"Partial list replaced full cache");
            var duplicate=names.ToArray();duplicate[79]=names[0];Require(!(bool)Invoke(Store(path),"Save",(object)duplicate)!,"Duplicate slot accepted");
            window=(MainWindow)Activator.CreateInstance(typeof(MainWindow),Flags,null,[false,Store(path)],null)!;
            Require(Field<IReadOnlyList<PresetName>>(window,"names").Count==80&&Field<bool>(window,"presetNamesAreCached"),"Offline startup did not load 80 cached names");
            Require(Field<CurrentPresetSnapshot?>(window,"currentSnapshot") is null&&!Field<bool>(window,"editSnapshotValid")&&!Field<Button>(window,"selectPreset").IsEnabled,"Cached names enabled device edits");
            Require(Field<ComboBox>(window,"presets").Items.Count==80,"Offline dropdown unavailable");
            Field<ComboBox>(window,"presets").SelectedIndex=3;
            ((Task)Invoke(window,"RecallDropdownSelectionAsync")!).GetAwaiter().GetResult();
            Require(Field<ComboBox>(window,"presets").SelectedIndex==3&&!Field<Button>(window,"selectPreset").IsEnabled,
                "Offline browsing discarded selection or enabled recall");
            var report=(string)Invoke(window,"CreatePresetEffectReport")!;
            Require(report.Contains("offline cache")&&report.Contains("19D Original patch 80"),"Cached names missing from report");
            Field<TextBox>(window,"search").Text="patch 80";
            Require(Field<ComboBox>(window,"presets").Items.Count==1&&((string)Invoke(window,"CreatePresetEffectReport")!).Contains("0A Original patch 1"),"Offline search restricted full report");
            var renamed=names.Select(p=>p.WireIndex==4?p with{Name="Renamed lead"}:p).ToArray();
            typeof(MainWindow).GetField("names",Flags)!.SetValue(window,renamed);Invoke(window,"CacheLivePresetNames");
            Require(!Field<bool>(window,"presetNamesAreCached"),"Fresh names still labelled cached");
            window.Close();window=(MainWindow)Activator.CreateInstance(typeof(MainWindow),Flags,null,[false,Store(path)],null)!;
            Require(Field<IReadOnlyList<PresetName>>(window,"names").Single(p=>p.WireIndex==4).Name=="Renamed lead","Verified rename not persisted");
            var valid=File.ReadAllText(path);
            foreach(var invalid in new[]{"{",valid.Replace("\"SchemaVersion\":1","\"SchemaVersion\":2"),new string('x',65537)})
            {File.WriteAllText(path,invalid);Require(Invoke(Store(path),"Load") is null,"Malformed/unknown/oversize cache accepted");}
            var blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"file");
            Require(!(bool)Invoke(Store(Path.Combine(blocked,"names.json")),"Save",(object)names)!,"Unwritable cache unexpectedly succeeded");
            Require(Field<F15ReadSession?>(window,"connectedSession") is null,"Cache checks initiated hardware work");
            Console.WriteLine("PASS: persistent 80-name offline startup/report/search, no active/edit claims, refresh/rename persistence, atomic complete-list validation, corrupt/schema/oversize rejection and write-failure handling. No device commands.");
        }
        finally{window?.Close();foreach(var file in Directory.GetFiles(directory))File.Delete(file);Directory.Delete(directory);}
    }
}
