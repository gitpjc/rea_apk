using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    /// <summary>Explicit bounded developer check; normal UI handlers, no preset recalls or saves.</summary>
    internal async Task RunDrumCheckAsync(string target,string output)
    {
        var prefix=Path.GetFullPath(output);
        if(File.Exists(prefix+".result.json")||File.Exists(prefix+".before.json"))throw new IOException("Use a new Drum evidence prefix.");
        Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        address.Text=target;await ReadAsync();
        if(!lastReadSucceeded||connectedSession is null||currentSnapshot is not {} before)throw new IOException("Desktop connection failed.");
        var original=DrumSettings.Parse(Convert.FromHexString(before.RawStatePayloadHex));
        await File.WriteAllTextAsync(prefix+".before.json",JsonSerializer.Serialize(before));
        var steps=new List<object>();bool? originalPlaying=null;string? failure=null;var restored=false;
        async Task WaitAsync()
        {
            var end=DateTimeOffset.UtcNow.AddSeconds(80);
            while(operation is not null||drumStarting||drumWriting)
            {if(DateTimeOffset.UtcNow>end)throw new TimeoutException("Desktop Drum operation timed out.");await Task.Delay(40);}
            if(!editSnapshotValid||!drumReady)throw new IOException("Desktop Drum is not ready: "+drumSummary.Text);
        }
        void Click(Button b){if(!b.IsEnabled)throw new IOException("Desktop Drum control unexpectedly disabled.");b.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));}
        void Check(int? tempo=null,int? volume=null,byte? pattern=null)
        {
            var s=ReadDisplayedDrum()??throw new IOException("Missing desktop Drum settings.");
            if(tempo is {} t&&s.Tempo!=t||volume is {} v&&s.Volume!=v||pattern is {} p&&s.Pattern.Id!=p)throw new IOException("Desktop Drum settings differ.");
            steps.Add(new{at=DateTimeOffset.UtcNow,s.Tempo,s.Volume,pattern=s.Pattern.DisplayName,playing=drumPlaying,
                dial=drumDial.Tempo,patternLabel=drumPatternLabel.Text,playButton=AutomationName(drumPlay)});
        }
        try
        {
            await NavigateToPageAsync(drumScroll);await WaitAsync();originalPlaying=drumPlaying;Check();
            // Exercise the same dial event and coalescing path as dragging, including a newer pending value.
            var changedTempo=original.Tempo==260?259:original.Tempo+1;
            var peer=System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(drumDial)!;
            var range=(System.Windows.Automation.Provider.IRangeValueProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.RangeValue)!;
            range.SetValue(changedTempo);range.SetValue(Math.Min(260,changedTempo+1));range.SetValue(changedTempo);
            await WaitAsync();Check(tempo:changedTempo);
            range.SetValue(original.Tempo);await WaitAsync();Check(tempo:original.Tempo);
            drumVolume.Value=original.Volume==0?1:original.Volume-1;await WaitAsync();Check(volume:(int)drumVolume.Value);
            drumVolume.Value=original.Volume;await WaitAsync();Check(volume:original.Volume);
            var next=(byte)((original.Pattern.Id+1)%70);
            drumStyle.SelectedIndex=next/10;Click(drumPatternButtons[next%10]);await WaitAsync();Check(pattern:next);
            drumStyle.SelectedIndex=original.Pattern.Id/10;Click(drumPatternButtons[original.Pattern.Id%10]);await WaitAsync();Check(pattern:original.Pattern.Id);
            if(!drumPlaying){Click(drumPlay);await WaitAsync();if(!drumPlaying)throw new IOException("Desktop playback did not start.");}
            await NavigateToPageAsync(libraryScroll);
            if(drumReady||drumScroll.Visibility!=Visibility.Collapsed)throw new IOException($"Desktop did not leave Drum: ready={drumReady}, writing={drumWriting}, starting={drumStarting}, operation={operation is not null}, notice={drumNotice}");
            await NavigateToPageAsync(drumScroll);await WaitAsync();if(!drumPlaying)throw new IOException("Playback did not survive page navigation.");Check();
            await NavigateToPageAsync(tunerScroll);
            if(!tunerActive)throw new IOException("Tuner did not start while Drum was playing.");
            await NavigateToPageAsync(drumScroll);await WaitAsync();
            if(!drumPlaying)throw new IOException("Drum playback did not survive the Tuner roundtrip observed in mobile.");Check();
            Click(drumPlay);await WaitAsync();if(drumPlaying)throw new IOException("Pause failed.");Check();
            TapDrum();await Task.Delay(1000);TapDrum();await WaitAsync();Check();
            range.SetValue(original.Tempo);await WaitAsync();
            if(originalPlaying.Value!=drumPlaying){Click(drumPlay);await WaitAsync();}
        }
        catch(Exception e){failure=e.ToString();}
        finally
        {
            try
            {
                await ReleaseConnectionAsync();
                using var cleanup=new CancellationTokenSource(TimeSpan.FromSeconds(75));
                await using var independent=await F15ReadSession.ConnectAsync(Convert.ToUInt64(target.Replace(":","").Replace("-",""),16),cleanup.Token);
                await independent.ReadIdentityAsync(cleanup.Token);
                var current=await independent.ReadCurrentPresetAsync(cleanup.Token);
                var a=Convert.FromHexString(before.RawStatePayloadHex);var b=Convert.FromHexString(current.RawStatePayloadHex);
                if(a.Length!=b.Length||current.RawDetailPayloadHex!=before.RawDetailPayloadHex||a.Where((v,i)=>i is not (40 or 41 or 42 or 43)&&v!=b[i]).Any())
                    throw new IOException("Unrelated settings differ; cleanup will not overwrite them.");
                var entry=await independent.EnterDrumAsync(current,cleanup.Token);
                if(originalPlaying is {} playing&&entry.Playing!=playing)current=(await independent.SetDrumPlaybackAsync(current,playing,cleanup.Token)).Current;
                var settings=DrumSettings.Parse(Convert.FromHexString(current.RawStatePayloadHex));
                if(settings.Tempo!=original.Tempo)current=await independent.SetDrumTempoAsync(current,original.Tempo,cleanup.Token);
                if(settings.Volume!=original.Volume)current=await independent.SetDrumVolumeAsync(current,original.Volume,cleanup.Token);
                if(settings.Pattern.Id!=original.Pattern.Id)current=await independent.SetDrumPatternAsync(current,original.Pattern.Id,cleanup.Token);
                await independent.ExitDrumAsync(cleanup.Token);
                var after=await independent.ReadCurrentPresetAsync(cleanup.Token);
                restored=after.RawStatePayloadHex==before.RawStatePayloadHex&&after.RawDetailPayloadHex==before.RawDetailPayloadHex;
                if(!restored)throw new IOException("Complete original comparison differs.");
            }
            catch(Exception e){failure=(failure??"")+"\nCleanup: "+e;}
            await File.WriteAllTextAsync(prefix+".result.json",JsonSerializer.Serialize(new{version=DesktopVersion.Current,restored,failure,steps},new JsonSerializerOptions{WriteIndented=true}));
        }
        if(failure is not null||!restored)throw new IOException(failure??"Original restoration failed.");
        Console.WriteLine("PASS: actual desktop Drum handlers, coalesced dial, volume, pattern, playback/page return and TAP; independently restored complete original state/detail.");
    }
    private static string AutomationName(FrameworkElement control)=>System.Windows.Automation.AutomationProperties.GetName(control);
}
