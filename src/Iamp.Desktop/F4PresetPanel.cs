using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

/// <summary>F4 preset selection driven by confirmed amplifier state.</summary>
public sealed class F4PresetPanel : Border
{
    private static readonly Brush Blue=Colour("#4C9EFF"),Muted=Colour("#A3ADBD"),Inactive=Colour("#4A505A");
    private readonly TextBlock bankLabel=Label("Bank not read",13,Muted),bankValue=Label("—",22,Blue),activePreset=Label("No amplifier preset read yet",16,Muted);
    private readonly Border[] switchCards=new Border[4];
    private readonly TextBlock[] slotLabels=new TextBlock[4],presetLabels=new TextBlock[4];
    private readonly Border[] indicators=new Border[4];
    private readonly Ellipse[] switchRings=new Ellipse[4];
    private readonly Button[] switchButtons=new Button[4];
    private readonly Button previousBank=MainWindow.Button("← Previous bank"),nextBank=MainWindow.Button("Next bank →");
    private IReadOnlyList<PresetName> loadedNames=Array.Empty<PresetName>();
    private CurrentPresetSnapshot? actual;
    private bool confirmed,canRecall;
    private int? displayedBank,lastWire;
    public event Action<int>? PresetRequested;

    public F4PresetPanel()
    {
        Background=Colour("#20242B");BorderBrush=Colour("#3A424E");BorderThickness=new Thickness(1);CornerRadius=new CornerRadius(12);Padding=new Thickness(16);
        HorizontalAlignment=HorizontalAlignment.Stretch;
        var body=new StackPanel();
        var header=new Grid();header.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
        var brand=Label("MOOER",13,Brushes.White);brand.FontWeight=FontWeights.Bold;brand.VerticalAlignment=VerticalAlignment.Center;header.Children.Add(brand);
        var display=new StackPanel {HorizontalAlignment=HorizontalAlignment.Center};bankLabel.TextAlignment=TextAlignment.Center;bankValue.TextAlignment=TextAlignment.Center;bankValue.FontFamily=new FontFamily("Consolas");display.Children.Add(bankLabel);display.Children.Add(bankValue);Grid.SetColumn(display,1);header.Children.Add(display);
        var model=Label("F4",18,Muted);model.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(model,2);header.Children.Add(model);body.Children.Add(header);
        var banks=new WrapPanel {HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,10,0,0)};
        foreach(var button in new[]{previousBank,nextBank}){button.Padding=new Thickness(12,6,12,6);button.Margin=new Thickness(4);button.Background=Colour("#343B46");button.Foreground=Brushes.White;banks.Children.Add(button);}
        previousBank.Click+=(_,_)=>BrowseBank(-1);nextBank.Click+=(_,_)=>BrowseBank(1);body.Children.Add(banks);
        AutomationProperties.SetName(bankLabel,"F4 bank context");AutomationProperties.SetName(bankValue,"F4 bank number");AutomationProperties.SetName(activePreset,"F4 active preset");
        var switches=new UniformGrid {Columns=4,Margin=new Thickness(-4,14,-4,0)};
        for(var i=0;i<4;i++){
            var stack=new StackPanel();
            indicators[i]=new Border {Width=30,Height=4,CornerRadius=new CornerRadius(2),Background=Inactive,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,10)};stack.Children.Add(indicators[i]);
            var stomp=new Grid {Width=40,Height=40,HorizontalAlignment=HorizontalAlignment.Center};
            switchRings[i]=new Ellipse {Stroke=Inactive,StrokeThickness=2,Fill=new LinearGradientBrush(Color.FromRgb(0xD9,0xDE,0xE5),Color.FromRgb(0x64,0x6D,0x78),90)};stomp.Children.Add(switchRings[i]);
            stomp.Children.Add(new Ellipse {Width=24,Height=24,Stroke=Colour("#DCE2EA"),StrokeThickness=1,Fill=new LinearGradientBrush(Color.FromRgb(0x94,0x9D,0xA8),Color.FromRgb(0xD8,0xDE,0xE5),90)});stack.Children.Add(stomp);
            var letter=Label(((char)('A'+i)).ToString(),16,Brushes.White);letter.FontWeight=FontWeights.Bold;letter.TextAlignment=TextAlignment.Center;letter.Margin=new Thickness(0,8,0,4);stack.Children.Add(letter);
            slotLabels[i]=Label("—",12,Muted);slotLabels[i].TextAlignment=TextAlignment.Center;stack.Children.Add(slotLabels[i]);
            presetLabels[i]=Label("Preset not read",12,Muted);presetLabels[i].TextAlignment=TextAlignment.Center;presetLabels[i].TextWrapping=TextWrapping.Wrap;presetLabels[i].TextTrimming=TextTrimming.CharacterEllipsis;presetLabels[i].Height=48;presetLabels[i].Margin=new Thickness(0,4,0,0);stack.Children.Add(presetLabels[i]);
            switchCards[i]=new Border {CornerRadius=new CornerRadius(7),Padding=new Thickness(5,9,5,5),Margin=new Thickness(4,0,4,0),BorderThickness=new Thickness(1),BorderBrush=Brushes.Transparent,Child=stack};
            var index=i;
            switchButtons[i]=new Button {Content=switchCards[i],Cursor=System.Windows.Input.Cursors.Hand,HorizontalContentAlignment=HorizontalAlignment.Stretch,Padding=new Thickness(0),Background=Brushes.Transparent,BorderThickness=new Thickness(0)};
            switchButtons[i].Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TargetType=\"Button\"><Border x:Name=\"focus\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" BorderThickness=\"1\" BorderBrush=\"Transparent\" CornerRadius=\"8\"><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property=\"IsKeyboardFocused\" Value=\"True\"><Setter TargetName=\"focus\" Property=\"BorderBrush\" Value=\"#4C9EFF\"/></Trigger><Trigger Property=\"IsMouseOver\" Value=\"True\"><Setter TargetName=\"focus\" Property=\"Background\" Value=\"#303943\"/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
            switchButtons[i].Click+=(_,_)=>{if(switchButtons[index].IsEnabled&&displayedBank is {} bank)PresetRequested?.Invoke(bank*4+index+1);};
            AutomationProperties.SetName(switchCards[i],$"F4 switch {(char)('A'+i)}, preset not read");switches.Children.Add(switchButtons[i]);
        }
        body.Children.Add(switches);activePreset.TextWrapping=TextWrapping.Wrap;activePreset.Margin=new Thickness(0,14,0,0);body.Children.Add(activePreset);Child=body;
        Update(Array.Empty<PresetName>(),null,false);
    }

    public void Update(IReadOnlyList<PresetName> names,CurrentPresetSnapshot? current,bool currentConfirmed,bool allowRecall=false)
    {
        loadedNames=names;actual=current;confirmed=currentConfirmed;canRecall=allowRecall;
        if(current?.WireIndex!=lastWire){displayedBank=current is {WireIndex:>=1 and <=80}?PresetAddress.FromWireIndex(current.WireIndex).Bank:null;lastWire=current?.WireIndex;}
        Render();
    }
    private void BrowseBank(int step)
    {
        if(!canRecall||displayedBank is not {} bank)return;
        displayedBank=Math.Clamp(bank+step,0,19);Render();
    }
    private void Render()
    {
        var current=actual;var names=loadedNames;var currentConfirmed=confirmed;
        var known=current is {WireIndex:>=1 and <=80};
        var address=known?PresetAddress.FromWireIndex(current!.WireIndex):(PresetAddress?)null;
        bankLabel.Text=!known?"Bank not read":displayedBank!=address?.Bank?"Selected bank":currentConfirmed?"Current bank":"Last read bank";
        bankValue.Text=displayedBank?.ToString(System.Globalization.CultureInfo.InvariantCulture)??"—";
        previousBank.IsEnabled=canRecall&&displayedBank>0;nextBank.IsEnabled=canRecall&&displayedBank<19;
        activePreset.Text=!known?"No amplifier preset read yet":$"{(currentConfirmed?"Active":"Last read")}: {address!.Value.Label} · {current!.Name}";
        activePreset.Foreground=known?Blue:Muted;
        for(var i=0;i<4;i++){
            var wire=known?displayedBank!.Value*4+i+1:0;
            var active=known&&wire==current!.WireIndex;
            var slot=known?PresetAddress.FromWireIndex(wire).Label:"—";
            var name=!known?"Preset not read":active?current!.Name:names.FirstOrDefault(n=>n.WireIndex==wire)?.Name??"Name not loaded";
            slotLabels[i].Text=slot;presetLabels[i].Text=name;presetLabels[i].Foreground=active?Brushes.White:Muted;
            switchCards[i].Background=active?Colour("#203B5C"):Brushes.Transparent;switchCards[i].BorderBrush=active?Blue:Brushes.Transparent;
            switchRings[i].Stroke=active?Blue:Inactive;indicators[i].Background=active?Blue:Inactive;
            var description=$"F4 switch {(char)('A'+i)}, {slot}, {name}"+(active?(currentConfirmed?", active preset":", last read preset"):"");
            switchCards[i].ToolTip=description;AutomationProperties.SetName(switchCards[i],description);
            AutomationProperties.SetName(switchButtons[i],description);
            switchButtons[i].IsEnabled=canRecall&&known&&!active&&names.Any(n=>n.WireIndex==wire);
            switchButtons[i].ToolTip=description+(active?"":" · Click to recall");
        }
        ToolTip=currentConfirmed?"Choose a bank, then click A, B, C or D to recall its preset.":"Retained amplifier preset information. Connect to change presets.";
    }

    private static TextBlock Label(string text,double size,Brush colour)=>new(){Text=text,FontSize=size,Foreground=colour};
    private static Brush Colour(string value)=>(Brush)new BrushConverter().ConvertFromString(value)!;
}
