using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private Button? effectPowerButton;
    private EffectPresentation? displayedEffect;

    private static UIElement EffectButtonContent(EffectPresentation effect)
    {
        var content=new StackPanel {HorizontalAlignment=HorizontalAlignment.Center};
        var color=effect.Enabled==false?MutedBrush:Brush(effect.Color);
        var file=System.IO.Path.Combine(AppContext.BaseDirectory,"assets","effects",effect.Category.ToLowerInvariant()+".png");
        if(File.Exists(file)) {
            var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;
            bitmap.UriSource=new Uri(file);bitmap.EndInit();bitmap.Freeze();
            content.Children.Add(new Rectangle {Width=32,Height=32,Fill=color,Margin=new Thickness(0,0,0,5),
                OpacityMask=new ImageBrush(bitmap) {Stretch=Stretch.Uniform}});
        }
        content.Children.Add(Text(effect.Category,12,color));
        if(effect.Enabled is null)content.Children.Add(Text("State unknown",10,MutedBrush,new Thickness(0,3,0,0)));
        foreach(var label in content.Children.OfType<TextBlock>()) {
            label.HorizontalAlignment=HorizontalAlignment.Center;label.TextAlignment=TextAlignment.Center;
        }
        return content;
    }
    private void AddEffectPowerControl(EffectPresentation effect)
    {
        displayedEffect=effect;
        var row=new WrapPanel {VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,4)};
        var state=Text(effect.Enabled switch {true=>"Enabled",false=>"Disabled",_=>"Enabled state not verified for this model"},13,
            effect.Enabled switch {true=>BlueBrush,false=>Brush("#FF5C5C"),_=>MutedBrush},new Thickness(0,0,14,0));state.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(state);
        effectPowerButton=Button(effect.Enabled==false?"Enable":"Disable");
        System.Windows.Automation.AutomationProperties.SetName(effectPowerButton,$"{(effect.Enabled==false?"Enable effect":"Disable effect")}: {effect.Category} {effect.Title}");
        effectPowerButton.Click+=async(_,_)=>await ToggleEffectAsync(effect.Source.Position);
        row.Children.Add(effectPowerButton);effectHeading.Children.Add(row);UpdateEffectPowerState();
    }
    private void UpdateEffectPowerState()
    {
        UpdateChainControls();
        if(effectPowerButton is null)return;
        effectPowerButton.IsEnabled=!preview && operation is null && !polling && editSnapshotValid && snapshotAddress.HasValue &&
            snapshotFirmware=="V1.1.9" && displayedEffect?.Enabled is not null && !HasPendingInputs();
        effectPowerButton.ToolTip=HasPendingInputs()?"Apply or clear pending parameter inputs before switching an effect.":"Live on/off change · Nothing is saved";
    }
    private async Task ToggleEffectAsync(byte position)
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" || snapshotAddress is not {} target ||
            currentSnapshot is not {} expected || HasPendingInputs() || position is <1 or >20 ||
            !EffectBypassCommand.TryGetEnabled(expected.Blocks[position-1],out var enabled))return;
        Begin($"Checking amplifier before {(enabled?"disabling":"enabling")} effect…");
        try {
            var confirmed=await UseSessionAsync(target,s=>s.SetEffectEnabledAsync(expected,position,!enabled,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            var selected=effectButtons.FirstOrDefault(e=>e.Effect.Source.Position==position).Effect;
            if(selected is not null)SelectEffect(selected);
            snapshot.Text=SnapshotLabel();status.Text=$"Effect {(!enabled?"enabled":"disabled")} verified · Nothing saved";lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {editSnapshotValid=false;status.Text="Effect switch interrupted · Outcome may be unknown. Refresh before another action.";}
        catch(Exception) {editSnapshotValid=false;status.Text="Effect switch not confirmed. Refresh before another action. Nothing was saved.";}
        finally {status.Foreground=MutedBrush;End();}
    }
}
