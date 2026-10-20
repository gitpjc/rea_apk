using System.IO;
using System.Text.Json;

namespace Iamp.Desktop;

internal sealed record DesktopSettings([property: System.Text.Json.Serialization.JsonRequired] int SchemaVersion,[property: System.Text.Json.Serialization.JsonRequired] bool PreserveWindowSize,double? WindowWidth,double? WindowHeight,bool WindowMaximized)
{
    internal static DesktopSettings Default=>new(1,true,null,null,false);
}

internal sealed class DesktopSettingsStore(string file)
{
    internal DesktopSettingsStore():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Iamp.Desktop","settings.json")){}
    internal DesktopSettings Load()
    {
        try {
            using var input=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(input.Length>4096)return DesktopSettings.Default;
            var bytes=new byte[4097];var count=0;
            while(count<bytes.Length){var read=input.Read(bytes,count,bytes.Length-count);if(read==0)break;count+=read;}
            if(count>4096)return DesktopSettings.Default;
            var settings=JsonSerializer.Deserialize<DesktopSettings>(bytes.AsSpan(0,count),new JsonSerializerOptions{MaxDepth=8});
            return settings is not null&&Valid(settings)?settings:DesktopSettings.Default;
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){return DesktopSettings.Default;}
    }
    internal bool Save(DesktopSettings settings)
    {
        if(!Valid(settings))return false;string? temporary=null;
        try {
            var path=Path.GetFullPath(file);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            File.WriteAllBytes(temporary,JsonSerializer.SerializeToUtf8Bytes(settings));File.Move(temporary,path,true);temporary=null;return true;
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){return false;}
        finally{if(temporary is not null)try{File.Delete(temporary);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){}}
    }
    private static bool Valid(DesktopSettings s)=>s.SchemaVersion==1&&
        ((s.WindowWidth is null&&s.WindowHeight is null)||(s.WindowWidth is {} w&&s.WindowHeight is {} h&&double.IsFinite(w)&&double.IsFinite(h)&&w is >=1 and <=5000&&h is >=1 and <=5000));
}
