using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private sealed class PendingParameterEditor(string label,byte position,int parameterIndex,ushort minimum,ushort maximum,
        Func<F15ReadSession,CurrentPresetSnapshot,ushort,CancellationToken,Task<CurrentPresetSnapshot>> write)
    {
        internal string Label {get;}=label;
        internal byte Position {get;}=position;
        internal int ParameterIndex {get;}=parameterIndex;
        internal ushort Minimum {get;}=minimum;
        internal ushort Maximum {get;}=maximum;
        internal TextBox Input {get;}=new() {Width=90,MaxLength=3};
        internal Button Apply {get;}=MainWindow.Button($"Apply {label} to amp");
        internal TextBlock Message {get;}=Text("",12,MutedBrush);
        internal Func<F15ReadSession,CurrentPresetSnapshot,ushort,CancellationToken,Task<CurrentPresetSnapshot>> Write {get;}=write;
        internal bool TryValue(out ushort value)=>ushort.TryParse(Input.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) && value>=Minimum && value<=Maximum;
    }
    private readonly PendingParameterEditor noiseLevelEditor=new("Level",1,3,47,50,(s,e,v,ct)=>s.SetNoiseKillerLevelAsync(e,v,ct));
    private readonly PendingParameterEditor gainEditor=new("Gain",2,2,36,39,(s,e,v,ct)=>s.Set808GainAsync(e,v,ct));
    private readonly PendingParameterEditor volumeEditor=new("Vol",2,4,97,100,(s,e,v,ct)=>s.Set808VolumeAsync(e,v,ct));
    private IEnumerable<PendingParameterEditor> AdditionalEditors=>new[]{noiseLevelEditor,gainEditor,volumeEditor};

    private void InitializeAdditionalEditors()
    {
        foreach(var editor in AdditionalEditors) {
            editor.Input.TextChanged+=(_,_)=>UpdateAdditionalEditorStates();
            editor.Apply.Click+=async(_,_)=>await ApplyAdditionalParameterAsync(editor);
            System.Windows.Automation.AutomationProperties.SetName(editor.Input,$"Pending {editor.Label}, {editor.Minimum} to {editor.Maximum}");
        }
    }
    private void DetachAdditionalEditorControls()
    {
        foreach(var editor in AdditionalEditors) {
            if(editor.Input.Parent is Panel inputParent)inputParent.Children.Remove(editor.Input);
            if(editor.Apply.Parent is Panel applyParent)applyParent.Children.Remove(editor.Apply);
        }
    }
    private void AddAdditionalEditors(PresetEffectBlock displayed)
    {
        var knownNoise=displayed.Position==1 && displayed.TypeCode==4 && displayed.Values.Count==6 && displayed.Values[1]==2;
        var knownOd=displayed.Position==2 && displayed.TypeCode==1 && displayed.Values.Count==5 && displayed.Values[1]==3;
        foreach(var editor in AdditionalEditors.Where(e=>e.Position==displayed.Position && (knownNoise||knownOd))) {
            effectDetails.Children.Add(Text($"Pending {editor.Label} · Experimental supported range {editor.Minimum}–{editor.Maximum}",12,MutedBrush,new Thickness(0,10,0,7)));
            editor.Input.Text=displayed.Values[editor.ParameterIndex].ToString(CultureInfo.InvariantCulture);
            var actions=new WrapPanel();actions.Children.Add(editor.Input);editor.Apply.Margin=new Thickness(12,0,0,0);actions.Children.Add(editor.Apply);
            effectDetails.Children.Add(actions);editor.Message.Margin=new Thickness(0,7,0,0);effectDetails.Children.Add(editor.Message);
        }
        UpdateAdditionalEditorStates();
    }
    private PresetEffectBlock? EditableBlock(PendingParameterEditor editor)=>editor.Position==1?EditableNoiseKiller():Editable808Tone();
    private void UpdateAdditionalEditorStates()
    {
        foreach(var editor in AdditionalEditors) {
            var block=EditableBlock(editor);
            var supported=!preview && editSnapshotValid && snapshotAddress.HasValue && snapshotFirmware=="V1.1.9" && block is not null;
            editor.Input.IsEnabled=supported && operation is null;
            editor.Apply.IsEnabled=supported && operation is null && editor.TryValue(out var value) && value!=block!.Values[editor.ParameterIndex];
            editor.Message.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
                !supported?"Read a supported V1.1.9 configuration before applying.":!editor.TryValue(out value)?$"Enter a whole number from {editor.Minimum} to {editor.Maximum}.":
                value==block!.Values[editor.ParameterIndex]?"Matches confirmed snapshot · Other values are preserved.":
                $"Pending {value} · Confirmed {block.Values[editor.ParameterIndex]} · Apply reconnects and checks the preset. Nothing is saved.";
        }
    }
    private async Task ApplyAdditionalParameterAsync(PendingParameterEditor editor)
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" || snapshotAddress is not {} target ||
            currentSnapshot is not {} expected || EditableBlock(editor) is not {} block || !editor.TryValue(out var value) || value==block.Values[editor.ParameterIndex])return;
        Begin($"Checking amplifier before applying {editor.Label}…");snapshot.Text="Previous snapshot · Connecting for explicit live edit…";
        try {
            CurrentPresetSnapshot confirmed;
            confirmed=await UseSessionAsync(target,session=>editor.Write(session,expected,value,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            var selected=effectButtons.FirstOrDefault(item=>item.Effect.Source.Position==editor.Position).Effect;
            if(selected is not null)SelectEffect(selected);
            snapshot.Text=SnapshotLabel();status.Text=liveEditUnsaved?$"Live unsaved edit verified · {editor.Label} {value}":
                $"{editor.Label} {value} verified · Matches original snapshot · Nothing saved";
            lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {editSnapshotValid=false;status.Text=$"{editor.Label} edit interrupted · Outcome may be unknown. Read the amplifier before another edit.";snapshot.Text=SnapshotLabel();}
        catch(Exception) {editSnapshotValid=false;status.Text=$"{editor.Label} edit not confirmed. Read the amplifier before another edit. Nothing was saved.";snapshot.Text=SnapshotLabel();}
        finally {status.Foreground=MutedBrush;End();}
    }
}
