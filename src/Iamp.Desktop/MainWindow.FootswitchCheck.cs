using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    /// <summary>Read-only desktop handler check. Never pairs, unpairs, recalls or saves.</summary>
    internal async Task RunFootswitchCheckAsync(string target,string output,bool watch=false)
    {
        var prefix=Path.GetFullPath(output);if(File.Exists(prefix+".result.json"))throw new IOException("Use a new Footswitch evidence prefix.");
        Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);address.Text=target;await ReadAsync();
        if(!lastReadSucceeded||connectedSession is not {} session||currentSnapshot is not {} before)throw new IOException("Desktop connection failed.");
        await NavigateToPageAsync(footswitchScroll);
        // The amplifier can initially report 97 00 before the already-paired F4 reconnects.
        // Observe its actual later notification instead of failing at the first read.
        var connectionDeadline=DateTimeOffset.UtcNow.AddSeconds(10);
        while(session.IsAvailable&&session.FootswitchStatus!=FootswitchConnectionStatus.Connected&&DateTimeOffset.UtcNow<connectionDeadline)await Task.Delay(100);
        UpdateFootswitchState();
        var observations=new List<object>();
        if(watch){
            var until=DateTimeOffset.UtcNow.AddSeconds(90);
            Console.WriteLine("F4 read-only watch ready. Waiting for user footswitch actions; no pairing or preset writes will be sent by this check.");
            while(DateTimeOffset.UtcNow<until&&!File.Exists(prefix+".stop")){
                observations.Add(new {time=DateTimeOffset.UtcNow,footswitch=session.FootswitchStatus.ToString(),slot=currentSnapshot?.SlotLabel,name=currentSnapshot?.Name});
                await File.WriteAllTextAsync(prefix+".progress.json",JsonSerializer.Serialize(observations));await Task.Delay(500);
            }
        }
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var after=await session.ReadCurrentPresetAsync(timeout.Token);UpdateFootswitchState();
        var unchanged=before.RawStatePayloadHex==after.RawStatePayloadHex&&before.RawDetailPayloadHex==after.RawDetailPayloadHex;
        var status=session.FootswitchStatus;
        var root=(FrameworkElement)Content;var size=new Size(1280,900);root.Width=size.Width;root.Height=size.Height;root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
        var bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(prefix+".png"))encoder.Save(stream);
        await File.WriteAllTextAsync(prefix+".result.json",JsonSerializer.Serialize(new {version=DesktopVersion.Current,passed=unchanged&&status==FootswitchConnectionStatus.Connected,footswitch=status.ToString(),display=footswitchStatus.Text,pairEnabled=pairFootswitch.IsEnabled,forgetEnabled=forgetFootswitch.IsEnabled,before,after,unchanged,observations}));
        if(!unchanged||status!=FootswitchConnectionStatus.Connected)throw new IOException("Read-only F4 check did not verify a connected F4 and unchanged preset.");
        Console.WriteLine("PASS: normal desktop read and Footswitch page report connected F4; complete preset state/detail unchanged. No pairing or preset writes.");
    }
}
