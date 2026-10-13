using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using Iamp.Transport.Windows;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed class MainWindow : Window
{
    private static readonly Brush BackgroundBrush = Brush("#15171B"), PanelBrush = Brush("#20242B"),
        MutedBrush = Brush("#A3ADBD"), BlueBrush = Brush("#4C9EFF");
    private readonly ComboBox devices = new() { DisplayMemberPath = "DisplayName", MinWidth = 360 };
    private readonly TextBox address = new() { Width = 180 };
    private readonly Button scan = Button("Scan Bluetooth"), read = Button("Read amplifier"), cancel = Button("Cancel");
    private readonly TextBlock status = Text("Ready to scan", 14), snapshot = Text("No snapshot yet · Disconnected", 13, MutedBrush);
    private readonly TextBlock firmware = Text("—", 28), presetCount = Text("—", 28), libraryCount = Text("—", 28), rhythmCount = Text("—", 28);
    private readonly TextBlock currentPreset = Text("Read the amplifier to see its current preset.", 20);
    private readonly WrapPanel effectChain = new();
    private readonly StackPanel effectDetails = new();
    private readonly List<(Button Button, EffectPresentation Effect)> effectButtons = new();
    private readonly TextBox search = new() { MinWidth = 280 };
    private readonly ListView presets = new() { Background = PanelBrush, BorderThickness = new Thickness(0), Foreground = Brushes.White };
    private readonly TextBlock count = Text("0 presets", 13, MutedBrush);
    private CancellationTokenSource? operation;
    private IReadOnlyList<PresetName> names = Array.Empty<PresetName>();
    private DateTimeOffset? snapshotTime;
    private bool lastReadSucceeded;
    private readonly bool preview;

    public MainWindow(bool preview = false)
    {
        this.preview = preview;
        Title = "iAMP Desktop · Amplifier library"; Width = 1280; Height = 900; MinWidth = 1000; MinHeight = 720;
        Background = BackgroundBrush; Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var root = new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(212) });
        root.ColumnDefinitions.Add(new ColumnDefinition()); Content = root;
        root.Children.Add(BuildSidebar());
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(scroll, 1); root.Children.Add(scroll);
        var page = new StackPanel { Margin = new Thickness(32, 27, 32, 24) }; scroll.Content = page;
        var header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titles = new StackPanel(); titles.Children.Add(Text("Amplifier library", 30));
        titles.Children.Add(Text("Read your amplifier’s information and browse its presets.", 14, MutedBrush, new Thickness(0,7,0,0)));
        header.Children.Add(titles);
        var badge = new Border { Background = Brush("#18304D"), CornerRadius = new CornerRadius(6), Padding = new Thickness(13,7,13,7), VerticalAlignment = VerticalAlignment.Top,
            Child = Text("READ ONLY", 11, BlueBrush) }; Grid.SetColumn(badge,1); header.Children.Add(badge); page.Children.Add(header);
        page.Children.Add(BuildConnection());
        var cardGrid = new Grid { Margin = new Thickness(0,20,0,0) };
        for (var i=0;i<4;i++) cardGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var values = new[] { Card("FIRMWARE", firmware, "Reported by amplifier"), Card("PRESET SLOTS", presetCount, "Names read from your amp"),
            Card("GIR / GNR SLOTS", libraryCount, "Capture library capacity"), Card("DRUMS / LOOPER", rhythmCount, "Patterns and file slots") };
        for (var i=0;i<values.Length;i++) { Grid.SetColumn(values[i],i); values[i].Margin=new Thickness(0,0,i<3?12:0,0); cardGrid.Children.Add(values[i]); }
        page.Children.Add(cardGrid);
        var currentPanel=new StackPanel();
        currentPanel.Children.Add(Text("CURRENT PRESET",10,BlueBrush));
        currentPreset.Margin=new Thickness(0,8,0,0);currentPanel.Children.Add(currentPreset);
        page.Children.Add(new Border { Background=Brush("#1D2B3C"),CornerRadius=new CornerRadius(8),Padding=new Thickness(18),
            Margin=new Thickness(0,14,0,0),Child=currentPanel });
        var effects=new StackPanel();effects.Children.Add(Text("Preset effects",18));
        effects.Children.Add(Text("Select an effect to view its snapshot. All values are read only.",12,MutedBrush,new Thickness(0,5,0,12)));
        effects.Children.Add(effectChain);effectDetails.Margin=new Thickness(0,15,0,0);effects.Children.Add(effectDetails);
        effectDetails.Children.Add(Text("Read the amplifier to see its preset effects.",13,MutedBrush));
        page.Children.Add(new Border { Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(18),Margin=new Thickness(0,14,0,0),Child=effects });
        var library = new StackPanel { Margin = new Thickness(0,27,0,0) };
        var bar = new Grid(); bar.ColumnDefinitions.Add(new ColumnDefinition()); bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var label = new StackPanel(); label.Children.Add(Text("Presets", 22)); label.Children.Add(count); bar.Children.Add(label);
        var searchPanel = new StackPanel(); searchPanel.Children.Add(Text("Search names or index", 12, MutedBrush));
        searchPanel.Children.Add(search); Grid.SetColumn(searchPanel,1); bar.Children.Add(searchPanel); library.Children.Add(bar);
        var view = new GridView(); view.Columns.Add(new GridViewColumn { Header = "INDEX", Width = 100, DisplayMemberBinding = new Binding("WireIndex") });
        view.Columns.Add(new GridViewColumn { Header = "PRESET NAME", Width = 650, DisplayMemberBinding = new Binding("Name") }); presets.View = view;
        presets.Height = 315; presets.Margin = new Thickness(0,16,0,0); library.Children.Add(presets);
        var headerStyle = new Style(typeof(GridViewColumnHeader));
        headerStyle.Setters.Add(new Setter(Control.BackgroundProperty,Brush("#2C323C")));
        headerStyle.Setters.Add(new Setter(Control.ForegroundProperty,MutedBrush));
        headerStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(10,9,10,9)));
        headerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Left));
        headerStyle.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="GridViewColumnHeader">
              <Border Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}">
                <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"/>
              </Border>
            </ControlTemplate>
            """)));
        view.ColumnHeaderContainerStyle=headerStyle;
        library.Children.Add(Text("Preset indices are shown as reported by the amplifier.",12,MutedBrush,new Thickness(0,10,0,0)));
        page.Children.Add(library);
        page.Children.Add(Text("This version reads amplifier information, preset names and supported current-preset details. Editing and preset switching are not available yet.",13,MutedBrush,new Thickness(0,24,0,0)));
        StyleInputs(); cancel.IsEnabled=false;
        scan.Click += async (_,_) => await ScanAsync(); read.Click += async (_,_) => await ReadAsync(); cancel.Click += (_,_) => operation?.Cancel();
        search.TextChanged += (_,_) => Filter();
        Closed += (_,_) => operation?.Cancel();
        if (preview) SetDemo();
    }

    private UIElement BuildSidebar()
    {
        var side = new StackPanel { Margin = new Thickness(22,28,18,20) };
        side.Children.Add(Text("iAMP",36)); side.Children.Add(Text("DESKTOP COMPANION",10,MutedBrush,new Thickness(0,2,0,37)));
        side.Children.Add(new Border { Background=Brush("#203B5C"), CornerRadius=new CornerRadius(7), Padding=new Thickness(14,12,14,12), Child=Text("Amplifier library",14,BlueBrush) });
        side.Children.Add(Text("F15i Li",17,Brush("#D5DCE8"),new Thickness(14,30,0,5)));
        side.Children.Add(Text("Bluetooth connection",12,MutedBrush,new Thickness(14,0,0,0)));
        side.Children.Add(Text("Snapshot viewer",12,MutedBrush,new Thickness(14,22,0,0)));
        return new Border { Background=Brush("#1A1D22"), BorderBrush=Brush("#30353D"), BorderThickness=new Thickness(0,0,1,0), Child=side };
    }

    private UIElement BuildConnection()
    {
        var panel = new StackPanel();
        panel.Children.Add(Text("Connect your amplifier",18));
        panel.Children.Add(Text("Turn on the amp and disconnect the iAMP phone app before reading.",13,MutedBrush,new Thickness(0,6,0,16)));
        var row = new WrapPanel(); row.Children.Add(devices); scan.Margin=new Thickness(12,0,0,0); row.Children.Add(scan); panel.Children.Add(row);
        var manual = new WrapPanel { Margin=new Thickness(0,13,0,0) }; manual.Children.Add(Text("Or enter Bluetooth address",12,MutedBrush,new Thickness(0,8,12,0)));
        manual.Children.Add(address); address.ToolTip="12 hexadecimal digits, with optional colons or hyphens";
        manual.Children.Add(Text("Optional · overrides selection",12,MutedBrush,new Thickness(12,8,0,0))); panel.Children.Add(manual);
        var actions = new WrapPanel { Margin=new Thickness(0,17,0,0) }; read.Background=BlueBrush; read.Foreground=Brush("#101C2B");
        actions.Children.Add(read); cancel.Margin=new Thickness(10,0,0,0); actions.Children.Add(cancel);
        status.Margin=new Thickness(17,8,0,0); actions.Children.Add(status); panel.Children.Add(actions);
        snapshot.Margin=new Thickness(0,13,0,0); panel.Children.Add(snapshot);
        return new Border { Background=PanelBrush, CornerRadius=new CornerRadius(10), Padding=new Thickness(22), Margin=new Thickness(0,25,0,0), Child=panel };
    }

    private static Border Card(string label,TextBlock value,string caption)
    {
        var stack=new StackPanel(); stack.Children.Add(Text(label,10,MutedBrush)); value.Margin=new Thickness(0,8,0,8); stack.Children.Add(value); stack.Children.Add(Text(caption,11,MutedBrush));
        return new Border { Background=PanelBrush, CornerRadius=new CornerRadius(8), Padding=new Thickness(17), Child=stack };
    }
    private void StyleInputs()
    {
        foreach(var box in new[]{address,search}) { box.Background=Brush("#15191F");box.Foreground=Brushes.White;box.BorderBrush=Brush("#46505E");box.Padding=new Thickness(9,7,9,7);box.CaretBrush=Brushes.White; }
        devices.Padding=new Thickness(7); devices.Foreground=Brush("#15171B");
    }
    private void Begin(string message)
    {
        operation=new CancellationTokenSource(TimeSpan.FromSeconds(75)); scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=false;
        cancel.IsEnabled=true; status.Text=message; status.Foreground=BlueBrush;
    }
    private void End()
    {
        operation?.Dispose(); operation=null; scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=!preview;cancel.IsEnabled=false;
    }
    private async Task ScanAsync()
    {
        Begin("Scanning Bluetooth…");
        try { var found=await F15DeviceDiscovery.ScanAsync(operation!.Token); devices.ItemsSource=found; if(found.Count>0)devices.SelectedIndex=0;
            status.Text=found.Count>0?$"Found {found.Count} amplifier(s)":"No amplifier found. Scan again or enter an address."; }
        catch(OperationCanceledException){status.Text="Scan cancelled";}
        catch(Exception ex){status.Text=$"Scan failed: {ex.Message}";}
        finally { status.Foreground=MutedBrush; End(); }
    }
    private async Task ReadAsync()
    {
        lastReadSucceeded=false;
        var input=address.Text.Replace(":","").Replace("-","").Replace(" ","");
        ulong selected;
        if(input.Length>0) {
            if(input.Length!=12||!ulong.TryParse(input,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out selected)) {status.Text="Enter a Bluetooth address with 12 hexadecimal digits.";return;}
        } else if(devices.SelectedItem is F15DiscoveredDevice device) selected=device.Address;
        else {status.Text="Select an amplifier or enter its Bluetooth address.";return;}
        Begin("Connecting…"); snapshot.Text=names.Count>0?"Previous snapshot · Reading new data…":"Reading amplifier…";
        try {
            F15Identity identity; IReadOnlyList<PresetName> loaded; CurrentPresetSnapshot current;
            await using(var session=await F15ReadSession.ConnectAsync(selected,operation!.Token)) {
                status.Text="Reading amplifier information…"; identity=await session.ReadIdentityAsync(operation.Token);
                status.Text="Reading preset names…"; loaded=await session.ReadPresetNamesAsync(operation.Token);
                status.Text="Reading current preset…";current=await session.ReadCurrentPresetAsync(operation.Token);
            }
            firmware.Text=identity.Firmware;presetCount.Text=identity.PresetSlots.ToString();libraryCount.Text=$"{identity.GirSlots} / {identity.GnrSlots}";
            rhythmCount.Text=$"{identity.DrumPatterns} / {identity.LooperFiles}"; names=loaded;Filter();
            currentPreset.Text=$"Index {current.WireIndex} · {current.Name}";
            SetEffects(current.Blocks);
            snapshotTime=DateTimeOffset.Now;snapshot.Text=SnapshotLabel();status.Text="Read complete";lastReadSucceeded=true;
        }
        catch(OperationCanceledException){status.Text="Read cancelled";snapshot.Text=SnapshotLabel();}
        catch(Exception ex){status.Text=ReadError(ex);snapshot.Text=SnapshotLabel();}
        finally {status.Foreground=MutedBrush;End();}
    }
    internal async Task<bool> ReadSnapshotPreviewAsync(string bluetoothAddress)
    {
        address.Text=bluetoothAddress;
        await ReadAsync();
        return lastReadSucceeded;
    }
    private string SnapshotLabel()=>snapshotTime is {} time ? $"Snapshot · {time:dd MMM yyyy, HH:mm:ss zzz} · Disconnected" : "No snapshot · Disconnected";
    private static string ReadError(Exception error)=>error switch {
        TimeoutException => "No response. Disconnect the phone app and try reading again.",
        FormatException => "This amplifier’s response is not supported by this version.",
        _ => "Could not read the amplifier. Check Bluetooth, the address and the phone app connection."
    };
    private void Filter()
    {
        var term=search.Text.Trim();var filtered=names.Where(p=>p.Name.Contains(term,StringComparison.OrdinalIgnoreCase)||p.WireIndex.ToString().Contains(term)).ToArray();
        presets.ItemsSource=filtered;count.Text=$"{filtered.Length} of {names.Count} presets";
    }
    private void SetDemo()
    {
        firmware.Text="Demo";presetCount.Text="80";libraryCount.Text="10 / 10";rhythmCount.Text="70 / 10";
        currentPreset.Text="Index 3 · Demo · Lead tone";
        SetEffects(new[] { new PresetEffectBlock(1,1,new ushort[]{0,5,25,50,75}),new PresetEffectBlock(2,2,Array.Empty<ushort>()),
            new PresetEffectBlock(3,9,Array.Empty<ushort>()),new PresetEffectBlock(4,7,Array.Empty<ushort>()),new PresetEffectBlock(5,8,Array.Empty<ushort>()) });
        names=new[]{new PresetName(1,"Demo · Clean tone"),new PresetName(2,"Demo · Warm rhythm"),new PresetName(3,"Demo · Lead tone"),
            new PresetName(4,"Demo · Ambient space"),new PresetName(5,"Demo · Bright clean"),new PresetName(6,"Demo · Vintage drive")};
        Filter(); status.Text="PREVIEW · Demo data";status.Foreground=BlueBrush;snapshot.Text="Layout preview · Demo data · No amplifier connected";
        scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=false;
    }
    private void SetEffects(IReadOnlyList<PresetEffectBlock> blocks)
    {
        effectChain.Children.Clear();effectButtons.Clear();effectDetails.Children.Clear();
        foreach(var block in blocks.Where(b=>b.TypeCode!=0)) {
            var effect=EffectPresentation.From(block);var button=Button(effect.Category);
            button.MinWidth=100;button.Margin=new Thickness(0,0,10,8);button.Background=Brush(effect.Color);button.Foreground=Brush("#15171B");
            button.BorderThickness=new Thickness(0,0,0,3);button.BorderBrush=Brushes.Transparent;
            button.ToolTip=$"View {effect.Category} details";button.Click+=(_,_)=>SelectEffect(effect);
            effectButtons.Add((button,effect));effectChain.Children.Add(button);
        }
        if(effectButtons.Count>0)SelectEffect(effectButtons[0].Effect);
        else effectDetails.Children.Add(Text("No preset effect details available.",13,MutedBrush));
    }
    private void SelectEffect(EffectPresentation effect)
    {
        foreach(var item in effectButtons)item.Button.BorderBrush=ReferenceEquals(item.Effect,effect)?Brushes.White:Brushes.Transparent;
        effectDetails.Children.Clear();effectDetails.Children.Add(Text(effect.Title,22,Brush(effect.Color)));
        if(effect.Parameters.Count==0) {effectDetails.Children.Add(Text("Parameter details not available in this version.",13,MutedBrush,new Thickness(0,8,0,0)));return;}
        var values=new WrapPanel { Margin=new Thickness(0,12,0,0) };
        foreach(var parameter in effect.Parameters) {
            var cell=new StackPanel { MinWidth=110,Margin=new Thickness(0,0,24,0) };
            cell.Children.Add(Text(parameter.Name,12,MutedBrush));cell.Children.Add(Text(parameter.Value.ToString(),27));values.Children.Add(cell);
        }
        effectDetails.Children.Add(values);
    }
    private static Brush Brush(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    private static TextBlock Text(string text,double size,Brush? color=null,Thickness? margin=null)=>new(){Text=text,FontSize=size,Foreground=color??Brushes.White,Margin=margin??new Thickness(0),TextWrapping=TextWrapping.Wrap};
    private static Button Button(string text)
    {
        var button=new Button{Content=text,Padding=new Thickness(14,8,14,8),Background=Brush("#343B46"),Foreground=Brushes.White,BorderThickness=new Thickness(0),Cursor=System.Windows.Input.Cursors.Hand};
        button.Template=(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="Button">
              <Border x:Name="Surface" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="5" Padding="{TemplateBinding Padding}">
                <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
              </Border>
              <ControlTemplate.Triggers>
                <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Surface" Property="Opacity" Value="0.45"/></Trigger>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Opacity" Value="0.8"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        return button;
    }
}
