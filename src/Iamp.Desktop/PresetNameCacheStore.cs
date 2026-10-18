using System.IO;
using System.Text.Json;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

internal sealed record PresetNameCacheEntry(int WireIndex,string Name);
internal sealed record PresetNameCache(int SchemaVersion,DateTimeOffset ReadAt,PresetNameCacheEntry[] Presets);

internal sealed class PresetNameCacheStore(string file)
{
    private const int MaximumBytes=65536;
    internal PresetNameCacheStore():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Iamp.Desktop","preset-names.json")){}

    internal PresetNameCache? Load()
    {
        try
        {
            using var input=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(input.Length>MaximumBytes)return null;
            var bytes=new byte[MaximumBytes+1];var count=0;
            while(count<bytes.Length){var read=input.Read(bytes,count,bytes.Length-count);if(read==0)break;count+=read;}
            if(count>MaximumBytes)return null;
            var cached=JsonSerializer.Deserialize<PresetNameCache>(bytes.AsSpan(0,count),new JsonSerializerOptions{MaxDepth=16});
            return cached is {SchemaVersion:1}&&cached.ReadAt!=default&&Valid(cached.Presets)?cached:null;
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){return null;}
    }

    internal bool Save(IEnumerable<PresetName> names)
    {
        var entries=names.Select(p=>new PresetNameCacheEntry(p.WireIndex,p.Name)).OrderBy(p=>p.WireIndex).ToArray();
        if(!Valid(entries))return false;
        string? temporary=null;
        try
        {
            var path=Path.GetFullPath(file);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            File.WriteAllBytes(temporary,JsonSerializer.SerializeToUtf8Bytes(new PresetNameCache(1,DateTimeOffset.UtcNow,entries)));
            File.Move(temporary,path,true);return true;
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){return false;}
        finally{if(temporary is not null)try{File.Delete(temporary);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){}}
    }

    private static bool Valid(PresetNameCacheEntry[]? entries)=>entries is {Length:80}&&entries.All(p=>p is not null&&p.WireIndex is >=1 and <=80&&
        !string.IsNullOrWhiteSpace(p.Name)&&p.Name.Length<=128&&!p.Name.Any(char.IsControl))&&entries.Select(p=>p.WireIndex).Distinct().Count()==80;
}
