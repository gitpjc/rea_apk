using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using Iamp.Transport.Windows;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed partial class MainWindow : Window
{
    private static readonly Brush BackgroundBrush = Brush("#15171B"), PanelBrush = Brush("#20242B"),
        MutedBrush = Brush("#A3ADBD"), BlueBrush = Brush("#4C9EFF");
    private readonly ComboBox devices = new() { DisplayMemberPath = "DisplayName", Width = 360 };
    private readonly TextBox address = new() { Width = 180 };
    private readonly Button scan = Button("Scan Bluetooth"), read = Button("Read amplifier"), cancel = Button("Cancel");
    private readonly TextBlock status = Text("Ready to scan", 14), snapshot = Text("No snapshot yet · Disconnected", 13, MutedBrush);
    private readonly TextBlock firmware = Text("—", 28), presetCount = Text("—", 28), libraryCount = Text("—", 28), rhythmCount = Text("—", 28);
    private readonly TextBlock currentPreset = Text("Read the amplifier to see its current preset.", 20);
    private readonly TextBlock sourceBadge = Text("EXPERIMENTAL EDIT",11,BlueBrush);
    private readonly WrapPanel effectChain = new();
    private readonly StackPanel effectDetails = new();
    private readonly TextBox thresholdInput = new() { Width=90, MaxLength=2 };
    private readonly Button applyThreshold = Button("Apply threshold to amp");
    private readonly TextBlock thresholdMessage = Text("",12,MutedBrush);
    private readonly TextBox toneInput = new() { Width=90, MaxLength=3 };
    private readonly Button applyTone = Button("Apply Tone to amp");
    private readonly TextBlock toneMessage = Text("",12,MutedBrush);
    private CurrentPresetSnapshot? currentSnapshot;
    private ulong? snapshotAddress;
    private string? snapshotFirmware;
    private bool editSnapshotValid;
    private bool liveEditUnsaved;
    private string? originalSnapshotDetail;
    private bool lastMutationSucceeded;
    private readonly List<(Button Button, EffectPresentation Effect)> effectButtons = new();
    private readonly TextBox search = new() { Width = 280 };
    private readonly ListView presets = new() { Background = PanelBrush, BorderThickness = new Thickness(0), Foreground = Brushes.White };
    private readonly TextBlock count = Text("0 presets", 13, MutedBrush);
    private readonly Button selectPreset = Button("Select preset on amp");
    private readonly TextBlock selectionMessage = Text("Read a supported amplifier before selecting a preset.",12,MutedBrush);
    private CancellationTokenSource? operation;
    private IReadOnlyList<PresetName> names = Array.Empty<PresetName>();
    private DateTimeOffset? snapshotTime;
    private bool lastReadSucceeded;
    private readonly bool preview;
    private readonly ScrollViewer libraryScroll=new() {VerticalScrollBarVisibility=ScrollBarVisibility.Auto};

    public MainWindow(bool preview = false)
    {
        this.preview = preview;
        Title = "iAMP Desktop · Amplifier library"; Width = Math.Min(1280,SystemParameters.WorkArea.Width); Height = Math.Min(900,SystemParameters.WorkArea.Height); MinWidth = 620; MinHeight = 320;
        Background = BackgroundBrush; Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var root = new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(212) });
        root.ColumnDefinitions.Add(new ColumnDefinition()); Content = root;
        root.Children.Add(BuildSidebar());
        var scroll = libraryScroll;
        Grid.SetColumn(scroll, 1); root.Children.Add(scroll);
        Grid.SetColumn(historyScroll,1);root.Children.Add(historyScroll);
        var page = new StackPanel { Margin = new Thickness(32, 20, 32, 24) }; scroll.Content = page;
        var header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});header.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        var titles = new StackPanel(); titles.Children.Add(Text("Amplifier library", 30));
        titles.Children.Add(Text("Read your amplifier’s information and browse its presets.", 14, MutedBrush, new Thickness(0,7,0,0)));
        header.Children.Add(titles);
        var badge = new Border { Background = Brush("#18304D"), CornerRadius = new CornerRadius(6), Padding = new Thickness(13,7,13,7), VerticalAlignment = VerticalAlignment.Top,
            Child = sourceBadge }; Grid.SetColumn(badge,1); header.Children.Add(badge); page.Children.Add(header);
        page.Children.Add(BuildConnection());
        var cardGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns=4, Margin = new Thickness(0,14,0,0) };
        var values = new[] { Card("FIRMWARE", firmware, "Reported by amplifier"), Card("PRESET SLOTS", presetCount, "Names read from your amp"),
            Card("GIR / GNR SLOTS", libraryCount, "Capture library capacity"), Card("DRUMS / LOOPER", rhythmCount, "Patterns and file slots") };
        for (var i=0;i<values.Length;i++) { values[i].Margin=new Thickness(0,0,12,12); cardGrid.Children.Add(values[i]); }
        page.Children.Add(cardGrid);
        var currentPanel=new StackPanel();
        currentPanel.Children.Add(Text("CURRENT PRESET",10,BlueBrush));
        currentPreset.Margin=new Thickness(0,8,0,0);currentPanel.Children.Add(currentPreset);
        savePreset.Margin=new Thickness(0,12,0,0);savePreset.HorizontalAlignment=HorizontalAlignment.Left;
        savePreset.Click+=async(_,_)=>await SavePresetAsync();currentPanel.Children.Add(savePreset);
        currentPanel.Children.Add(saveMessage);
        page.Children.Add(new Border { Background=Brush("#1D2B3C"),CornerRadius=new CornerRadius(8),Padding=new Thickness(14),
            Margin=new Thickness(0,10,0,0),Child=currentPanel });
        var effects=new StackPanel();effects.Children.Add(Text("Preset effects",18));
        effects.Children.Add(Text("Select an effect to view its snapshot. Supported Noise Killer and OD808 values can be applied explicitly.",12,MutedBrush,new Thickness(0,5,0,12)));
        effects.Children.Add(effectChain);effectDetails.Margin=new Thickness(0,15,0,0);effects.Children.Add(effectDetails);
        effectDetails.Children.Add(Text("Read the amplifier to see its preset effects.",13,MutedBrush));
        page.Children.Add(new Border { Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(14),Margin=new Thickness(0,10,0,0),Child=effects });
        var library = new StackPanel { Margin = new Thickness(0,27,0,0) };
        var bar = new Grid(); bar.ColumnDefinitions.Add(new ColumnDefinition()); bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        var label = new StackPanel(); label.Children.Add(Text("Presets", 22)); label.Children.Add(count); bar.Children.Add(label);
        var searchPanel = new StackPanel(); searchPanel.Children.Add(Text("Search names or index", 12, MutedBrush));
        searchPanel.Children.Add(search); Grid.SetColumn(searchPanel,1); bar.Children.Add(searchPanel); library.Children.Add(bar);
        var selectionActions=new WrapPanel {Margin=new Thickness(0,12,0,0)};
        selectionActions.Children.Add(selectPreset);selectionMessage.Margin=new Thickness(12,8,0,0);selectionActions.Children.Add(selectionMessage);
        library.Children.Add(selectionActions);
        library.Children.Add(Text("Explicit selection recalls the saved preset; unsaved changes may be discarded. Available only between the two captured configurations at indices 4 and 5 on V1.1.9, while the preset matches its original snapshot. A changed configuration disables selection. Browsing rows changes nothing.",12,MutedBrush,new Thickness(0,8,0,0)));
        var indexTemplate = (DataTemplate)XamlReader.Parse("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <TextBlock Text="{Binding WireIndex}" TextAlignment="Right" HorizontalAlignment="Stretch" Margin="0,0,20,0" />
            </DataTemplate>
            """);
        var view = new GridView(); view.Columns.Add(new GridViewColumn { Header = "INDEX", Width = 64, CellTemplate = indexTemplate });
        view.Columns.Add(new GridViewColumn { Header = "PRESET NAME", Width = 650, DisplayMemberBinding = new Binding("Name") }); presets.View = view;
        // GridView cell presenters inherit this alignment; otherwise a templated
        // TextBlock is measured at its text width and cannot right-align the column.
        var rowStyle = new Style(typeof(ListViewItem));
        rowStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch));
        presets.ItemContainerStyle=rowStyle;
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
        var indexHeaderStyle = new Style(typeof(GridViewColumnHeader),headerStyle);
        indexHeaderStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Center));
        indexHeaderStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(0,9,0,9)));
        view.Columns[0].HeaderContainerStyle=indexHeaderStyle;
        page.SizeChanged+=(_,_)=> {
            var available=Math.Max(120,page.ActualWidth);
            var compact=available<620;
            devices.Width=Math.Min(360,Math.Max(120,available-36));search.Width=Math.Min(280,available);
            cardGrid.Columns=available<380?1:available<760?2:4;
            Grid.SetRow(badge,compact?1:0);Grid.SetColumn(badge,compact?0:1);Grid.SetColumnSpan(badge,compact?2:1);
            badge.HorizontalAlignment=compact?HorizontalAlignment.Left:HorizontalAlignment.Right;
            badge.Margin=new Thickness(0,compact?10:0,0,0);
            Grid.SetRow(searchPanel,compact?1:0);Grid.SetColumn(searchPanel,compact?0:1);Grid.SetColumnSpan(searchPanel,compact?2:1);
            searchPanel.Margin=new Thickness(0,compact?10:0,0,0);
            searchPanel.HorizontalAlignment=compact?HorizontalAlignment.Left:HorizontalAlignment.Right;
            view.Columns[1].Width=Math.Max(180,available-100);
        };
        library.Children.Add(Text("Preset indices are shown as reported by the amplifier.",12,MutedBrush,new Thickness(0,10,0,0)));
        page.Children.Add(library);
        page.Children.Add(Text("Experimental V1.1.9 support: five bounded live controls and captured preset recall. Saving is restricted to British Lead Solo at index 4 with Threshold 25 or 26 and every other value unchanged. Other-slot saving, renaming and broader editing remain unavailable.",13,MutedBrush,new Thickness(0,24,0,0)));
        InitializeAdditionalEditors();InitializeSnapshotArchive();InitializeDiagnostics();InitializeComparison();StyleInputs(); cancel.IsEnabled=false;
        scan.Click += async (_,_) => await ScanAsync(); read.Click += async (_,_) => await ReadAsync(); cancel.Click += (_,_) => operation?.Cancel();
        search.TextChanged += (_,_) => Filter();
        thresholdInput.TextChanged += (_,_) => UpdateThresholdState();
        applyThreshold.Click += async (_,_) => await ApplyThresholdAsync();
        toneInput.TextChanged += (_,_) => UpdateToneState();
        applyTone.Click += async (_,_) => await ApplyToneAsync();
        selectPreset.Click += async (_,_) => await SelectPresetAsync();
        presets.SelectionChanged += (_,_) => UpdatePresetSelectionState();
        address.TextChanged += (_,_) => InvalidateEditTarget();
        devices.SelectionChanged += (_,_) => InvalidateEditTarget();
        System.Windows.Automation.AutomationProperties.SetName(thresholdInput,"Pending Noise Killer threshold, 25 to 28");
        System.Windows.Automation.AutomationProperties.SetName(toneInput,"Pending OD808 Tone, 97 to 100");
        InitializeKeyboardNavigation();
        Closed += (_,_) => operation?.Cancel();
        if (preview) SetDemo();
        UpdatePresetSelectionState();
    }

    private UIElement BuildSidebar()
    {
        var side = new StackPanel { Margin = new Thickness(22,28,18,20) };
        side.Children.Add(Text("iAMP",36)); side.Children.Add(Text("DESKTOP COMPANION",10,MutedBrush,new Thickness(0,2,0,0)));
        side.Children.Add(Text($"Version {DesktopVersion.Current}",12,MutedBrush,new Thickness(0,7,0,26)));
        ConfigureNavigation();
        side.Children.Add(libraryNavigation);side.Children.Add(historyNavigation);
        side.Children.Add(Text("F15i Li",17,Brush("#D5DCE8"),new Thickness(14,30,0,5)));
        side.Children.Add(Text("Bluetooth connection",12,MutedBrush,new Thickness(14,0,0,0)));
        side.Children.Add(Text("Snapshot viewer",12,MutedBrush,new Thickness(14,22,0,0)));
        exportDiagnostics.Margin=new Thickness(0,18,0,0);
        exportDiagnostics.HorizontalAlignment=HorizontalAlignment.Stretch;
        side.Children.Add(exportDiagnostics);
        return new Border { Background=Brush("#1A1D22"), BorderBrush=Brush("#30353D"), BorderThickness=new Thickness(0,0,1,0), Child=new ScrollViewer {Content=side,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled} };
    }

    private UIElement BuildConnection()
    {
        var panel = new StackPanel();
        panel.Children.Add(Text("Connect your amplifier",18));
        panel.Children.Add(Text("Turn on the amp and disconnect the iAMP phone app before reading.",13,MutedBrush,new Thickness(0,4,0,10)));
        var row = new WrapPanel(); row.Children.Add(devices); scan.Margin=new Thickness(12,0,0,0); row.Children.Add(scan); panel.Children.Add(row);
        var manual = new WrapPanel { Margin=new Thickness(0,7,0,0) }; manual.Children.Add(Text("Bluetooth address",12,MutedBrush,new Thickness(0,8,12,0)));
        manual.Children.Add(address); address.ToolTip="12 hexadecimal digits, with optional colons or hyphens";
        manual.Children.Add(Text("Optional · overrides selection",12,MutedBrush,new Thickness(12,8,0,0)));
        panel.Children.Add(new Expander {Header="Manual Bluetooth address",Foreground=MutedBrush,Margin=new Thickness(0,8,0,0),Content=manual});
        var actions = new WrapPanel { Margin=new Thickness(0,12,0,0) }; read.Background=BlueBrush; read.Foreground=Brush("#101C2B");
        actions.Children.Add(read); cancel.Margin=new Thickness(10,0,0,0); actions.Children.Add(cancel);
        exportSnapshot.Margin=new Thickness(10,0,0,0);actions.Children.Add(exportSnapshot);
        openSnapshot.Margin=new Thickness(10,0,0,0);actions.Children.Add(openSnapshot);
        compareSnapshot.Margin=new Thickness(10,0,0,0);actions.Children.Add(compareSnapshot);
        foreach(var button in actions.Children.OfType<Button>())button.Margin=new Thickness(button==read?0:10,4,0,4);
        status.Margin=new Thickness(17,8,0,0); actions.Children.Add(status); panel.Children.Add(actions);
        snapshot.Margin=new Thickness(0,8,0,0); panel.Children.Add(snapshot);
        return new Border { Background=PanelBrush, CornerRadius=new CornerRadius(10), Padding=new Thickness(18), Margin=new Thickness(0,20,0,0), Child=panel };
    }

    private static Border Card(string label,TextBlock value,string caption)
    {
        var stack=new StackPanel(); stack.Children.Add(Text(label,10,MutedBrush)); value.Margin=new Thickness(0,8,0,8); stack.Children.Add(value); stack.Children.Add(Text(caption,11,MutedBrush));
        return new Border { Background=PanelBrush, CornerRadius=new CornerRadius(8), Padding=new Thickness(13), Child=stack };
    }
    private void StyleInputs()
    {
        foreach(var box in new[]{address,search,thresholdInput,toneInput}.Concat(AdditionalEditors.Select(e=>e.Input))) { box.Background=Brush("#15191F");box.Foreground=Brushes.White;box.BorderBrush=Brush("#46505E");box.Padding=new Thickness(9,7,9,7);box.CaretBrush=Brushes.White; }
        devices.Padding=new Thickness(7); devices.Foreground=Brush("#15171B");
    }
    private void Begin(string message)
    {
        operation=new CancellationTokenSource(TimeSpan.FromSeconds(75)); scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=false;
        cancel.IsEnabled=true; status.Text=message; status.Foreground=BlueBrush;
        UpdateDiagnosticsState();
        UpdateComparisonState();
        UpdateThresholdState();
    }
    private void End()
    {
        operation?.Dispose(); operation=null; scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=!preview;cancel.IsEnabled=false;
        UpdateDiagnosticsState();
        UpdateComparisonState();
        UpdateThresholdState();
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
        editSnapshotValid=false;UpdateThresholdState();
        var input=address.Text.Replace(":","").Replace("-","").Replace(" ","");
        ulong selected;
        if(input.Length>0) {
            if(input.Length!=12||!ulong.TryParse(input,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out selected)) {status.Text="Enter a Bluetooth address with 12 hexadecimal digits.";return;}
        } else if(devices.SelectedItem is F15DiscoveredDevice device) selected=device.Address;
        else {status.Text="Select an amplifier or enter its Bluetooth address.";return;}
        Begin("Connecting…"); snapshot.Text=names.Count>0?"Previous snapshot · Reading new data…":"Reading amplifier…";
        try {
            F15Identity identity; IReadOnlyList<PresetName> loaded; CurrentPresetSnapshot current;
            await using(var session=await F15ReadSession.ConnectAsync(selected,operation!.Token,diagnostics)) {
                status.Text="Reading amplifier information…"; identity=await session.ReadIdentityAsync(operation.Token);
                status.Text="Reading preset names…"; loaded=await session.ReadPresetNamesAsync(operation.Token);
                status.Text="Reading current preset…";current=await session.ReadCurrentPresetAsync(operation.Token);
            }
            firmware.Text=identity.Firmware;presetCount.Text=identity.PresetSlots.ToString();libraryCount.Text=$"{identity.GirSlots} / {identity.GnrSlots}";
            rhythmCount.Text=$"{identity.DrumPatterns} / {identity.LooperFiles}"; names=loaded;Filter();
            currentPreset.Text=$"Index {current.WireIndex} · {current.Name}";
            liveEditUnsaved=liveEditUnsaved && snapshotAddress==selected && currentSnapshot?.WireIndex==current.WireIndex;
            if(!liveEditUnsaved)originalSnapshotDetail=current.RawDetailPayloadHex;
            liveEditUnsaved=current.RawDetailPayloadHex!=originalSnapshotDetail;
            currentSnapshot=current;snapshotAddress=selected;snapshotFirmware=identity.Firmware;editSnapshotValid=true;
            localSnapshotSource=false;sourceBadge.Text="EXPERIMENTAL EDIT";
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
    private string SnapshotLabel()=>snapshotTime is {} time ? localSnapshotSource?
        $"Local snapshot · {time:dd MMM yyyy, HH:mm:ss zzz} · Offline · No amplifier changes":
        $"Snapshot · {time:dd MMM yyyy, HH:mm:ss zzz} · Disconnected{(liveEditUnsaved?" · Live edit verified; not saved":"")}" : "No snapshot · Disconnected";
    private static string ReadError(Exception error)=>error switch {
        TransferStateChangedException => "The amplifier reported a transfer-state change. This session was stopped; reconnect only when the amplifier is stable.",
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
        SetEffects(new[] { new PresetEffectBlock(1,4,new ushort[]{1,2,25,50,0,0}),new PresetEffectBlock(2,2,Array.Empty<ushort>()),
            new PresetEffectBlock(3,9,Array.Empty<ushort>()),new PresetEffectBlock(4,7,Array.Empty<ushort>()),new PresetEffectBlock(5,8,Array.Empty<ushort>()) });
        names=new[]{new PresetName(1,"Demo · Clean tone"),new PresetName(2,"Demo · Warm rhythm"),new PresetName(3,"Demo · Lead tone"),
            new PresetName(4,"Demo · Ambient space"),new PresetName(5,"Demo · Bright clean"),new PresetName(6,"Demo · Vintage drive")};
        Filter(); status.Text="PREVIEW · Demo data";status.Foreground=BlueBrush;snapshot.Text="Layout preview · Demo data · No amplifier connected";
        scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=false;
    }

    internal void PreparePresetListPreview()
    {
        if(!preview)throw new InvalidOperationException("Preset layout preview requires demo mode.");
        names=new[]{new PresetName(1,"Demo · Clean tone"),new PresetName(2,"Demo · Warm rhythm"),
            new PresetName(9,"Demo · Lead tone"),new PresetName(10,"Demo · Ambient space"),
            new PresetName(11,"Demo · Bright clean"),new PresetName(80,"Demo · Vintage drive")};
        Filter();
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
        if(thresholdInput.Parent is Panel oldInputParent)oldInputParent.Children.Remove(thresholdInput);
        if(applyThreshold.Parent is Panel oldApplyParent)oldApplyParent.Children.Remove(applyThreshold);
        if(toneInput.Parent is Panel oldToneParent)oldToneParent.Children.Remove(toneInput);
        if(applyTone.Parent is Panel oldToneApplyParent)oldToneApplyParent.Children.Remove(applyTone);
        DetachAdditionalEditorControls();
        foreach(var item in effectButtons)item.Button.BorderBrush=ReferenceEquals(item.Effect,effect)?Brushes.White:Brushes.Transparent;
        effectDetails.Children.Clear();effectDetails.Children.Add(Text(effect.Title,22,Brush(effect.Color)));
        if(effect.Parameters.Count==0) {effectDetails.Children.Add(Text("Parameter details not available in this version.",13,MutedBrush,new Thickness(0,8,0,0)));return;}
        var values=new WrapPanel { Margin=new Thickness(0,8,0,0) };
        foreach(var parameter in effect.Parameters) {
            var cell=new StackPanel { MinWidth=110,Margin=new Thickness(0,0,24,0) };
            cell.Children.Add(Text(parameter.Name,12,MutedBrush));cell.Children.Add(Text(parameter.Value.ToString(),27));values.Children.Add(cell);
        }
        effectDetails.Children.Add(values);
        if(effect.Source.TypeCode==4 && effect.Source.Values.Count==6 && effect.Source.Values[1]==2) {
            effectDetails.Children.Add(Text("Pending threshold · Experimental supported range 25–28",12,MutedBrush,new Thickness(0,10,0,7)));
            thresholdInput.Text=effect.Source.Values[2].ToString(CultureInfo.InvariantCulture);
            var actions=new WrapPanel();actions.Children.Add(thresholdInput);applyThreshold.Margin=new Thickness(12,0,0,0);actions.Children.Add(applyThreshold);
            effectDetails.Children.Add(actions);thresholdMessage.Margin=new Thickness(0,9,0,0);effectDetails.Children.Add(thresholdMessage);
            UpdateThresholdState();
        }
        if(effect.Source.TypeCode==1 && effect.Source.Values.Count==5 && effect.Source.Values[1]==3) {
            effectDetails.Children.Add(Text("Pending Tone · Experimental supported range 97–100",12,MutedBrush,new Thickness(0,10,0,7)));
            toneInput.Text=effect.Source.Values[3].ToString(CultureInfo.InvariantCulture);
            var actions=new WrapPanel();actions.Children.Add(toneInput);applyTone.Margin=new Thickness(12,0,0,0);actions.Children.Add(applyTone);
            effectDetails.Children.Add(actions);toneMessage.Margin=new Thickness(0,9,0,0);effectDetails.Children.Add(toneMessage);
            UpdateToneState();
        }
        AddAdditionalEditors(effect.Source);
    }
    private PresetEffectBlock? EditableNoiseKiller()
    {
        if(currentSnapshot?.Blocks.FirstOrDefault() is not {} block || block.Position!=1 || block.TypeCode!=4 ||
            block.Values.Count!=6 || block.Values[0]!=1 || block.Values[1]!=2 || block.Values[2] is <25 or >28 ||
            block.Values[3] is <47 or >50 || block.Values[4]!=0 || block.Values[5]!=0) return null;
        return block;
    }
    private void InvalidateEditTarget()
    {
        if(editSnapshotValid && operation is null)status.Text="Target changed · Read the amplifier before editing.";
        editSnapshotValid=false;UpdateThresholdState();
    }
    private bool TryPendingThreshold(out ushort value)=>ushort.TryParse(thresholdInput.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) && value is >=25 and <=28;
    private void UpdateThresholdState()
    {
        var block=EditableNoiseKiller();
        var supported=!preview && editSnapshotValid && snapshotAddress.HasValue && snapshotFirmware=="V1.1.9" && block is not null;
        thresholdInput.IsEnabled=supported && operation is null;
        applyThreshold.IsEnabled=supported && operation is null && TryPendingThreshold(out var value) && value!=block!.Values[2];
        thresholdMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            !supported?"Read a supported V1.1.9 preset before applying a threshold.":
            !TryPendingThreshold(out value)?"Enter a whole number from 25 to 28.":
            value==block!.Values[2]?$"Matches confirmed snapshot · Level stays at {block.Values[3]}.":$"Pending {value} · Confirmed {block.Values[2]} · Apply reconnects and checks the preset. Nothing is saved.";
        UpdatePresetSelectionState();
        UpdateToneState();
        UpdateAdditionalEditorStates();
        UpdateSnapshotArchiveState();
        UpdateSaveState();
    }
    private PresetEffectBlock? Editable808Tone()
    {
        var block=currentSnapshot?.Blocks.FirstOrDefault(b=>b.Position==2);
        return IsSupported808ToneBlock(block)?block:null;
    }
    private static bool IsSupported808ToneBlock(PresetEffectBlock? block)=>block is not null && block.Position==2 && block.TypeCode==1 &&
        block.Values.Count==5 && block.Values[0]==1 && block.Values[1]==3 && block.Values[2] is >=36 and <=39 &&
        block.Values[3] is >=97 and <=100 && block.Values[4] is >=97 and <=100;
    private bool TryPendingTone(out ushort value)=>ushort.TryParse(toneInput.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) && value is >=97 and <=100;
    private void UpdateToneState()
    {
        var block=Editable808Tone();
        var supported=!preview && editSnapshotValid && snapshotAddress.HasValue && snapshotFirmware=="V1.1.9" && block is not null;
        toneInput.IsEnabled=supported && operation is null;
        applyTone.IsEnabled=supported && operation is null && TryPendingTone(out var value) && value!=block!.Values[3];
        toneMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            !supported?"Read a supported V1.1.9 OD808 preset before applying Tone.":!TryPendingTone(out value)?"Enter a whole number from 97 to 100.":
            value==block!.Values[3]?$"Matches confirmed snapshot · Gain stays at {block.Values[2]} and Vol at {block.Values[4]}.":
            $"Pending {value} · Confirmed {block.Values[3]} · Apply reconnects and checks the preset. Nothing is saved.";
    }
    private async Task ApplyToneAsync()
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" || snapshotAddress is not {} target ||
            currentSnapshot is not {} expected || Editable808Tone() is not {} block || !TryPendingTone(out var value) || value==block.Values[3])return;
        Begin("Checking amplifier before applying OD808 Tone…");snapshot.Text="Previous snapshot · Connecting for explicit live edit…";
        try {
            CurrentPresetSnapshot confirmed;
            await using(var session=await F15ReadSession.ConnectAsync(target,operation!.Token,diagnostics))
                confirmed=await session.Set808ToneAsync(expected,value,operation.Token);
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"Index {confirmed.WireIndex} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            var toneEffect=effectButtons.FirstOrDefault(item=>item.Effect.Source.Position==2 && item.Effect.Source.TypeCode==1).Effect;
            if(toneEffect is not null)SelectEffect(toneEffect);
            snapshot.Text=SnapshotLabel();status.Text=liveEditUnsaved?$"Live unsaved edit verified · OD808 Tone {value}":
                $"OD808 Tone {value} verified · Matches original snapshot · Nothing saved";
            lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {editSnapshotValid=false;status.Text="Tone edit interrupted · Outcome may be unknown. Read the amplifier before another edit.";snapshot.Text=SnapshotLabel();}
        catch(Exception) {editSnapshotValid=false;status.Text="Tone edit not confirmed. Read the amplifier before another edit. Nothing was saved.";snapshot.Text=SnapshotLabel();}
        finally {status.Foreground=MutedBrush;End();}
    }
    private void UpdatePresetSelectionState()
    {
        var selected=presets.SelectedItem as PresetName;
        var capturedConfiguration=IsCapturedPresetConfiguration(currentSnapshot);
        var supported=!preview && operation is null && editSnapshotValid && !liveEditUnsaved && snapshotAddress.HasValue &&
            snapshotFirmware=="V1.1.9" && capturedConfiguration && currentSnapshot?.WireIndex is 4 or 5 && selected?.WireIndex is 4 or 5;
        selectPreset.IsEnabled=supported && selected!.WireIndex!=currentSnapshot!.WireIndex;
        selectionMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            liveEditUnsaved?"Unavailable while live edits differ from the last read or saved snapshot.":
            !editSnapshotValid||snapshotFirmware!="V1.1.9"||currentSnapshot?.WireIndex is not (4 or 5)?"Read a V1.1.9 snapshot at index 4 or 5 first.":
            !capturedConfiguration?"Preset differs from the captured configuration · Selection disabled.":
            selected?.WireIndex is not (4 or 5)?"Choose index 4 or 5 in the list.":
            selected.WireIndex==currentSnapshot.WireIndex?"Already the current preset.":$"Pending recall · Index {selected.WireIndex} · Nothing is saved.";
    }
    private static bool IsCapturedPresetConfiguration(CurrentPresetSnapshot? current)
    {
        if(current is null)return false;
        try {
            PresetSelectionCommand.RequireObservedPreset(current.WireIndex,Convert.FromHexString(current.RawDetailPayloadHex));
            return true;
        }
        catch(NotSupportedException) {return false;}
        catch(FormatException) {return false;}
    }
    private async Task SelectPresetAsync()
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || liveEditUnsaved || snapshotFirmware!="V1.1.9" ||
            snapshotAddress is not {} target || currentSnapshot is not {} expected || expected.WireIndex is not (4 or 5) ||
            presets.SelectedItem is not PresetName selected || selected.WireIndex is not (4 or 5) || selected.WireIndex==expected.WireIndex ||
            !IsCapturedPresetConfiguration(expected)) return;
        Begin("Checking amplifier before explicit preset recall…");
        snapshot.Text="Previous snapshot · Connecting for explicit preset recall…";
        try {
            CurrentPresetSnapshot confirmed;
            await using(var session=await F15ReadSession.ConnectAsync(target,operation!.Token,diagnostics))
                confirmed=await session.SelectObservedPresetAsync(expected,(byte)selected.WireIndex,operation.Token);
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=false;originalSnapshotDetail=confirmed.RawDetailPayloadHex;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"Index {confirmed.WireIndex} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            snapshot.Text=SnapshotLabel();status.Text=$"Preset recall verified · Index {confirmed.WireIndex} · Nothing saved";
            lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {
            editSnapshotValid=false;status.Text="Preset recall interrupted · Outcome may be unknown. Read the amplifier before another operation.";snapshot.Text=SnapshotLabel();
        }
        catch(Exception) {
            editSnapshotValid=false;status.Text="Preset recall not confirmed. Read the amplifier before another operation. Nothing was saved.";snapshot.Text=SnapshotLabel();
        }
        finally {status.Foreground=MutedBrush;End();}
    }
    private async Task ApplyThresholdAsync()
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" ||
            snapshotAddress is not {} target || currentSnapshot is not {} expected || EditableNoiseKiller() is not {} block ||
            !TryPendingThreshold(out var value) || value==block.Values[2]) return;
        Begin("Checking amplifier before applying threshold…");
        snapshot.Text="Previous snapshot · Connecting for explicit live edit…";
        try {
            CurrentPresetSnapshot confirmed;
            await using(var session=await F15ReadSession.ConnectAsync(target,operation!.Token,diagnostics))
                confirmed=await session.SetNoiseKillerThresholdAsync(expected,value,operation.Token);
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"Index {confirmed.WireIndex} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            snapshot.Text=SnapshotLabel();status.Text=liveEditUnsaved?$"Live unsaved edit verified · Threshold {value}":
                $"Threshold {value} verified · Matches original snapshot · Nothing saved";
            lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {
            editSnapshotValid=false;status.Text="Edit interrupted · Outcome may be unknown. Read the amplifier before another edit.";snapshot.Text=SnapshotLabel();
        }
        catch(Exception) {
            editSnapshotValid=false;status.Text="Edit not confirmed. The preset may have changed; read the amplifier before another edit. Nothing was saved.";snapshot.Text=SnapshotLabel();
        }
        finally {status.Foreground=MutedBrush;End();}
    }
    private static Brush Brush(string hex)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    private static TextBlock Text(string text,double size,Brush? color=null,Thickness? margin=null)=>new(){Text=text,FontSize=size,Foreground=color??Brushes.White,Margin=margin??new Thickness(0),TextWrapping=TextWrapping.Wrap};
    private static Button Button(string text)
    {
        var button=new Button{Content=text,Padding=new Thickness(14,8,14,8),Background=Brush("#343B46"),Foreground=Brushes.White,BorderBrush=Brushes.Transparent,BorderThickness=new Thickness(2),Cursor=System.Windows.Input.Cursors.Hand,HorizontalContentAlignment=HorizontalAlignment.Center};
        button.Template=(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="Button">
              <Border x:Name="Surface" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="5" Padding="{TemplateBinding Padding}">
                <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center"/>
              </Border>
              <ControlTemplate.Triggers>
                <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Surface" Property="Opacity" Value="0.45"/></Trigger>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Opacity" Value="0.8"/></Trigger>
                <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#4C9EFF"/><Setter TargetName="Surface" Property="BorderThickness" Value="2"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        return button;
    }
}
