using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private sealed record PresetSaveChoice(byte Destination,string Name);
    private Window CreatePresetRecallDialog(PresetName selected)
    {
        var dialog=new Window {Title="Recall preset",Owner=IsVisible?this:null,Width=540,Height=310,MinWidth=420,MinHeight=280,
            WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BackgroundBrush,Foreground=Brushes.White,FontFamily=FontFamily,FontSize=14};
        NameScope.SetNameScope(dialog,new NameScope());
        var body=new StackPanel();body.Children.Add(Text("Recall preset",24));
        body.Children.Add(Text("Discard live unsaved edits and pending inputs, and recall the selected preset?",14,MutedBrush,new Thickness(0,14,0,16)));
        body.Children.Add(Text($"{selected.SlotLabel} · {selected.Name}",18));
        var actions=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0)};
        var cancelButton=Button("Cancel");cancelButton.IsCancel=true;cancelButton.IsDefault=true;cancelButton.Click+=(_,_)=>dialog.DialogResult=false;
        var recallButton=Button("Discard and recall");recallButton.Margin=new Thickness(10,0,0,0);recallButton.Background=BlueBrush;recallButton.Foreground=Brush("#101C2B");
        recallButton.Click+=(_,_)=>dialog.DialogResult=true;
        AutomationProperties.SetName(recallButton,"Discard unsaved changes and recall selected preset");
        dialog.RegisterName("PresetRecallConfirm",recallButton);dialog.RegisterName("PresetRecallCancel",cancelButton);
        actions.Children.Add(cancelButton);actions.Children.Add(recallButton);
        var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        layout.Children.Add(new ScrollViewer {Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        Grid.SetRow(actions,1);layout.Children.Add(actions);dialog.Content=new Border {Background=BackgroundBrush,Padding=new Thickness(24),Child=layout};
        dialog.Loaded+=(_,_)=>cancelButton.Focus();return dialog;
    }
    private sealed record SaveDestinationOption(byte WireIndex,string Name)
    {
        public string DisplayName=>$"{PresetAddress.FromWireIndex(WireIndex).Label} · {Name}";
    }

    private Window CreatePresetSaveDialog(CurrentPresetSnapshot expected)
    {
        var dialog=new Window {Title="Save or rename preset",Owner=IsVisible?this:null,Width=580,Height=460,MinWidth=420,MinHeight=340,
            WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BackgroundBrush,Foreground=Brushes.White,FontFamily=FontFamily,FontSize=14};
        NameScope.SetNameScope(dialog,new NameScope());
        var body=new StackPanel();body.Children.Add(Text("Save or rename preset",24));
        body.Children.Add(Text($"Current amplifier settings from {expected.SlotLabel} · {expected.Name}",13,MutedBrush,new Thickness(0,8,0,18)));
        body.Children.Add(Text("Preset name",13));
        var input=new TextBox {Text=expected.Name,MaxLength=PresetSaveCommand.MaximumNameLength,Background=PanelBrush,Foreground=Brushes.White,
            BorderBrush=Brush("#46505E"),CaretBrush=Brushes.White,Padding=new Thickness(9),Margin=new Thickness(0,6,0,0)};
        dialog.RegisterName("PresetSaveName",input);AutomationProperties.SetName(input,"Preset name, up to 20 characters");body.Children.Add(input);
        body.Children.Add(Text("Up to 20 characters: English letters, numbers, spaces and punctuation.",12,MutedBrush,new Thickness(0,5,0,14)));
        body.Children.Add(Text("Save to preset",13));
        var options=names.Select(n=>new SaveDestinationOption((byte)n.WireIndex,n.Name)).OrderBy(n=>n.WireIndex).ToArray();
        if(options.All(n=>n.WireIndex!=expected.WireIndex))options=options.Append(new SaveDestinationOption((byte)expected.WireIndex,expected.Name)).OrderBy(n=>n.WireIndex).ToArray();
        var destination=new ComboBox {ItemsSource=options,SelectedItem=options.First(n=>n.WireIndex==expected.WireIndex),MinHeight=38,MaxDropDownHeight=300,
            Background=PanelBrush,Foreground=Brushes.White,HorizontalContentAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,6,0,0),
            Template=effectFilter.Template,ItemContainerStyle=presets.ItemContainerStyle,
            ItemTemplate=(DataTemplate)XamlReader.Parse("""<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><TextBlock Text="{Binding DisplayName}" Margin="8,6"/></DataTemplate>""")};
        TextSearch.SetTextPath(destination,"DisplayName");dialog.RegisterName("PresetSaveDestination",destination);AutomationProperties.SetName(destination,"Preset save destination");body.Children.Add(destination);
        var note=Text("",12,MutedBrush,new Thickness(0,10,0,0));body.Children.Add(note);
        var actions=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
        var cancelButton=Button("Cancel");cancelButton.IsCancel=true;cancelButton.Click+=(_,_)=>dialog.Close();actions.Children.Add(cancelButton);
        var saveButton=Button("Save");saveButton.IsDefault=true;saveButton.Margin=new Thickness(10,0,0,0);actions.Children.Add(saveButton);
        dialog.RegisterName("PresetSaveContinue",saveButton);
        void Validate() {
            try {_=PresetSaveCommand.EncodeName(input.Text);saveButton.IsEnabled=destination.SelectedItem is SaveDestinationOption;
                note.Text="The chosen destination will be read again before you confirm overwriting it.";note.Foreground=MutedBrush;}
            catch(ArgumentException){saveButton.IsEnabled=false;note.Text="Enter 1–20 printable ASCII characters. Non-ASCII names are not supported yet.";note.Foreground=Brush("#FFA45C");}
        }
        input.TextChanged+=(_,_)=>Validate();destination.SelectionChanged+=(_,_)=>Validate();
        saveButton.Click+=(_,_)=> {Validate();if(saveButton.IsEnabled && destination.SelectedItem is SaveDestinationOption selected){dialog.Tag=new PresetSaveChoice(selected.WireIndex,input.Text);dialog.DialogResult=true;}};
        var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        layout.Children.Add(new ScrollViewer {Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        actions.Margin=new Thickness(0,18,0,0);Grid.SetRow(actions,1);layout.Children.Add(actions);dialog.Content=new Border {Background=BackgroundBrush,Padding=new Thickness(24),Child=layout};
        dialog.Loaded+=(_,_)=>{input.Focus();input.SelectAll();};Validate();return dialog;
    }

    private Window CreatePresetOverwriteDialog(CurrentPresetSnapshot expected,PresetSaveDestination proof,string name)
    {
        var dialog=new Window {Title="Confirm preset overwrite",Owner=IsVisible?this:null,Width=580,Height=390,MinWidth=420,MinHeight=340,
            WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BackgroundBrush,Foreground=Brushes.White,FontFamily=FontFamily,FontSize=14};
        NameScope.SetNameScope(dialog,new NameScope());
        var body=new StackPanel();body.Children.Add(Text("Confirm preset overwrite",24));
        body.Children.Add(Text($"Current amplifier settings from {expected.SlotLabel} · {expected.Name}",13,MutedBrush,new Thickness(0,12,0,18)));
        body.Children.Add(Text("Save to preset",13,MutedBrush));
        body.Children.Add(Text($"{PresetAddress.FromWireIndex(proof.WireIndex).Label} · {proof.Name}",18,Brushes.White,new Thickness(0,6,0,14)));
        body.Children.Add(Text($"New name: {name}",14));
        body.Children.Add(Text("This replaces the saved settings and name in the destination. After saving, that preset becomes active.",13,MutedBrush,new Thickness(0,18,0,0)));
        var actions=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0)};
        var cancelButton=Button("Cancel");cancelButton.IsCancel=true;cancelButton.IsDefault=true;cancelButton.Click+=(_,_)=>dialog.DialogResult=false;
        var saveButton=Button("Save");saveButton.Margin=new Thickness(10,0,0,0);saveButton.Click+=(_,_)=>dialog.DialogResult=true;
        AutomationProperties.SetName(saveButton,"Confirm saving over the destination preset");
        dialog.RegisterName("PresetOverwriteSave",saveButton);dialog.RegisterName("PresetOverwriteCancel",cancelButton);
        actions.Children.Add(cancelButton);actions.Children.Add(saveButton);
        var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        layout.Children.Add(new ScrollViewer {Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        Grid.SetRow(actions,1);layout.Children.Add(actions);dialog.Content=new Border {Background=BackgroundBrush,Padding=new Thickness(24),Child=layout};
        dialog.Loaded+=(_,_)=>cancelButton.Focus();return dialog;
    }
}
