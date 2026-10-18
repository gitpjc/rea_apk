using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    /// <summary>Explicit developer acceptance run using normal connection/navigation handlers; never saves or recalls.</summary>
    internal async Task RunTunerCheckAsync(string target, string output)
    {
        var prefix=Path.GetFullPath(output);
        if(new[]{".ready",".result.json",".live.json",".before.json"}.Any(s=>File.Exists(prefix+s)))
            throw new IOException("Use a new tuner check output prefix to preserve evidence.");
        Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        address.Text=target;await ReadAsync();
        if(!lastReadSucceeded || connectedSession is not {} session || currentSnapshot is not {} before)
            throw new IOException("Normal desktop connection failed.");
        await File.WriteAllTextAsync(prefix+".before.json",JsonSerializer.Serialize(before));
        var received=new ConcurrentQueue<object>();var displayed=new List<object>();
        void OnPayload(byte[] payload)
        {
            received.Enqueue(new{at=DateTimeOffset.UtcNow,payload=Convert.ToHexString(payload)});
            while(received.Count>2048)received.TryDequeue(out _);
        }
        session.TunerPayloadReceived+=OnPayload;
        string? failure=null;bool restored=false;string? previousDisplay=null;
        try
        {
            NavigateToPage(tunerScroll);
            var start=DateTimeOffset.UtcNow;
            while((tunerStarting || operation is not null) && DateTimeOffset.UtcNow-start<TimeSpan.FromSeconds(80))await Task.Delay(50);
            if(!tunerActive || !session.IsAvailable)throw new IOException("Normal desktop tuner entry failed.");
            await File.WriteAllTextAsync(prefix+".ready","Tuner check active");
            Console.WriteLine("Normal desktop tuner check ready; local event trace active.");
            var until=DateTimeOffset.UtcNow.AddMinutes(10);
            while(DateTimeOffset.UtcNow<until && !File.Exists(prefix+".stop") && connectedSession==session && session.IsAvailable)
            {
                var display=tunerNote.Text+" / "+tunerDeviation.Text;
                if(display!=previousDisplay){displayed.Add(new{at=DateTimeOffset.UtcNow,note=tunerNote.Text,indicator=tunerDeviation.Text});previousDisplay=display;if(displayed.Count>2048)displayed.RemoveAt(0);}
                await File.WriteAllTextAsync(prefix+".live.json",JsonSerializer.Serialize(new{at=DateTimeOffset.UtcNow,tunerActive,sessionAvailable=session.IsAvailable,
                    amplifierInactive=tunerAmpInactive,notice=tunerSummary.Text,note=tunerNote.Text,indicator=tunerDeviation.Text,received=received.TakeLast(2048).ToArray(),displayed=displayed.TakeLast(2048).ToArray()}));
                await File.WriteAllTextAsync(prefix+".diagnostics.json",diagnostics.Export(DesktopVersion.Current));
                await Task.Delay(250);
            }
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            session.TunerPayloadReceived-=OnPayload;
            try
            {
                if(tunerActive && !await StopTunerAsync())throw new IOException("Desktop tuner exit unconfirmed.");
                await ReleaseConnectionAsync();
                using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(45));
                var addressValue=Convert.ToUInt64(target.Replace(":","").Replace("-",""),16);
                await using var independent=await F15ReadSession.ConnectAsync(addressValue,deadline.Token);
                var after=await independent.ReadCurrentPresetAsync(deadline.Token);
                restored=after.RawStatePayloadHex==before.RawStatePayloadHex && after.RawDetailPayloadHex==before.RawDetailPayloadHex;
                if(!restored)throw new IOException("Independent original state/detail comparison differs; no restoration write attempted.");
            }
            catch(Exception error){failure=(failure??"")+"\nCleanup: "+error;}
            await File.WriteAllTextAsync(prefix+".diagnostics.json",diagnostics.Export(DesktopVersion.Current));
            await File.WriteAllTextAsync(prefix+".result.json",JsonSerializer.Serialize(new{version=DesktopVersion.Current,restored,failure,
                received=received.TakeLast(2048).ToArray(),displayed=displayed.TakeLast(2048).ToArray()}));
        }
        if(failure is not null || !restored)throw new IOException(failure??"Original verification failed.");
    }
}
