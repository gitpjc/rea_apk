using System.Globalization;
using System.Windows.Controls;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly HashSet<byte> initializedParameterPositions=new();
    private readonly Dictionary<(byte Position,int Index),string> pendingParameterValues=new();
    private readonly Dictionary<byte,byte> parameterTargets=new();
    private bool restoringParameterInputs;
    private void AssignParameterTarget(PresetEffectBlock block)
    {
        var profile=(block.TypeCode,block.Values.Count,block.Values.Count>1?block.Values[1]:-1) switch {
            (4,6,2)=>1,(1,5,3)=>2,(2,9,31)=>3,(7,8,1)=>4,(8,10,1)=>5,_=>0
        };
        if(profile!=0)parameterTargets[(byte)profile]=block.Position;
    }
    private void CaptureParameterInputs()
    {
        if(restoringParameterInputs)return;
        foreach(var input in ParameterInputs().Where(p=>initializedParameterPositions.Contains(p.Position)))
            pendingParameterValues[(input.Position,input.Index)]=input.Input.Text;
    }
    private string PendingParameterText(PresetEffectBlock block,int index)
        =>pendingParameterValues.TryGetValue((block.Position,index),out var value)?value:ConfirmedParameterText(block,index);

    private IEnumerable<(byte Position,int Index,TextBox Input)> ParameterInputs()
    {
        if(EditableNoiseKiller() is {} noise)yield return (noise.Position,2,thresholdInput);
        if(Editable808Tone() is {} od)yield return (od.Position,3,toneInput);
        foreach(var editor in AdditionalEditors)
            if(EditableBlock(editor) is {} block)yield return (block.Position,editor.ParameterIndex,editor.Input);
    }

    // Use only for our verified single-parameter write or an unchanged refresh.
    // Fresh presets/imports still initialize their controls from the new snapshot.
    private void SetEffectsPreservingPending(IReadOnlyList<PresetEffectBlock> blocks)
    {
        CaptureParameterInputs();
        var saved=pendingParameterValues.ToArray();var targets=parameterTargets.ToArray();var positions=initializedParameterPositions.ToArray();
        var selected=displayedEffect?.Source.Position;
        SetEffects(blocks);
        pendingParameterValues.Clear();foreach(var entry in saved)pendingParameterValues[entry.Key]=entry.Value;
        parameterTargets.Clear();foreach(var entry in targets)parameterTargets[entry.Key]=entry.Value;
        initializedParameterPositions.Clear();foreach(var position in positions)initializedParameterPositions.Add(position);
        restoringParameterInputs=true;
        try {
            // Reload each shared editor from its own last-selected module before
            // presenting the active module. Identical models never share inputs.
            foreach(var item in effectButtons.Where(e=>targets.Any(t=>t.Value==e.Effect.Source.Position)).ToArray())SelectEffect(item.Effect);
            var effect=effectButtons.FirstOrDefault(e=>e.Effect.Source.Position==selected).Effect;
            if(effect is not null)SelectEffect(effect);
        }
        finally {restoringParameterInputs=false;}
        UpdateThresholdState();
    }

    private string ConfirmedParameterText(PresetEffectBlock block,int index)
        =>block.Values[index].ToString(CultureInfo.InvariantCulture);
}
