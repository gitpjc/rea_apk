using System.Reflection;
using System.IO;
using Iamp.Desktop;
using Iamp.Protocol;

internal static class EffectCacheStoreChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    internal static void Run()
    {
        var root=Path.GetFullPath(Path.GetTempPath());var directory=Path.GetFullPath(Path.Combine(root,"iamp-effect-cache-check-"+Guid.NewGuid().ToString("N")));
        if(!directory.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Unexpected cache check path.");
        Directory.CreateDirectory(directory);
        var type=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetEffectCacheStore")!;
        object Store(string path)=>Activator.CreateInstance(type,Flags,null,new object[]{path},null)!;
        object? Invoke(object store,string method,params object[] args)=>type.GetMethod(method,Flags)!.Invoke(store,args);
        void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        var records=Enumerable.Range(1,80).Select(i=>new DecodedPreset((byte)i,"not cached",new[]{new PresetEffectBlock(1,4,new ushort[]{0,2,25,50,0,0})},Array.Empty<byte>())).ToArray();
        try {
            var first=Store(directory);Require(Invoke(first,"Load",(ulong)1,"V1.1.9") is null,"Cache miss differs.");
            Require((bool)Invoke(first,"Save",(ulong)1,"V1.1.9",records)!,"Cache save failed.");
            var second=Store(directory);Require(Invoke(second,"Load",(ulong)1,"V1.1.9") is PresetEffectCache {Presets.Length:80},"New process/store did not load persistent data.");
            Require(Invoke(second,"Load",(ulong)2,"V1.1.9") is null && Invoke(second,"Load",(ulong)1,"V1.1.2") is null,"Cache crossed device/firmware boundaries.");
            Require(!Directory.GetFiles(directory).Any(p=>p.EndsWith(".tmp")),"Atomic save left a temporary file.");
            Require((bool)Invoke(second,"Invalidate",(ulong)1,"V1.1.9")! && Invoke(second,"Load",(ulong)1,"V1.1.9") is null && Invoke(Store(directory),"Load",(ulong)1,"V1.1.9") is null,"Invalidation retained stale memory or disk data.");
            Require((bool)Invoke(first,"Save",(ulong)1,"V1.1.9",records)!,"Cache did not recover after invalidation.");
            var file=Directory.GetFiles(directory,"*.json").Single();File.WriteAllText(file,"{");
            Require(Invoke(Store(directory),"Load",(ulong)1,"V1.1.9") is null,"Corrupt cache accepted.");
            File.WriteAllBytes(file,new byte[]{0xff,0xff});Require(Invoke(Store(directory),"Load",(ulong)1,"V1.1.9") is null,"Invalid UTF-8 cache accepted.");
            File.WriteAllText(file,new string('x',PresetEffectCacheCodec.MaximumCharacters+1));Require(Invoke(Store(directory),"Load",(ulong)1,"V1.1.9") is null,"Oversize cache accepted.");
            var blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"file, not directory");var memory=Store(blocked);
            Require(!(bool)Invoke(memory,"Save",(ulong)3,"V1.1.9",records)! && Invoke(memory,"Load",(ulong)3,"V1.1.9") is PresetEffectCache,"Failed disk write lost the in-memory cache.");
            Invoke(memory,"Invalidate",(ulong)3,"V1.1.9");Require(Invoke(memory,"Load",(ulong)3,"V1.1.9") is null,"Invalidation retained fallback memory data.");
            Console.WriteLine("PASS: persistent effect cache reload, device/firmware separation, atomic save, corrupt/UTF-8/oversize rejection and in-memory fallback.");
        }
        finally {foreach(var file in Directory.GetFiles(directory))File.Delete(file);Directory.Delete(directory);}
    }
}
