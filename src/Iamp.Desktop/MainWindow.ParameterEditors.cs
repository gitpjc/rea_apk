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
        internal byte ProfilePosition {get;}=position;
        internal byte Position {get;set;}=position;
        internal int ParameterIndex {get;}=parameterIndex;
        internal ushort Minimum {get;}=minimum;
        internal ushort Maximum {get;}=maximum;
        internal TextBox Input {get;}=new() {Width=90,MaxLength=maximum.ToString(CultureInfo.InvariantCulture).Length};
        internal Slider Slider {get;set;}=null!;
        internal Button Apply {get;}=MainWindow.Button($"Apply {label} to amp");
        internal TextBlock Message {get;}=Text("",12,MutedBrush);
        internal Func<F15ReadSession,CurrentPresetSnapshot,ushort,CancellationToken,Task<CurrentPresetSnapshot>> Write {get;}=write;
        internal bool TryValue(out ushort value)=>ushort.TryParse(Input.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) && value>=Minimum && value<=Maximum;
    }
    private readonly PendingParameterEditor noiseLevelEditor=new("Level",1,3,0,100,(s,e,v,ct)=>s.SetNoiseKillerLevelAsync(e,v,ct));
    private readonly PendingParameterEditor gainEditor=new("Gain",2,2,0,100,(s,e,v,ct)=>s.Set808GainAsync(e,v,ct));
    private readonly PendingParameterEditor volumeEditor=new("Vol",2,4,0,100,(s,e,v,ct)=>s.Set808VolumeAsync(e,v,ct));
    private readonly PendingParameterEditor ampGainEditor=new("Gain",3,2,0,100,(s,e,v,ct)=>s.SetAmpGainAsync(e,v,ct));
    private readonly PendingParameterEditor ampMasterEditor=new("Master",3,7,0,100,(s,e,v,ct)=>s.SetAmpMasterAsync(e,v,ct));
    private readonly PendingParameterEditor bassEditor=new("Bass",3,3,0,100,(s,e,v,ct)=>s.SetAmpBassAsync(e,v,ct));
    private readonly PendingParameterEditor midEditor=new("Mid",3,4,0,100,(s,e,v,ct)=>s.SetAmpMidAsync(e,v,ct));
    private readonly PendingParameterEditor trebleEditor=new("Treble",3,5,0,100,(s,e,v,ct)=>s.SetAmpTrebleAsync(e,v,ct));
    private readonly PendingParameterEditor presenceEditor=new("Presence",3,6,0,100,(s,e,v,ct)=>s.SetAmpPresenceAsync(e,v,ct));
    private readonly PendingParameterEditor delayLevelEditor=new("Level",4,2,0,100,(s,e,v,ct)=>s.SetDelayLevelAsync(e,v,ct));
    private readonly PendingParameterEditor delayFeedbackEditor=new("Feedback",4,3,0,100,(s,e,v,ct)=>s.SetDelayFeedbackAsync(e,v,ct));
    private readonly PendingParameterEditor delayTimeEditor=new("Time (ms)",4,4,40,2500,(s,e,v,ct)=>s.SetDelayTimeAsync(e,v,ct));
    private readonly PendingParameterEditor reverbPreDelayEditor=new("Pre Delay (ms)",5,2,0,200,(s,e,v,ct)=>s.SetReverbPreDelayAsync(e,v,ct));
    private readonly PendingParameterEditor reverbLevelEditor=new("Level",5,3,0,100,(s,e,v,ct)=>s.SetReverbLevelAsync(e,v,ct));
    private readonly PendingParameterEditor reverbDecayEditor=new("Decay",5,4,0,100,(s,e,v,ct)=>s.SetReverbDecayAsync(e,v,ct));
    private readonly PendingParameterEditor reverbToneEditor=new("Tone",5,5,0,100,(s,e,v,ct)=>s.SetReverbToneAsync(e,v,ct));
    private IEnumerable<PendingParameterEditor> AdditionalEditors=>new[]{noiseLevelEditor,gainEditor,volumeEditor,ampGainEditor,bassEditor,midEditor,trebleEditor,presenceEditor,ampMasterEditor,delayLevelEditor,delayFeedbackEditor,delayTimeEditor,reverbPreDelayEditor,reverbLevelEditor,reverbDecayEditor,reverbToneEditor};

    private void InitializeAdditionalEditors()
    {
        foreach(var editor in AdditionalEditors) {
            editor.Slider=CreateParameterSlider(editor.Input,editor.Label,editor.Minimum,editor.Maximum,()=>ApplyAdditionalParameterAsync(editor));
            editor.Input.TextChanged+=(_,_)=>UpdateAdditionalEditorStates();
            editor.Apply.Click+=async(_,_)=>await ApplyAdditionalParameterAsync(editor);
            System.Windows.Automation.AutomationProperties.SetName(editor.Input,$"Pending {editor.Label}, {editor.Minimum} to {editor.Maximum}");
        }
    }
    private void DetachAdditionalEditorControls()
    {
        foreach(var editor in AdditionalEditors) {
            DetachSlider(editor.Slider);
            if(editor.Input.Parent is Panel inputParent)inputParent.Children.Remove(editor.Input);
            if(editor.Apply.Parent is Panel applyParent)applyParent.Children.Remove(editor.Apply);
        }
    }
    private void AddAdditionalEditors(PresetEffectBlock displayed,bool initializeInputs)
    {
        foreach(var setting in EffectPresentation.From(displayed).Parameters) {
            if(displayed.TypeCode==1 && displayed.Values.Count==5 && displayed.Values[1]==3 && setting.ValueIndex==3) {
                toneInput.Text=PendingParameterText(displayed,3);
                AddMobileParameterSlider(toneSlider,toneInput,"Tone",EffectPresentation.From(displayed).Color);
                var toneActions=new WrapPanel();toneActions.Children.Add(toneInput);applyTone.Margin=new Thickness(12,0,0,0);toneActions.Children.Add(applyTone);
                effectDetails.Children.Add(toneActions);toneMessage.Margin=new Thickness(0,9,0,0);effectDetails.Children.Add(toneMessage);UpdateToneState();continue;
            }
            var editor=AdditionalEditors.FirstOrDefault(e=>MatchesEditorProfile(e,displayed) && e.ParameterIndex==setting.ValueIndex);
            if(editor is null)continue;
            editor.Position=displayed.Position;
            editor.Input.Text=PendingParameterText(displayed,editor.ParameterIndex);
            AddMobileParameterSlider(editor.Slider,editor.Input,editor.Label,EffectPresentation.From(displayed).Color);
            var actions=new WrapPanel();actions.Children.Add(editor.Input);editor.Apply.Margin=new Thickness(12,0,0,0);actions.Children.Add(editor.Apply);
            effectDetails.Children.Add(actions);editor.Message.Margin=new Thickness(0,7,0,0);effectDetails.Children.Add(editor.Message);
        }
        UpdateAdditionalEditorStates();
    }
    private static bool MatchesEditorProfile(PendingParameterEditor editor,PresetEffectBlock block)
        =>editor.ProfilePosition switch {
            1=>block.TypeCode==4 && block.Values.Count==6 && block.Values[1]==2,
            2=>block.TypeCode==1 && block.Values.Count==5 && block.Values[1]==3,
            3=>block.TypeCode==2 && block.Values.Count==9 && block.Values[1]==31,
            4=>block.TypeCode==7 && block.Values.Count==8 && block.Values[1]==1,
            5=>block.TypeCode==8 && block.Values.Count==10 && block.Values[1]==1,
            _=>false
        };
    private PresetEffectBlock? EditableBlock(PendingParameterEditor editor)=>editor.ProfilePosition switch {
        1=>EditableNoiseKiller(),2=>Editable808Tone(),3=>EditableAmpBass(),4=>EditableAnalogDelay(),5=>EditableHallReverb(),_=>null
    };
    private PresetEffectBlock? EditableAmpBass()
    {
        return FindEditableParameterBlock(ObservedParameter.AmpBass);
    }
    private PresetEffectBlock? EditableAnalogDelay()
    {
        return FindEditableParameterBlock(ObservedParameter.DelayLevel);
    }
    private PresetEffectBlock? EditableHallReverb()
    {
        return FindEditableParameterBlock(ObservedParameter.ReverbTone);
    }
    private void UpdateAdditionalEditorStates()
    {
        UpdateEffectPowerState();
        foreach(var editor in AdditionalEditors) {
            var block=EditableBlock(editor);
            if(block is not null)editor.Position=block.Position;
            var supported=!preview && editSnapshotValid && snapshotAddress.HasValue && snapshotFirmware=="V1.1.9" && block is not null;
            editor.Input.IsEnabled=supported && operation is null;
            editor.Apply.IsEnabled=supported && operation is null && editor.TryValue(out var value) && value!=block!.Values[editor.ParameterIndex];
            editor.Message.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
                !supported?"Read a supported V1.1.9 configuration before applying.":!editor.TryValue(out value)?$"Enter a whole number from {editor.Minimum} to {editor.Maximum}.":
                value==block!.Values[editor.ParameterIndex]?"Matches confirmed snapshot · Other values are preserved.":
                $"Pending {value} · Confirmed {block.Values[editor.ParameterIndex]} · Release the slider or choose Apply. Nothing is saved.";
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
            var parameter=Enum.GetValues<ObservedParameter>().Single(p=>ObservedParameterCommands.Definition(p).Position==editor.ProfilePosition && ObservedParameterCommands.Definition(p).ValueIndex==editor.ParameterIndex);
            confirmed=await UseSessionAsync(target,session=>session.SetParameterAtPositionAsync(expected,block.Position,parameter,value,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffectsPreservingPending(confirmed.Blocks);
            var selected=effectButtons.FirstOrDefault(item=>item.Effect.Source.Position==block.Position).Effect;
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
