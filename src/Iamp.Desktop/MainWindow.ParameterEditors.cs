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
        internal EqParameterSpec? EqSpec {get;init;}
        internal ModParameterSpec? ModSpec {get;init;}
        internal string? Unit {get;init;}
        internal bool TryValue(out ushort value)=>EqSpec is {} eq?eq.TryParse(Input.Text,out value):ModSpec is {} mod?mod.TryParse(Input.Text,out value):ushort.TryParse(Input.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) && value>=Minimum && value<=Maximum;
        internal string Format(ushort value)=>EqSpec?.Format(value)??ModSpec?.Format(value)??value.ToString(CultureInfo.InvariantCulture);
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
    private readonly PendingParameterEditor delayTimeEditor=new("Time",4,4,40,2500,(s,e,v,ct)=>s.SetDelayTimeAsync(e,v,ct)){Unit="ms"};
    private readonly PendingParameterEditor reverbPreDelayEditor=new("Pre Delay",5,2,0,200,(s,e,v,ct)=>s.SetReverbPreDelayAsync(e,v,ct)){Unit="ms"};
    private readonly PendingParameterEditor reverbLevelEditor=new("Level",5,3,0,100,(s,e,v,ct)=>s.SetReverbLevelAsync(e,v,ct));
    private readonly PendingParameterEditor reverbDecayEditor=new("Decay",5,4,0,100,(s,e,v,ct)=>s.SetReverbDecayAsync(e,v,ct));
    private readonly PendingParameterEditor reverbToneEditor=new("Tone",5,5,0,100,(s,e,v,ct)=>s.SetReverbToneAsync(e,v,ct));
    private readonly PendingParameterEditor[] dynaEditors=[
        CreateObservedEditor("Sensitivity",ObservedParameter.RedCompSensitivity),CreateObservedEditor("Level",ObservedParameter.RedCompLevel),
        CreateObservedEditor("Attack",ObservedParameter.YellowCompAttack),CreateObservedEditor("Ratio",ObservedParameter.YellowCompRatio),
        CreateObservedEditor("Threshold",ObservedParameter.YellowCompThreshold),CreateObservedEditor("Level",ObservedParameter.YellowCompLevel),
        CreateObservedEditor("Threshold",ObservedParameter.IntelReducerThreshold),CreateObservedEditor("Depth",ObservedParameter.IntelReducerDepth),CreateObservedEditor("Level",ObservedParameter.IntelReducerLevel)];
    private static PendingParameterEditor CreateObservedEditor(string label,ObservedParameter parameter)
    {
        var definition=ObservedParameterCommands.Definition(parameter);
        return new(label,definition.Position,definition.ValueIndex,definition.Minimum,definition.Maximum,
            (s,e,v,ct)=>s.SetParameterAtPositionAsync(e,ObservedParameterCommands.FindSupportedBlock(e.Blocks,parameter)?.Position??
                throw new NotSupportedException("A unique model target is required."),parameter,v,ct)){EqSpec=EqParameters.Get(parameter),ModSpec=ModParameters.Get(parameter),Unit=DelayParameters.Get(parameter)?.Unit??ReverbParameters.Get(parameter)?.Unit??ModParameters.Get(parameter)?.Unit};
    }
    private readonly PendingParameterEditor[] filterEditors=[
        CreateObservedEditor("Attack",ObservedParameter.TouchWahAttack),CreateObservedEditor("Peak",ObservedParameter.TouchWahPeak),
        CreateObservedEditor("Sensitivity",ObservedParameter.TouchWahSensitivity),CreateObservedEditor("Level",ObservedParameter.TouchWahLevel),
        CreateObservedEditor("Rate",ObservedParameter.AutoWahRate),CreateObservedEditor("Peak",ObservedParameter.AutoWahPeak),
        CreateObservedEditor("Range",ObservedParameter.AutoWahRange),CreateObservedEditor("Level",ObservedParameter.AutoWahLevel),
        CreateObservedEditor("Rate",ObservedParameter.QFilterRate),CreateObservedEditor("Q",ObservedParameter.QFilterQ),CreateObservedEditor("Mix",ObservedParameter.QFilterMix)];
    private readonly PendingParameterEditor[] eqEditors=EqParameters.All.Select(s=>CreateObservedEditor(s.Name,s.Parameter)).ToArray();
    private readonly PendingParameterEditor[] delayEditors=DelayParameters.All.Where(s=>s.Profile!=4).Select(s=>CreateObservedEditor(s.Name,s.Parameter)).ToArray();
    private readonly PendingParameterEditor[] reverbEditors=ReverbParameters.All.Where(s=>s.Profile!=5).Select(s=>CreateObservedEditor(s.Name,s.Parameter)).ToArray();
    private readonly PendingParameterEditor[] modEditors=ModParameters.All.Select(s=>CreateObservedEditor(s.Name,s.Parameter)).ToArray();
    private IEnumerable<PendingParameterEditor> AdditionalEditors=>new[]{noiseLevelEditor,gainEditor,volumeEditor,ampGainEditor,bassEditor,midEditor,trebleEditor,presenceEditor,ampMasterEditor,delayLevelEditor,delayFeedbackEditor,delayTimeEditor,reverbPreDelayEditor,reverbLevelEditor,reverbDecayEditor,reverbToneEditor}.Concat(dynaEditors).Concat(filterEditors).Concat(eqEditors).Concat(delayEditors).Concat(reverbEditors).Concat(modEditors);

    private void InitializeAdditionalEditors()
    {
        foreach(var editor in AdditionalEditors) {
            if(editor.EqSpec is {} eq) {
                editor.Input.MaxLength=12;
                editor.Slider=CreateParameterSlider(editor.Input,editor.Label,0,(ushort)(eq.WireValues.Count-1),()=>ApplyAdditionalParameterAsync(editor),
                    index=>eq.Format(eq.WireValues[index]),text=>eq.TryParse(text,out var wire)?(ushort)eq.WireValues.ToList().IndexOf(wire):null);
                editor.Slider.ToolTip=$"{eq.Name} · {eq.Format(eq.WireValues[0])}–{eq.Format(eq.WireValues[^1])} {eq.Unit} · Release to apply. Arrow keys select the next supported step.";
                System.Windows.Automation.AutomationProperties.SetName(editor.Slider,$"Pending {eq.Name}, {eq.Format(eq.WireValues[0])} to {eq.Format(eq.WireValues[^1])} {eq.Unit}");
            }
            else if(editor.ModSpec is {} mod) {
                editor.Input.MaxLength=Math.Max(mod.Format(mod.Minimum).Length,mod.Format(mod.Maximum).Length)+2;
                editor.Slider=CreateParameterSlider(editor.Input,editor.Label,editor.Minimum,editor.Maximum,()=>ApplyAdditionalParameterAsync(editor),mod.Format,text=>mod.TryParse(text,out var wire)?wire:null);
                editor.Slider.ToolTip=$"{mod.Name} · {mod.Format(mod.Minimum)}–{mod.Format(mod.Maximum)} {mod.Unit} · Release to apply.";
                System.Windows.Automation.AutomationProperties.SetName(editor.Slider,$"Pending {mod.Name}, {mod.Format(mod.Minimum)} to {mod.Format(mod.Maximum)} {mod.Unit}");
            }
            else editor.Slider=CreateParameterSlider(editor.Input,editor.Label,editor.Minimum,editor.Maximum,()=>ApplyAdditionalParameterAsync(editor));
            editor.Input.TextChanged+=(_,_)=>UpdateAdditionalEditorStates();
            editor.Apply.Click+=async(_,_)=>await ApplyAdditionalParameterAsync(editor);
            System.Windows.Automation.AutomationProperties.SetName(editor.Input,editor.ModSpec is {} modSpec?$"Pending {editor.Label}, {modSpec.Format(editor.Minimum)} to {modSpec.Format(editor.Maximum)} {modSpec.Unit}":$"Pending {editor.Label}, {editor.Minimum} to {editor.Maximum}");
        }
    }
    private void DetachAdditionalEditorControls()
    {
        foreach(var editor in AdditionalEditors) {
            DetachSlider(editor.Slider);
            if(editor.Input.Parent is Panel inputParent)inputParent.Children.Remove(editor.Input);
            if(editor.Apply.Parent is Panel applyParent)applyParent.Children.Remove(editor.Apply);
            if(editor.Message.Parent is Panel messageParent)messageParent.Children.Remove(editor.Message);
        }
    }
    private void AddAdditionalEditors(PresetEffectBlock displayed,bool initializeInputs)
    {
        Panel host=effectDetails;string? previousGroup=null;
        foreach(var setting in EffectPresentation.From(displayed).Parameters) {
            if(OdParameters.IsKnownLayout(displayed) && setting.ValueIndex==3) {
                toneInput.Text=PendingParameterText(displayed,3);
                AddMobileParameterSlider(toneSlider,toneInput,"Tone",EffectPresentation.From(displayed).Color);
                UpdateToneState();continue;
            }
            var editor=AdditionalEditors.FirstOrDefault(e=>MatchesEditorProfile(e,displayed) && e.ParameterIndex==setting.ValueIndex);
            if(editor is null)continue;
            editor.Position=displayed.Position;
            editor.Input.Text=PendingParameterText(displayed,editor.ParameterIndex);
            var group=editor.EqSpec?.Group;
            if(group!=previousGroup) {
                host=effectDetails;
                if(group is not null) {
                    var content=new StackPanel();
                    effectDetails.Children.Add(new Expander {Header=group,IsExpanded=true,Content=content,Foreground=System.Windows.Media.Brushes.White,Margin=new Thickness(0,12,0,0)});
                    host=content;
                }
                previousGroup=group;
            }
            AddMobileParameterSlider(editor.Slider,editor.Input,editor.Label,EffectPresentation.From(displayed).Color,host,editor.EqSpec?.Unit??editor.Unit);
        }
        UpdateAdditionalEditorStates();
    }
    private static bool MatchesEditorProfile(PendingParameterEditor editor,PresetEffectBlock block)
        =>ObservedParameterCommands.Profile(block)==editor.ProfilePosition;
    private PresetEffectBlock? EditableBlock(PendingParameterEditor editor)=>editor.ProfilePosition switch {
        1=>EditableNoiseKiller(),2=>Editable808Tone(),3=>EditableAmpBass(),4=>EditableAnalogDelay(),5=>EditableHallReverb(),
        6=>FindEditableParameterBlock(ObservedParameter.RedCompSensitivity),7=>FindEditableParameterBlock(ObservedParameter.YellowCompAttack),
        8=>FindEditableParameterBlock(ObservedParameter.IntelReducerThreshold),9=>FindEditableParameterBlock(ObservedParameter.TouchWahAttack),
        10=>FindEditableParameterBlock(ObservedParameter.AutoWahRate),11=>FindEditableParameterBlock(ObservedParameter.QFilterRate),
        12=>FindEditableParameterBlock(ObservedParameter.Eq100Hz),13=>FindEditableParameterBlock(ObservedParameter.CustomEqBand1Gain),_=>DelayParameters.All.FirstOrDefault(s=>s.Profile==editor.ProfilePosition) is {} delay?FindEditableParameterBlock(delay.Parameter):ReverbParameters.All.FirstOrDefault(s=>s.Profile==editor.ProfilePosition) is {} reverb?FindEditableParameterBlock(reverb.Parameter):ModParameters.All.FirstOrDefault(s=>s.Profile==editor.ProfilePosition) is {} mod?FindEditableParameterBlock(mod.Parameter):null
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
                !supported?"Read a supported V1.1.9 configuration before applying.":!editor.TryValue(out value)?editor.EqSpec is {} eq?$"Enter a supported step from {eq.Format(eq.WireValues[0])} to {eq.Format(eq.WireValues[^1])} {eq.Unit}.":editor.ModSpec is {} mod?$"Enter a supported value from {mod.Format(mod.Minimum)} to {mod.Format(mod.Maximum)} {mod.Unit}.":$"Enter a whole number from {editor.Minimum} to {editor.Maximum}.":
                value==block!.Values[editor.ParameterIndex]?"Matches confirmed snapshot · Other values are preserved.":
                $"Pending {editor.Format(value)} · Confirmed {editor.Format(block.Values[editor.ParameterIndex])} · Release the slider or choose Apply. Nothing is saved.";
        }
    }
    private async Task ApplyAdditionalParameterAsync(PendingParameterEditor editor)
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" || snapshotAddress is not {} target ||
            currentSnapshot is not {} expected || EditableBlock(editor) is not {} block || !editor.TryValue(out var value) || value==block.Values[editor.ParameterIndex])return;
        BeginSteadyOperation($"Checking amplifier before applying {editor.Label}…");snapshot.Text="Previous snapshot · Connecting for explicit live edit…";
        try {
            CurrentPresetSnapshot confirmed;
            var parameter=Enum.GetValues<ObservedParameter>().Single(p=>ObservedParameterCommands.Definition(p).Position==editor.ProfilePosition && ObservedParameterCommands.Definition(p).ValueIndex==editor.ParameterIndex);
            confirmed=await UseSessionAsync(target,session=>session.SetParameterAtPositionAsync(expected,block.Position,parameter,value,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffectsPreservingPending(confirmed.Blocks);
            var selected=effectButtons.FirstOrDefault(item=>item.Effect.Source.Position==block.Position).Effect;
            if(selected is not null)SelectEffect(selected);
            snapshot.Text=SnapshotLabel();status.Text=liveEditUnsaved?$"Live unsaved edit verified · {editor.Label} {editor.Format(value)}":
                $"{editor.Label} {editor.Format(value)} verified · Matches original snapshot · Nothing saved";
            lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {editSnapshotValid=false;status.Text=$"{editor.Label} edit interrupted · Outcome may be unknown. Read the amplifier before another edit.";snapshot.Text=SnapshotLabel();}
        catch(Exception) {editSnapshotValid=false;status.Text=$"{editor.Label} edit not confirmed. Read the amplifier before another edit. Nothing was saved.";snapshot.Text=SnapshotLabel();}
        finally {status.Foreground=MutedBrush;End();}
    }
}
