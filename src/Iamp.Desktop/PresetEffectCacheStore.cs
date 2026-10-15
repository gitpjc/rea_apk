using System.IO;
using System.Security.Cryptography;
using System.Text;
using Iamp.Protocol;

namespace Iamp.Desktop;

internal sealed class PresetEffectCacheStore(string directory)
{
    internal PresetEffectCacheStore():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Iamp.Desktop","EffectCache")){}
    private readonly Dictionary<string,PresetEffectCache> memory=new();

    internal static string Key(ulong address,string firmware)
        =>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{address:X12}|{firmware}")));

    internal PresetEffectCache? Load(ulong address,string firmware)
    {
        var key=Key(address,firmware);if(memory.TryGetValue(key,out var cached))return cached;
        try {
            var path=Path.Combine(directory,key+".json");
            using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(input.Length>PresetEffectCacheCodec.MaximumCharacters)return null;
            var bytes=new byte[PresetEffectCacheCodec.MaximumCharacters+1];var count=0;
            while(count<bytes.Length){var n=input.Read(bytes,count,bytes.Length-count);if(n==0)break;count+=n;}
            if(count>PresetEffectCacheCodec.MaximumCharacters)return null;
            var result=PresetEffectCacheCodec.Decode(new UTF8Encoding(false,true).GetString(bytes,0,count),firmware);
            memory[key]=result;return result;
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or FormatException or DecoderFallbackException){return null;}
    }

    internal bool Save(ulong address,string firmware,IEnumerable<DecodedPreset> records)
    {
        var json=PresetEffectCacheCodec.Encode(firmware,DateTimeOffset.Now,records);var key=Key(address,firmware);
        memory[key]=PresetEffectCacheCodec.Decode(json,firmware);
        string? temporary=null;
        try {
            Directory.CreateDirectory(directory);var path=Path.Combine(directory,key+".json");temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            File.WriteAllText(temporary,json,new UTF8Encoding(false));File.Move(temporary,path,true);return true;
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException){return false;}
        finally {if(temporary is not null)try{File.Delete(temporary);}catch(Exception e) when(e is IOException or UnauthorizedAccessException){} }
    }
}
