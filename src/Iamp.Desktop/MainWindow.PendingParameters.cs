using System.Globalization;
using System.Windows.Controls;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly HashSet<byte> initializedParameterPositions=new();

    private IEnumerable<(byte Position,int Index,TextBox Input)> ParameterInputs()
    {
        yield return (1,2,thresholdInput);
        yield return (2,3,toneInput);
        foreach(var editor in AdditionalEditors)yield return (editor.Position,editor.ParameterIndex,editor.Input);
    }

    // Use only for our verified single-parameter write or an unchanged refresh.
    // Fresh presets/imports still initialize their controls from the new snapshot.
    private void SetEffectsPreservingPending(IReadOnlyList<PresetEffectBlock> blocks)
    {
        var inputs=ParameterInputs().Where(p=>initializedParameterPositions.Contains(p.Position))
            .Select(p=>(p.Position,p.Index,p.Input,Text:p.Input.Text)).ToArray();
        SetEffects(blocks);
        foreach(var input in inputs) {
            input.Input.Text=input.Text;
            initializedParameterPositions.Add(input.Position);
        }
        UpdateThresholdState();
    }

    private string ConfirmedParameterText(PresetEffectBlock block,int index)
        =>block.Values[index].ToString(CultureInfo.InvariantCulture);
}
