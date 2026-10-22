using System.Windows;
using System.Windows.Controls;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private FrameworkElement sharedHeader=null!;
    private readonly TextBlock amplifierHeading=Text("Amplifier · Disconnected",17),amplifierPreset=Text("No amplifier preset read yet",13,MutedBrush,new Thickness(0,4,0,0));
    private readonly Expander amplifierDiscovery=new(){Header="Manual Bluetooth address",Foreground=MutedBrush};
    private CurrentPresetSnapshot? observedAmplifierPreset;
    private CurrentPresetSnapshot? headerEditorSnapshot;

    private void UpdateSharedHeader()
    {
        var connected=connectedSession is {IsAvailable:true};
        cancel.Visibility=operation is not null&&(connectionScanInProgress||connectionReadInProgress)?Visibility.Visible:Visibility.Collapsed;
        var name=devices.Items.OfType<F15DiscoveredDevice>().FirstOrDefault(d=>d.Address==connectedAddress)?.Name;
        amplifierHeading.Text=connected?$"{(string.IsNullOrWhiteSpace(name)?"Amplifier":name)} · Connected via Bluetooth":"Amplifier · Disconnected";
        if(preview){amplifierHeading.Text="Demo · No amplifier connected";amplifierDiscovery.IsExpanded=false;}
        if(editSnapshotValid&&!localSnapshotSource&&currentSnapshot is not null&&!ReferenceEquals(headerEditorSnapshot,currentSnapshot))
        {observedAmplifierPreset=currentSnapshot;headerEditorSnapshot=currentSnapshot;}
        var known=observedAmplifierPreset??(!localSnapshotSource?currentSnapshot:null);
        amplifierPreset.Text=known is null?"No amplifier preset read yet":
            $"{(preview?"Demo preset":connected?"Current preset · last confirmed":"Last read preset")}: {known.SlotLabel} · {known.Name}"+
            (connected&&!editSnapshotValid&&!tunerActive?" · Editor needs refresh":"");
        if(connectedAddress is {} target)amplifierHeading.ToolTip=$"Bluetooth address · {target:X12}";
        else amplifierHeading.ToolTip=null;
        UpdateSnapshotContext();
        UpdateFootswitchState();
    }
}
