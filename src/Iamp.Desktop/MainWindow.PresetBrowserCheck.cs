using System.IO;
using System.Text.Json;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    internal async Task RunScanConnectCheckAsync(string targetText,string output)
    {
        await ScanAsync();
        if(!lastReadSucceeded || connectedSession is null || !TryReadTarget(out var actual) || actual!=Convert.ToUInt64(targetText,16) || names.Count!=80)
            throw new IOException("Scan did not automatically connect and load the target amplifier.");
        if(currentSnapshot?.SlotLabel!="0D" || currentPreset.Text!="0D · British Lead Solo" || read.Content?.ToString()!="Refresh amplifier")
            throw new IOException("Connected preset display differs from the amplifier.");
        var prefix=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        await ExportSnapshotToPathAsync(prefix+".iamp-snapshot.json");
        await ExportDiagnosticsToPathAsync(prefix+".iamp-diagnostics.json");
        status.Text="Scan connected automatically · 80 presets loaded · Current 0D";
        await File.WriteAllTextAsync(prefix+".scan.json",JsonSerializer.Serialize(new {version=DesktopVersion.Current,passed=true,presets=names.Count,current=currentSnapshot.SlotLabel,connected=true}));
        Console.WriteLine("Scan/connect hardware QA passed: automatic library/current load, retained connection and 0D label.");
    }

    internal async Task RunConnectionSyncCheckAsync(string targetText,string output)
    {
        if(read.IsEnabled)throw new IOException("Connect was enabled without a target.");
        address.Text="invalid";if(read.IsEnabled)throw new IOException("Invalid target enabled Connect.");
        address.Text="000000000000";if(read.IsEnabled)throw new IOException("Zero target enabled Connect.");
        address.Text=targetText;if(!read.IsEnabled)throw new IOException("Valid target did not enable Connect.");
        await ReadAsync();
        if(!lastReadSucceeded || connectedSession is not {} session || currentSnapshot is not {} original)throw new IOException("Connect failed.");
        _=ObservedPresetSaveCommand.CreateUnchanged(snapshotFirmware!,Convert.FromHexString(original.RawStatePayloadHex),Convert.FromHexString(original.RawDetailPayloadHex));
        var target=snapshotAddress!.Value;var automatic=false;var pendingPreserved=false;var restored=false;string? failure=null;
        var prefix=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        await File.WriteAllTextAsync(prefix+".baseline.json",DisplayedSnapshotJson());
        bool Matches(CurrentPresetSnapshot value)=>value.RawStatePayloadHex==original.RawStatePayloadHex&&value.RawDetailPayloadHex==original.RawDetailPayloadHex;
        async Task WaitUntil(Func<bool> ready) {
            var until=DateTimeOffset.UtcNow.AddSeconds(15);
            while(!ready() && DateTimeOffset.UtcNow<until)await Task.Delay(100);
            if(!ready())throw new IOException("Automatic synchronization did not complete.");
        }
        try {
            // Change only the amp's selected preset through the low-level API, not the desktop handler.
            // The production timer must notice it without pressing Refresh.
            await session.SelectPresetAsync(original,5,CancellationToken.None);
            await WaitUntil(()=>currentSnapshot?.WireIndex==5);
            automatic=currentPreset.Text.StartsWith("1A · ",StringComparison.Ordinal)&&presets.Items.OfType<PresetName>().Single(p=>p.WireIndex==5).IsCurrent;
            presets.SelectedItem=presets.Items.OfType<PresetName>().Single(p=>p.WireIndex==4);await SelectPresetAsync();
            if(!lastMutationSucceeded || currentSnapshot is not {} returned || !Matches(returned))throw new IOException("First sync test was not restored.");
            thresholdInput.Text="26";
            await session.SelectPresetAsync(returned,5,CancellationToken.None);
            await WaitUntil(()=>!editSnapshotValid);
            pendingPreserved=thresholdInput.Text=="26"&&currentSnapshot.WireIndex==4&&!applyThreshold.IsEnabled;
        }
        catch(Exception ex){failure=ex.ToString();}
        finally {
            await ReleaseConnectionAsync();
            try {
                using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await using var cleanup=await F15ReadSession.ConnectAsync(target,deadline.Token);
                var actual=await cleanup.ReadCurrentPresetAsync(deadline.Token);
                if(!Matches(actual))actual=await cleanup.SelectPresetAsync(actual,(byte)original.WireIndex,deadline.Token);
                restored=Matches(actual);
                currentSnapshot=actual;SetEffects(actual.Blocks);Filter();thresholdInput.Text="25";
                currentPreset.Text=$"{actual.SlotLabel} · {actual.Name}";snapshot.Text=SnapshotLabel();
            }
            catch(Exception ex){failure=(failure??"")+"\nCleanup: "+ex;}
            await File.WriteAllTextAsync(prefix+".sync.json",JsonSerializer.Serialize(new {version=DesktopVersion.Current,passed=failure is null&&automatic&&pendingPreserved&&restored,automatic,pendingPreserved,baselineRestored=restored,failure},new JsonSerializerOptions {WriteIndented=true}));
        }
        if(failure is not null || !automatic || !pendingPreserved || !restored)throw new IOException(failure??"Synchronization validation failed.");
        status.Text="Automatic preset sync verified · Pending inputs protected · Original 0D restored";
        Console.WriteLine("Connection sync hardware QA passed: timer updated current preset/highlight; pending inputs preserved; exact original restored.");
    }

    internal async Task RunPresetBrowserCheckAsync(string targetText,string output)
    {
        address.Text=targetText;await ReadAsync();
        if(!lastReadSucceeded || connectedSession is null || currentSnapshot is not {} original)throw new IOException("Connect failed.");
        _=ObservedPresetSaveCommand.CreateUnchanged(snapshotFirmware!,Convert.FromHexString(original.RawStatePayloadHex),Convert.FromHexString(original.RawDetailPayloadHex));
        var target=snapshotAddress!.Value;
        var prefix=Path.GetFullPath(output);Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        await File.WriteAllTextAsync(prefix+".baseline.json",DisplayedSnapshotJson());
        var results=new List<object>();string? failure=null;var restored=false;
        bool Matches(CurrentPresetSnapshot value)=>value.RawStatePayloadHex==original.RawStatePayloadHex&&value.RawDetailPayloadHex==original.RawDetailPayloadHex;
        try {
            if(names.Count!=80 || read.Content?.ToString()!="Refresh amplifier" || !disconnect.IsEnabled)throw new IOException("Connected browser is incomplete.");
            await PollCurrentPresetAsync();
            for(var index=1;index<=80;index++) {
                if(index==original.WireIndex)continue;
                if(currentSnapshot is not {} start || !Matches(start))throw new IOException("Original baseline differs before the next test.");
                presets.SelectedItem=presets.Items.OfType<PresetName>().Single(p=>p.WireIndex==index);
                await SelectPresetAsync();
                if(!lastMutationSucceeded || currentSnapshot?.WireIndex!=index)throw new IOException($"Preset {index} recall failed.");
                var label=currentSnapshot.SlotLabel;var name=currentSnapshot.Name;
                if(currentPreset.Text!=$"{label} · {name}" || !presets.Items.OfType<PresetName>().Single(p=>p.WireIndex==index).IsCurrent)
                    throw new IOException("Current label/highlight did not update.");
                presets.SelectedItem=presets.Items.OfType<PresetName>().Single(p=>p.WireIndex==original.WireIndex);
                await SelectPresetAsync();
                if(!lastMutationSucceeded || currentSnapshot is not {} returned || !Matches(returned))throw new IOException("Complete original baseline was not restored.");
                results.Add(new {index,label,name,originalRestored=true});
                if(results.Count%10==0)Console.WriteLine($"Preset browser: {results.Count}/79 alternative presets verified and original restored after each.");
            }
        }
        catch(Exception ex){failure=ex.ToString();}
        finally {
            await ReleaseConnectionAsync();
            try {
                using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await using var cleanup=await F15ReadSession.ConnectAsync(target,deadline.Token);
                var current=await cleanup.ReadCurrentPresetAsync(deadline.Token);
                if(!Matches(current))current=await cleanup.SelectPresetAsync(current,(byte)original.WireIndex,deadline.Token);
                restored=Matches(current);
                if(!restored)throw new IOException("Independent original baseline verification failed.");
                currentSnapshot=current;originalSnapshotDetail=current.RawDetailPayloadHex;liveEditUnsaved=false;editSnapshotValid=true;
                currentPreset.Text=$"{current.SlotLabel} · {current.Name}";SetEffects(current.Blocks);Filter();snapshot.Text=SnapshotLabel();UpdateThresholdState();
            }
            catch(Exception ex){failure=(failure??"")+"\nCleanup: "+ex;}
            await File.WriteAllTextAsync(prefix+".roundtrip.json",JsonSerializer.Serialize(new {version=DesktopVersion.Current,passed=failure is null&&restored&&results.Count==79,baselineRestored=restored,results,failure},new JsonSerializerOptions {WriteIndented=true}));
        }
        if(failure is not null || !restored)throw new IOException(failure??"Restoration failed.");
        status.Text="All 80 presets verified · Original 0D restored · Nothing saved";presets.BringIntoView();
        Console.WriteLine("Preset browser hardware QA passed: 79 alternative slots, exact original restored after every recall and independently verified.");
    }
}
