using System.Windows;
using System.Windows.Threading;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private F15ReadSession? connectedSession;
    private ulong? connectedAddress;
    private readonly System.Windows.Controls.Button disconnect=Button("Disconnect");
    private readonly DispatcherTimer refreshTimer=new() {Interval=TimeSpan.FromSeconds(3)};
    private CancellationTokenSource? pollCancellation;
    private bool polling,closing;

    private bool TryReadTarget(out ulong target)
    {
        var text=address.Text.Replace(":","").Replace("-","").Replace(" ","");target=0;
        if(text.Length>0)return text.Length==12 && ulong.TryParse(text,System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture,out target) && target!=0;
        if(devices.SelectedItem is F15DiscoveredDevice found){target=found.Address;return target!=0;}
        return false;
    }
    private void UpdateConnectionControls()
    {
        read.IsEnabled=!preview && operation is null && TryReadTarget(out _);
        read.Content=connectedSession is null?"Connect amplifier":"Refresh amplifier";
        disconnect.IsEnabled=connectedSession is not null && operation is null;
        UpdateEffectFilterState();
    }
    private void InitializeConnection()
    {
        refreshTimer.Tick+=async(_,_)=>await PollCurrentPresetAsync();
        disconnect.Click+=async(_,_)=>{await ReleaseConnectionAsync();editSnapshotValid=false;UpdateThresholdState();status.Text="Disconnected · Displayed snapshot retained";snapshot.Text=SnapshotLabel();};
        devices.SelectionChanged+=async(_,_)=> {
            UpdateConnectionControls();
            if(!preview && operation is null && devices.SelectedItem is F15DiscoveredDevice && string.IsNullOrWhiteSpace(address.Text))await ReadAsync();
        };
        address.TextChanged+=async(_,_)=> {
            UpdateConnectionControls();
            if(operation is null && connectedSession is not null && (!TryReadTarget(out var selected)||selected!=connectedAddress)) {
                await ReleaseConnectionAsync();snapshot.Text=SnapshotLabel();
            }
        };
        Closing+=async(_,e)=> {
            if(closing)return;
            if(connectedSession is null && operation is null)return;
            e.Cancel=true;operation?.Cancel();await ReleaseConnectionAsync();closing=true;Close();
        };
        UpdateConnectionControls();
    }
    private async Task ReleaseConnectionAsync()
    {
        refreshTimer.Stop();pollCancellation?.Cancel();
        var session=connectedSession;connectedSession=null;connectedAddress=null;
        if(session is not null)await session.DisposeAsync();
        UpdateConnectionControls();
    }
    internal Task ReleasePreviewConnectionAsync()=>ReleaseConnectionAsync();
    private async Task<T> UseSessionAsync<T>(ulong target,Func<F15ReadSession,Task<T>> action)
    {
        if(connectedSession is {} existing && connectedAddress==target)return await action(existing);
        await using var temporary=await F15ReadSession.ConnectAsync(target,operation!.Token,diagnostics);
        return await action(temporary);
    }
    private bool HasPendingInputs()
    {
        CaptureParameterInputs();
        return HasPendingMixerInputs() || pendingParameterValues.Any(p=>currentSnapshot?.Blocks.FirstOrDefault(b=>b.Position==p.Key.Position) is {} block &&
            (p.Key.Index>=block.Values.Count || !TryParseParameterText(block,p.Key.Index,p.Value,out var value) || value!=block.Values[p.Key.Index]));
    }
    private async Task PollCurrentPresetAsync()
    {
        if(polling || presetSaveDialogOpen || mixerGestureActive || chainDrag is not null || chainPickerOpen || operation is not null || connectedSession is not {} session || !editSnapshotValid)return;
        polling=true;var previous=currentSnapshot;
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));pollCancellation=timeout;
        try {
            var current=await session.ReadCurrentPresetAsync(timeout.Token);
            if(operation is not null || presetSaveDialogOpen || mixerGestureActive || chainDrag is not null || chainPickerOpen || connectedSession!=session || !ReferenceEquals(previous,currentSnapshot))return;
            if(snapshotFirmware=="V1.1.9" && batteryReadAt is {} batteryTime && DateTimeOffset.UtcNow-batteryTime>=TimeSpan.FromSeconds(30)) {
                var battery=await session.ReadBatteryAsync(timeout.Token);
                if(operation is not null || presetSaveDialogOpen || mixerGestureActive || chainDrag is not null || chainPickerOpen || connectedSession!=session || !ReferenceEquals(previous,currentSnapshot))return;
                SetBatteryDisplay(battery);
            }
            if(current.RawStatePayloadHex==previous?.RawStatePayloadHex && current.RawDetailPayloadHex==previous.RawDetailPayloadHex){snapshotTime=DateTimeOffset.Now;snapshot.Text=SnapshotLabel();return;}
            if(HasPendingInputs()) {
                editSnapshotValid=false;refreshTimer.Stop();UpdateThresholdState();
                status.Text="Amplifier changed · Pending inputs preserved. Refresh amplifier before editing.";return;
            }
            if(previous?.WireIndex!=current.WireIndex || !liveEditUnsaved)originalSnapshotDetail=current.RawDetailPayloadHex;
            liveEditUnsaved=current.RawDetailPayloadHex!=originalSnapshotDetail;
            currentSnapshot=current;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{current.SlotLabel} · {current.Name}";SetEffects(current.Blocks);Filter();snapshot.Text=SnapshotLabel();
            status.Text=$"Current preset · {current.SlotLabel} · {current.Name}";
        }
        catch(Iamp.Protocol.CurrentPresetChangedException) {
            if(connectedSession==session) {
                await ReleaseConnectionAsync();editSnapshotValid=false;UpdateThresholdState();snapshot.Text=SnapshotLabel();
                status.Text="Amplifier kept changing while reading · Leave its controls still and reconnect.";
            }
        }
        catch(Exception) {
            if(connectedSession==session) {
                await ReleaseConnectionAsync();editSnapshotValid=false;UpdateThresholdState();snapshot.Text=SnapshotLabel();
                status.Text="Amplifier connection lost · Reconnect to refresh the preset.";
            }
        }
        finally {if(pollCancellation==timeout)pollCancellation=null;polling=false;UpdateEffectPowerState();UpdateMixerState();}
    }
    private void SetBatteryDisplay(Iamp.Protocol.BatteryStatus battery)
    {
        batteryReadAt=DateTimeOffset.UtcNow;
        batterySummary.Text=$"Battery · {battery.Percentage}% · Read {DateTimeOffset.Now:HH:mm:ss}";
    }
}
