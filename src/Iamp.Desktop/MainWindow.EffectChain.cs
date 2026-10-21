using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button addChainEffect=Button("+"),moveEffectLeft=Button("← Move left"),moveEffectRight=Button("Move right →"),removeChainEffect=Button("Remove"),changeEffectModel=Button("Change");
    private bool chainControlsWired;
    private bool chainPickerOpen;
    private EffectDrag? chainDrag;
    private readonly Border chainInsertionLine=new() {Width=3,Background=Brush("#4385ff"),Visibility=Visibility.Collapsed,IsHitTestVisible=false};
    private UIElement CreateChainDragSurface()
    {
        var host=new Grid();host.Children.Add(effectChain);
        var overlay=new Canvas {IsHitTestVisible=false};overlay.Children.Add(chainInsertionLine);host.Children.Add(overlay);
        effectChain.Background=Brushes.Transparent;effectChain.AllowDrop=true;
        effectChain.PreviewDragOver+=(_,e)=> {
            e.Handled=true;
            if(IsCurrentChainDrag(e.Data)) {ShowChainInsertion(e.GetPosition(effectChain));e.Effects=DragDropEffects.Move;}
            else {HideChainInsertion();e.Effects=DragDropEffects.None;}
        };
        effectChain.PreviewDrop+=async(_,e)=> {
            e.Handled=true;var drag=chainDrag;
            var destination=IsCurrentChainDrag(e.Data)?ShowChainInsertion(e.GetPosition(effectChain)):(byte?)null;
            HideChainInsertion();
            if(drag is not null && destination is {} to && drag.Position!=to)
                await ChangeEffectChainAsync("Moving effect",drag.Position,to,null);
        };
        effectChain.DragLeave+=(_,e)=> {
            var p=e.GetPosition(effectChain);
            if(p.X<0 || p.Y<0 || p.X>effectChain.ActualWidth || p.Y>effectChain.ActualHeight)HideChainInsertion();
        };
        return host;
    }
    private const string ChainDragFormat="Iamp.Desktop.EffectChain";
    private bool IsCurrentChainDrag(IDataObject data)=>chainDrag is {} drag && data.GetData(ChainDragFormat) is string id && id==drag.Id &&
        ReferenceEquals(drag.Snapshot,currentSnapshot) && CanEditChain;
    private void HideChainInsertion()=>chainInsertionLine.Visibility=Visibility.Collapsed;
    private byte? ShowChainInsertion(Point pointer)
    {
        if(chainDrag is not {} drag || !CanEditChain || !ReferenceEquals(drag.Snapshot,currentSnapshot) || effectButtons.Count==0) {HideChainInsertion();return null;}
        var rectangles=effectButtons.Select(item=>new Rect(item.Button.TranslatePoint(new Point(),effectChain),item.Button.RenderSize)).ToArray();
        var insertion=FindChainInsertion(rectangles,pointer,drag.Position);
        Canvas.SetLeft(chainInsertionLine,Math.Max(0,insertion.X));Canvas.SetTop(chainInsertionLine,Math.Max(0,insertion.Top-2));
        chainInsertionLine.Height=insertion.Height+4;chainInsertionLine.Visibility=Visibility.Visible;
        return insertion.Destination;
    }
    // A gap is resolved before removing the source; adjust once when it lies to its right.
    private static (byte Destination,double X,double Top,double Height) FindChainInsertion(Rect[] rectangles,Point pointer,byte source)
    {
        if(rectangles.Length==0 || source<1 || source>rectangles.Length)throw new ArgumentOutOfRangeException(nameof(source));
        var nearest=rectangles.MinBy(r=>pointer.Y<r.Top?r.Top-pointer.Y:pointer.Y>r.Bottom?pointer.Y-r.Bottom:0);
        var row=Enumerable.Range(0,rectangles.Length).Where(i=>Math.Abs(rectangles[i].Top-nearest.Top)<1).ToArray();
        var gap=row.FirstOrDefault(i=>pointer.X<rectangles[i].Left+rectangles[i].Width/2,-1);
        var anchor=gap>=0?rectangles[gap]:rectangles[row[^1]];
        var x=gap>=0?anchor.Left-5:anchor.Right+4;
        if(gap<0)gap=row[^1]+1;
        var destination=gap>source-1?gap:gap+1;
        return ((byte)destination,x,anchor.Top,anchor.Height);
    }
    // Background reads share the transport workflow queue with edits; they are
    // not user operations and must not flicker or swallow chain interactions.
    private bool CanEditChain=>!preview && operation is null && editSnapshotValid && snapshotAddress.HasValue &&
        snapshotFirmware=="V1.1.9" && currentSnapshot is not null && !HasPendingInputs() && SupportedChain();
    private bool SupportedChain()
    {try {return currentSnapshot is not null && EffectChainCommands.Active(currentSnapshot.Blocks).Count<=20;}catch(NotSupportedException){return false;}}
    private void AddChainControls()
    {
        if(!chainControlsWired) {
            chainControlsWired=true;addChainEffect.FontSize=28;addChainEffect.MinWidth=70;addChainEffect.Foreground=Brush("#4385ff");
            addChainEffect.Margin=new Thickness(0,0,0,8);addChainEffect.ToolTip="Add an effect";
            System.Windows.Automation.AutomationProperties.SetName(addChainEffect,"Add effect");
            addChainEffect.Click+=(_,_)=>ShowAddEffectPicker();
            moveEffectLeft.Click+=async(_,_)=>await MoveDisplayedEffectAsync(-1);
            moveEffectRight.Click+=async(_,_)=>await MoveDisplayedEffectAsync(1);
            removeChainEffect.Click+=async(_,_)=> {if(displayedEffect is {} e)await ChangeEffectChainAsync("Removing effect",e.Source.Position,null,null);};
            changeEffectModel.Click+=(_,_)=>ShowEffectModelPicker();
        }
        effectChain.Children.Add(addChainEffect);UpdateChainControls();
    }
    private void AddChainSelectionActions()
    {
        foreach(var b in new[]{changeEffectModel,moveEffectLeft,moveEffectRight,removeChainEffect})if(b.Parent is Panel parent)parent.Children.Remove(b);
        effectHeading.Margin=new Thickness(0,0,0,8);
        foreach(var b in new[]{changeEffectModel,moveEffectLeft,moveEffectRight,removeChainEffect}){
            b.Margin=new Thickness(0,0,8,4);b.VerticalAlignment=VerticalAlignment.Center;effectHeading.Children.Add(b);
        }
        UpdateChainControls();
    }
    private void UpdateChainControls()
    {
        var enabled=CanEditChain;var count=currentSnapshot?.Blocks.Count(b=>b.TypeCode!=0)??0;
        addChainEffect.IsEnabled=enabled;
        var pos=displayedEffect?.Source.Position??0;
        moveEffectLeft.IsEnabled=enabled && pos>1;moveEffectRight.IsEnabled=enabled && pos>0 && pos<count;
        removeChainEffect.IsEnabled=enabled && pos>0;
        changeEffectModel.IsEnabled=enabled && pos>0;
        foreach(var b in new[]{changeEffectModel,moveEffectLeft,moveEffectRight,removeChainEffect})b.ToolTip=HasPendingInputs()?"Apply or clear pending parameter inputs before changing the chain.":"Live chain change · Nothing is saved";
    }
    private async Task MoveDisplayedEffectAsync(int direction)
    {
        if(displayedEffect is {} effect && CanEditChain) {
            var to=effect.Source.Position+direction;
            if(to>=1 && to<=effectButtons.Count)await ChangeEffectChainAsync("Moving effect",effect.Source.Position,(byte)to,null);
        }
    }
    private sealed record EffectDrag(string Id,CurrentPresetSnapshot Snapshot,byte Position);
    private void EnableEffectDragging(Button button,EffectPresentation effect)
    {
        Point? start=null;
        button.PreviewMouseLeftButtonDown+=(_,e)=>start=e.GetPosition(button);
        button.PreviewMouseMove+=(_,e)=> {
            if(e.LeftButton!=MouseButtonState.Pressed || start is not {} origin || !CanEditChain || currentSnapshot is not {} snapshot ||
                !effectButtons.Any(item=>ReferenceEquals(item.Button,button)))return;
            var point=e.GetPosition(button);
            if(Math.Abs(point.X-origin.X)<SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y-origin.Y)<SystemParameters.MinimumVerticalDragDistance)return;
            start=null;e.Handled=true;
            chainDrag=new EffectDrag(Guid.NewGuid().ToString("N"),snapshot,effect.Source.Position);
            try {DragDrop.DoDragDrop(button,new DataObject(ChainDragFormat,chainDrag.Id),DragDropEffects.Move);}
            finally {chainDrag=null;HideChainInsertion();}
        };
        button.ToolTip+=" · Drag to reorder";
    }
    private void ShowAddEffectPicker()
    {
        if(!CanEditChain || currentSnapshot is not {} starting)return;
        var picker=CreateAddEffectPicker(starting);picker.Owner=this;chainPickerOpen=true;
        try {picker.ShowDialog();}finally {chainPickerOpen=false;}
    }
    private void ShowEffectModelPicker()
    {
        if(!CanEditChain || currentSnapshot is not {} starting || displayedEffect is not {} effect)return;
        var picker=CreateEffectModelPicker(starting,effect.Source.Position);picker.Owner=this;chainPickerOpen=true;
        try {picker.ShowDialog();}finally {chainPickerOpen=false;}
    }
    private sealed record ChainCategory(string Name,byte? TypeCode);
    private static readonly ChainCategory[] ChainCategories=[new("Dyna",4),new("Filter",5),new("OD",1),new("Amp",2),
        new("GIR&IR",null),new("EQ",9),new("Mod",6),new("Delay",7),new("Reverb",8)];
    private Window CreateAddEffectPicker(CurrentPresetSnapshot starting)
        =>CreateEffectModelPicker(starting,null);
    private Window CreateEffectModelPicker(CurrentPresetSnapshot starting,byte? replacementPosition)
    {
        var replacing=replacementPosition.HasValue;
        var original=replacing?starting.Blocks.Single(b=>b.Position==replacementPosition):null;
        var picker=new Window {Title=replacing?"Change effect model":"Add effect",Width=680,Height=620,MinWidth=460,MinHeight=420,
            Background=PanelBrush,Foreground=Brushes.White,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var root=new DockPanel {Margin=new Thickness(20)};
        var top=new StackPanel();top.Children.Add(Text(replacing?"Change effect model":"Add an effect",22));
        top.Children.Add(Text("Choose an effect category and model.",13,MutedBrush,new Thickness(0,8,0,12)));
        top.Children.Add(Text("Search models",12,MutedBrush));
        var searchBox=new TextBox {Name="EffectModelSearch",Margin=new Thickness(0,4,0,10),Padding=new Thickness(8),Background=BackgroundBrush,Foreground=Brushes.White};
        System.Windows.Automation.AutomationProperties.SetName(searchBox,"Search effect models");top.Children.Add(searchBox);
        DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        var bottom=new StackPanel();var message=Text("",12,MutedBrush,new Thickness(0,10,0,10));message.Name="EffectPickerMessage";message.TextWrapping=TextWrapping.Wrap;
        bottom.Children.Add(message);var buttons=new WrapPanel();var add=Button(replacing?"Change model":"Add effect");add.Name=replacing?"ConfirmReplaceEffect":"ConfirmAddEffect";var close=Button("Cancel");close.Margin=new Thickness(10,0,0,0);
        buttons.Children.Add(add);buttons.Children.Add(close);bottom.Children.Add(buttons);DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
        var body=new Grid();body.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(135)});body.ColumnDefinitions.Add(new ColumnDefinition());root.Children.Add(body);
        var categories=new ListBox {Name="EffectCategories",ItemsSource=ChainCategories,DisplayMemberPath="Name",Background=PanelBrush,Foreground=Brushes.White,Margin=new Thickness(0,0,12,0)};
        var itemStyle=new Style(typeof(ListBoxItem));itemStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(10,12,10,12)));categories.ItemContainerStyle=itemStyle;
        System.Windows.Automation.AutomationProperties.SetName(categories,"Effect categories");body.Children.Add(categories);
        var list=new ListBox {Name="EffectModels",Background=BackgroundBrush,Foreground=Brushes.White,DisplayMemberPath="Name"};Grid.SetColumn(list,1);body.Children.Add(list);
        bool IsOriginal(EffectDefinition choice)=>original is not null && original.TypeCode==choice.TypeCode && original.Values[1]==choice.Model;
        void UpdateAdd()=>add.IsEnabled=list.SelectedItem is EffectDefinition selected && !IsOriginal(selected) &&
            (replacing || starting.Blocks.Count(b=>b.TypeCode!=0)<20) && CanEditChain && ReferenceEquals(currentSnapshot,starting);
        void FilterModels() {
            var category=categories.SelectedItem as ChainCategory;
            list.ItemsSource=PresetEffectSearch.Catalog.Where(e=>e.TypeCode==category?.TypeCode && e.Name.Contains(searchBox.Text.Trim(),StringComparison.OrdinalIgnoreCase)).ToArray();
            list.Foreground=Brushes.White;
            message.Text=category?.TypeCode is null?"GIR&IR library access is not available in this desktop version.":replacing?
                "Replaces this effect in the same position using the amplifier’s default values. Nothing is saved.":starting.Blocks.Count(b=>b.TypeCode!=0)>=20?
                "The effect chain has no empty slots.":
                "Adds to the end of the chain using the amplifier’s default values. Nothing is saved.";
            UpdateAdd();
        }
        categories.SelectionChanged+=(_,_)=>FilterModels();searchBox.TextChanged+=(_,_)=>FilterModels();list.SelectionChanged+=(_,_)=>UpdateAdd();
        close.Click+=(_,_)=>picker.Close();add.Click+=async(_,_)=> {
            if(!add.IsEnabled || list.SelectedItem is not EffectDefinition choice)return;
            if(!CanEditChain || !ReferenceEquals(currentSnapshot,starting)) {message.Text="The amplifier view changed. Close this picker and refresh before changing an effect.";UpdateAdd();return;}
            picker.Close();
            if(ReferenceEquals(currentSnapshot,starting)) {
                if(replacementPosition is {} position)await ReplaceEffectModelAsync(position,choice);
                else await ChangeEffectChainAsync("Adding effect",0,null,choice);
            }
        };
        picker.Content=root;categories.SelectedIndex=original is not null?Math.Max(0,Array.FindIndex(ChainCategories,c=>c.TypeCode==original.TypeCode)):0;
        if(original is not null)list.SelectedItem=list.Items.Cast<EffectDefinition>().FirstOrDefault(e=>IsOriginal(e));
        return picker;
    }
    private async Task ReplaceEffectModelAsync(byte position,EffectDefinition choice)
    {
        lastMutationSucceeded=false;
        if(!CanEditChain || currentSnapshot is not {} expected || snapshotAddress is not {} target)return;
        var original=expected.Blocks.FirstOrDefault(b=>b.Position==position && b.TypeCode!=0);
        if(original is null || (original.TypeCode==choice.TypeCode && original.Values[1]==choice.Model))return;
        BeginSteadyOperation("Changing effect model: checking the current amplifier chain…");
        try {
            var confirmed=await UseSessionAsync(target,s=>s.ReplaceEffectAsync(expected,position,choice,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            SetEffects(confirmed.Blocks);SelectChangedChainEffect(position,position,null);Filter();
            snapshot.Text=SnapshotLabel();status.Text="Effect model verified · Default values loaded · Nothing saved";lastMutationSucceeded=true;
        }
        catch(EffectChainReadbackException ex) {ShowUnconfirmedChain(ex.ActualSnapshot,position,position,null);}
        catch(OperationCanceledException) {editSnapshotValid=false;status.Text="Model change interrupted. Read the amplifier before another action; the outcome may be unknown.";}
        catch(Exception) {editSnapshotValid=false;status.Text="Model change not confirmed. Read the amplifier before another action. Nothing was saved.";}
        finally {status.Foreground=MutedBrush;End();}
    }
    private async Task ChangeEffectChainAsync(string action,byte position,byte? destination,EffectDefinition? addition)
    {
        lastMutationSucceeded=false;
        if(!CanEditChain || currentSnapshot is not {} expected || snapshotAddress is not {} target)return;
        if(destination==position)return;
        BeginSteadyOperation($"{action}: checking the current amplifier chain…");
        try {
            var confirmed=await UseSessionAsync(target,s=>addition is not null?s.AddEffectAsync(expected,addition,operation!.Token):
                destination is {} to?s.MoveEffectAsync(expected,position,to,operation!.Token):s.RemoveEffectAsync(expected,position,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            SetEffects(confirmed.Blocks);
            SelectChangedChainEffect(position,destination,addition);
            Filter();
            snapshot.Text=SnapshotLabel();status.Text="Effect chain verified · Nothing saved";lastMutationSucceeded=true;
        }
        catch(EffectChainReadbackException ex) {
            ShowUnconfirmedChain(ex.ActualSnapshot,position,destination,addition);
        }
        catch(OperationCanceledException) {editSnapshotValid=false;status.Text="Chain change interrupted. Read the amplifier before another action; the outcome may be unknown.";}
        catch(Exception) {editSnapshotValid=false;status.Text="Chain change not confirmed. Read the amplifier before another action. Nothing was saved.";}
        finally {status.Foreground=MutedBrush;End();}
    }
    private void ShowUnconfirmedChain(CurrentPresetSnapshot actual,byte position,byte? destination,EffectDefinition? addition)
    {
        currentSnapshot=actual;editSnapshotValid=false;liveEditUnsaved=actual.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
        currentPreset.Text=$"{actual.SlotLabel} · {actual.Name}";
        SetEffects(actual.Blocks);SelectChangedChainEffect(position,destination,addition);Filter();snapshot.Text=SnapshotLabel();
        status.Text="Current amplifier chain shown · Requested change not confirmed. Refresh before another edit. Nothing saved.";
    }
    private void SelectChangedChainEffect(byte position,byte? destination,EffectDefinition? addition)
    {
        var selectedPosition=addition is not null?effectButtons.Count:destination??Math.Min(position,effectButtons.Count);
        var selected=effectButtons.FirstOrDefault(e=>e.Effect.Source.Position==selectedPosition).Effect;if(selected is not null)SelectEffect(selected);
    }
}
