using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button showPresetJson=Button("Show preset JSON");
    private void ShowPresetJson()
    {
        if(!showPresetJson.IsEnabled || currentSnapshot is not {} preset)return;
        var pending=HasPendingInputs();
        var json=PresetJsonDocument.Create(preset,snapshotFirmware,snapshotTime,localSnapshotSource?"localSnapshot":"amplifierSnapshot",editSnapshotValid,pending);
        var dialog=CreatePresetJsonDialog(json,PresetJsonDocument.DefaultFileName(preset),pending);
        dialog.Owner=this;dialog.ShowDialog();
    }
    private Window CreatePresetJsonDialog(string json,string fileName,bool pending)
    {
        var dialog=new Window {Title="Preset JSON",Width=800,Height=650,MinWidth=440,MinHeight=340,Background=PanelBrush,Foreground=Brushes.White,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var root=new DockPanel {Margin=new Thickness(18)};
        var top=new StackPanel();top.Children.Add(Text("Preset JSON",22));
        var note=Text(pending?"Latest snapshot settings · Unapplied input values are not included.":"Settings from the displayed preset snapshot · Effect and setting order match the editor. Export on the Presets screen saves the complete file.",12,MutedBrush,new Thickness(0,8,0,12));note.TextWrapping=TextWrapping.Wrap;top.Children.Add(note);
        DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        var bottom=new StackPanel {Margin=new Thickness(0,12,0,0)};var feedback=Text("",12,MutedBrush);feedback.Name="PresetJsonFeedback";feedback.TextWrapping=TextWrapping.Wrap;
        var actions=new WrapPanel();var copy=Button("");copy.Name="CopyPresetJson";copy.ToolTip="Copy all JSON to clipboard";
        var copyContent=new StackPanel {Orientation=Orientation.Horizontal};
        var icon=new Grid {Width=18,Height=18,Margin=new Thickness(0,0,8,0)};
        icon.Children.Add(new Border {BorderBrush=Brushes.White,BorderThickness=new Thickness(1.5),Margin=new Thickness(0,0,5,5)});
        icon.Children.Add(new Border {BorderBrush=Brushes.White,BorderThickness=new Thickness(1.5),Background=PanelBrush,Margin=new Thickness(5,5,0,0)});
        copyContent.Children.Add(icon);copyContent.Children.Add(Text("Copy JSON",13));copy.Content=copyContent;
        System.Windows.Automation.AutomationProperties.SetName(copy,"Copy preset JSON to clipboard");
        copy.Click+=(_,_)=>CopyPresetJson(json,Clipboard.SetText,feedback);
        var close=Button("Close");close.IsCancel=true;close.Margin=new Thickness(10,0,0,0);close.Click+=(_,_)=>dialog.Close();
        actions.Children.Add(copy);actions.Children.Add(close);bottom.Children.Add(actions);bottom.Children.Add(feedback);DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
        var lines=new ListView {Name="PresetJsonLines",ItemsSource=json.Split('\n'),Background=BackgroundBrush,Foreground=Brushes.White,FontFamily=new FontFamily("Consolas"),FontSize=13,SelectionMode=SelectionMode.Extended};
        var rowStyle=new Style(typeof(ListViewItem));rowStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(4,0,4,0)));rowStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Left));lines.ItemContainerStyle=rowStyle;
        ScrollViewer.SetVerticalScrollBarVisibility(lines,ScrollBarVisibility.Auto);ScrollViewer.SetHorizontalScrollBarVisibility(lines,ScrollBarVisibility.Auto);
        VirtualizingPanel.SetIsVirtualizing(lines,true);System.Windows.Automation.AutomationProperties.SetName(lines,"Preset JSON lines");root.Children.Add(lines);
        dialog.Content=root;return dialog;
    }
    private static void CopyPresetJson(string json,Action<string> writeClipboard,TextBlock feedback)
    {
        try {writeClipboard(json);feedback.Text="JSON copied to clipboard.";}
        catch(ExternalException) {feedback.Text="Clipboard is busy. Try copying again.";}
    }
}
