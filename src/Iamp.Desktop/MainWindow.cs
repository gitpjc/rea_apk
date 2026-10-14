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
    private readonly Button clearSearch = Button("×");
    private readonly ComboBox presets = new() { Background = PanelBrush, Foreground = Brushes.White, HorizontalContentAlignment = HorizontalAlignment.Stretch, MaxDropDownHeight = 360 };
    private bool cancelPresetDropdown;
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
        Title = "iAMP Desktop · Amplifier Settings"; Width = Math.Min(1280,SystemParameters.WorkArea.Width); Height = Math.Min(900,SystemParameters.WorkArea.Height); MinWidth = 620; MinHeight = 320;
        Background = BackgroundBrush; Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var root = new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(212) });
        root.ColumnDefinitions.Add(new ColumnDefinition()); Content = root;
        root.Children.Add(BuildSidebar());
        var scroll = libraryScroll;
        Grid.SetColumn(scroll, 1); root.Children.Add(scroll);
        Grid.SetColumn(historyScroll,1);root.Children.Add(historyScroll);
        Grid.SetColumn(informationScroll,1);root.Children.Add(informationScroll);
        var page = new StackPanel { Margin = new Thickness(32, 20, 32, 24) }; scroll.Content = page;
        var header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});header.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        var titles = new StackPanel(); titles.Children.Add(Text("Amplifier Settings", 30));
        titles.Children.Add(Text("Choose presets and adjust your amplifier’s effects.", 14, MutedBrush, new Thickness(0,7,0,0)));
        header.Children.Add(titles);
        var badge = new Border { Background = Brush("#18304D"), CornerRadius = new CornerRadius(6), Padding = new Thickness(13,7,13,7), VerticalAlignment = VerticalAlignment.Top,
            Child = sourceBadge }; Grid.SetColumn(badge,1); header.Children.Add(badge); page.Children.Add(header);
        page.Children.Add(BuildConnection());
        var cardGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns=4, Margin = new Thickness(0,14,0,0) };
        var values = new[] { Card("FIRMWARE", firmware, "Reported by amplifier"), Card("PRESET SLOTS", presetCount, "Names read from your amp"),
            Card("GIR / GNR SLOTS", libraryCount, "Capture library capacity"), Card("DRUMS / LOOPER", rhythmCount, "Patterns and file slots") };
        for (var i=0;i<values.Length;i++) { values[i].Margin=new Thickness(0,0,12,12); cardGrid.Children.Add(values[i]); }
        var informationPage=new StackPanel {Margin=new Thickness(32,20,32,24)};
        informationPage.Children.Add(Text("Amplifier Information",30));
        informationPage.Children.Add(Text("Firmware and capacities from the latest amplifier read. Connect or refresh from Amplifier Settings.",14,MutedBrush,new Thickness(0,7,0,12)));
        var informationStatus=Text("",13,MutedBrush);
        informationStatus.SetBinding(TextBlock.TextProperty,new Binding("Text") {Source=snapshot});informationPage.Children.Add(informationStatus);
        informationPage.Children.Add(cardGrid);informationScroll.Content=informationPage;
        informationPage.SizeChanged+=(_,_)=>cardGrid.Columns=informationPage.ActualWidth<380?1:informationPage.ActualWidth<760?2:4;
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
        var bar = new Grid(); bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition());bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        var label = new StackPanel(); label.Children.Add(Text("Presets", 22)); label.Children.Add(count); bar.Children.Add(label);
        var searchPanel = new StackPanel(); searchPanel.Children.Add(Text("Search name or preset", 12, MutedBrush));
        var searchField = new Grid(); searchField.Children.Add(search);
        clearSearch.Width=24;clearSearch.Height=26;clearSearch.Padding=new Thickness(0);
        clearSearch.Margin=new Thickness(0,0,6,0);clearSearch.HorizontalAlignment=HorizontalAlignment.Right;
        clearSearch.VerticalAlignment=VerticalAlignment.Center;clearSearch.Background=Brushes.Transparent;
        clearSearch.Visibility=Visibility.Collapsed;clearSearch.ToolTip="Clear preset search";
        System.Windows.Automation.AutomationProperties.SetName(clearSearch,"Clear preset search");
        clearSearch.Click+=(_,_)=> {search.Clear();search.Focus();};searchField.Children.Add(clearSearch);
        searchPanel.Children.Add(searchField); Grid.SetColumn(searchPanel,2); bar.Children.Add(searchPanel); library.Children.Add(bar);
        var selectionActions=new WrapPanel {Margin=new Thickness(0,12,0,0)};
        selectionActions.Children.Add(selectPreset);selectionMessage.Margin=new Thickness(12,8,0,0);selectionActions.Children.Add(selectionMessage);
        library.Children.Add(selectionActions);
        library.Children.Add(Text("Choose a preset from the dropdown to recall it. Recall replaces the live preset without saving; unsaved changes may be discarded.",12,MutedBrush,new Thickness(0,8,0,0)));
        presets.ItemTemplate = (DataTemplate)XamlReader.Parse("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Grid Margin="2,5"><Grid.ColumnDefinitions><ColumnDefinition Width="26"/><ColumnDefinition Width="44"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                <TextBlock x:Name="CurrentTick" Text="✓" Foreground="#4385ff" FontSize="17" FontWeight="Bold" Visibility="Hidden" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1" Text="{Binding SlotLabel}" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="2" Text="{Binding Name}" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/>
              </Grid>
              <DataTemplate.Triggers><DataTrigger Binding="{Binding IsCurrent}" Value="True"><Setter TargetName="CurrentTick" Property="Visibility" Value="Visible"/></DataTrigger></DataTemplate.Triggers>
            </DataTemplate>
            """);
        presets.Template = (ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ComboBox">
              <Grid>
                <ToggleButton x:Name="DropdownToggle" Focusable="False" IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}" ClickMode="Press">
                  <ToggleButton.Template><ControlTemplate TargetType="ToggleButton">
                    <Border x:Name="Surface" Background="#20242B" BorderBrush="#46505E" BorderThickness="1" CornerRadius="5" Padding="10,5">
                      <Grid><Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="24"/></Grid.ColumnDefinitions><TextBlock Grid.Column="1" Text="▾" Foreground="White" HorizontalAlignment="Center" VerticalAlignment="Center"/></Grid>
                    </Border>
                    <ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#4385ff"/></Trigger></ControlTemplate.Triggers>
                  </ControlTemplate></ToggleButton.Template>
                </ToggleButton>
                <ContentPresenter x:Name="SelectedPresetPresenter" Margin="12,2,36,2" IsHitTestVisible="False" Content="{TemplateBinding SelectionBoxItem}" HorizontalAlignment="Stretch" VerticalAlignment="Center">
                  <ContentPresenter.ContentTemplate>
                    <DataTemplate>
                      <Grid Margin="2,5"><Grid.ColumnDefinitions><ColumnDefinition Width="44"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                        <TextBlock Text="{Binding SlotLabel}" VerticalAlignment="Center"/>
                        <TextBlock Grid.Column="1" Text="{Binding Name}" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/>
                      </Grid>
                    </DataTemplate>
                  </ContentPresenter.ContentTemplate>
                </ContentPresenter>
                <Popup x:Name="PART_Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False" PopupAnimation="Fade">
                  <Border Background="#20242B" BorderBrush="#46505E" BorderThickness="1" MinWidth="{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}" MaxHeight="{TemplateBinding MaxDropDownHeight}">
                    <ScrollViewer CanContentScroll="True" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/></ScrollViewer>
                  </Border>
                </Popup>
              </Grid>
              <ControlTemplate.Triggers>
                <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="DropdownToggle" Property="BorderBrush" Value="#4385ff"/></Trigger>
                <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.55"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        var rowStyle = new Style(typeof(ComboBoxItem));
        rowStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch));
        rowStyle.Setters.Add(new Setter(Control.ForegroundProperty,Brushes.White));
        rowStyle.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ComboBoxItem">
              <Border x:Name="Row" Background="Transparent" Padding="10,2"><ContentPresenter HorizontalAlignment="Stretch"/></Border>
              <ControlTemplate.Triggers><Trigger Property="IsHighlighted" Value="True"><Setter TargetName="Row" Property="Background" Value="#34485F"/></Trigger></ControlTemplate.Triggers>
            </ControlTemplate>
            """)));
        presets.ItemContainerStyle=rowStyle;
        presets.MinHeight=42;presets.Width=440;presets.HorizontalAlignment=HorizontalAlignment.Left;
        presets.VerticalAlignment=VerticalAlignment.Center;presets.Margin=new Thickness(16,0,20,0);
        Grid.SetColumn(presets,1);bar.Children.Add(presets);
        System.Windows.Automation.AutomationProperties.SetName(presets,"Choose amplifier preset");
        page.SizeChanged+=(_,_)=> {
            var available=Math.Max(120,page.ActualWidth);
            var compact=available<620;
            devices.Width=Math.Min(360,Math.Max(120,available-36));search.Width=Math.Min(280,available);
            Grid.SetRow(badge,compact?1:0);Grid.SetColumn(badge,compact?0:1);Grid.SetColumnSpan(badge,compact?2:1);
            badge.HorizontalAlignment=compact?HorizontalAlignment.Left:HorizontalAlignment.Right;
            badge.Margin=new Thickness(0,compact?10:0,0,0);
            var wrapSearch=available<760;var wrapPreset=available<420;
            Grid.SetRow(presets,wrapPreset?1:0);Grid.SetColumn(presets,wrapPreset?0:1);Grid.SetColumnSpan(presets,wrapPreset?3:1);
            presets.Margin=new Thickness(wrapPreset?0:16,wrapPreset?10:0,wrapSearch?0:20,0);
            presets.Width=Math.Min(440,Math.Max(120,available-(wrapPreset?0:Math.Max(100,label.ActualWidth)+16)-(wrapSearch?0:300)));
            Grid.SetRow(searchPanel,wrapPreset?2:wrapSearch?1:0);Grid.SetColumn(searchPanel,wrapSearch?0:2);Grid.SetColumnSpan(searchPanel,wrapSearch?3:1);
            searchPanel.Margin=new Thickness(0,wrapSearch?10:0,0,0);
            searchPanel.HorizontalAlignment=wrapSearch?HorizontalAlignment.Left:HorizontalAlignment.Right;

        };
        library.Children.Add(Text("Preset banks match iAMP: 0A–0D, 1A–1D … 19A–19D. A blue check on the left marks the active amplifier preset.",12,MutedBrush,new Thickness(0,10,0,0)));
        page.Children.Insert(2,library);
        page.Children.Add(Text("V1.1.9: preset recall across 0A–19D and five bounded live controls. Saving remains restricted to 0D British Lead Solo with Threshold 25 or 26 and every other original value unchanged. Broader saving, renaming and effect editing remain unavailable.",13,MutedBrush,new Thickness(0,24,0,0)));
        InitializeAdditionalEditors();InitializeSnapshotArchive();InitializeDiagnostics();InitializeComparison();StyleInputs(); cancel.IsEnabled=false;
        search.Padding=new Thickness(9,7,34,7);
        scan.Click += async (_,_) => await ScanAsync(); read.Click += async (_,_) => await ReadAsync(); cancel.Click += (_,_) => operation?.Cancel();
        search.TextChanged += (_,_) => {clearSearch.Visibility=search.Text.Length>0?Visibility.Visible:Visibility.Collapsed;Filter();};
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
        InitializeKeyboardNavigation();InitializeConnection();
        presets.DropDownOpened+=(_,_)=>cancelPresetDropdown=false;
        presets.DropDownClosed+=async(_,_)=> {
            if(!cancelPresetDropdown)await RecallDropdownSelectionAsync();
            else ResetDropdownToCurrent();
        };
        presets.PreviewKeyDown+=(_,e)=> {
            if(e.Key==System.Windows.Input.Key.Escape && presets.IsDropDownOpen)cancelPresetDropdown=true;
        };
        presets.KeyUp+=async(_,e)=> {
            if(!presets.IsDropDownOpen && e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Up or System.Windows.Input.Key.Down) {
                e.Handled=true;await RecallDropdownSelectionAsync();
            }
        };
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
        side.Children.Add(libraryNavigation);side.Children.Add(informationNavigation);side.Children.Add(historyNavigation);
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
        panel.Children.Add(Text("Turn on the amp and disconnect phone iAMP before connecting. Use one control app at a time.",13,MutedBrush,new Thickness(0,4,0,10)));
        var row = new WrapPanel(); row.Children.Add(devices); scan.Margin=new Thickness(12,0,0,0); row.Children.Add(scan); panel.Children.Add(row);
        var manual = new WrapPanel { Margin=new Thickness(0,7,0,0) }; manual.Children.Add(Text("Bluetooth address",12,MutedBrush,new Thickness(0,8,12,0)));
        manual.Children.Add(address); address.ToolTip="12 hexadecimal digits, with optional colons or hyphens";
        manual.Children.Add(Text("Optional · overrides selection",12,MutedBrush,new Thickness(12,8,0,0)));
        panel.Children.Add(new Expander {Header="Manual Bluetooth address",Foreground=MutedBrush,Margin=new Thickness(0,8,0,0),Content=manual});
        var actions = new WrapPanel { Margin=new Thickness(0,12,0,0) }; read.Background=BlueBrush; read.Foreground=Brush("#101C2B");
        actions.Children.Add(disconnect);
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
        UpdateComparisonState();UpdateConnectionControls();
        UpdateThresholdState();
    }
    private void End()
    {
        operation?.Dispose(); operation=null; scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=!preview;cancel.IsEnabled=false;
        UpdateDiagnosticsState();
        UpdateComparisonState();UpdateConnectionControls();
        UpdateThresholdState();
    }
    private async Task ScanAsync()
    {
        Begin("Scanning Bluetooth…");
        try { await ReleaseConnectionAsync();editSnapshotValid=false;var found=await F15DeviceDiscovery.ScanAsync(operation!.Token); devices.ItemsSource=found; if(found.Count>0)devices.SelectedIndex=0;
            status.Text=found.Count>0?$"Found {found.Count} amplifier(s)":"No amplifier found. Scan again or enter an address."; }
        catch(OperationCanceledException){status.Text="Scan cancelled";}
        catch(Exception ex){status.Text=$"Scan failed: {ex.Message}";}
        finally { status.Foreground=MutedBrush; End(); }
        if(!preview && devices.Items.Count==1 && string.IsNullOrWhiteSpace(address.Text))await ReadAsync();
    }
    private async Task ReadAsync()
    {
        if(operation is not null || preview)return;
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
            await ReleaseConnectionAsync();
            F15Identity identity; IReadOnlyList<PresetName> loaded; CurrentPresetSnapshot current;
            {
                var session=await F15ReadSession.ConnectAsync(selected,operation!.Token,diagnostics);
                if(closing){await session.DisposeAsync();throw new OperationCanceledException();}
                connectedSession=session;connectedAddress=selected;
                status.Text="Reading amplifier information…"; identity=await session.ReadIdentityAsync(operation.Token);
                status.Text="Reading preset names…"; loaded=await session.ReadPresetNamesAsync(operation.Token);
                status.Text="Reading current preset…";current=await session.ReadCurrentPresetAsync(operation.Token);
            }
            firmware.Text=identity.Firmware;presetCount.Text=identity.PresetSlots.ToString();libraryCount.Text=$"{identity.GirSlots} / {identity.GnrSlots}";
            rhythmCount.Text=$"{identity.DrumPatterns} / {identity.LooperFiles}"; names=loaded;Filter();
            currentPreset.Text=$"{current.SlotLabel} · {current.Name}";
            liveEditUnsaved=liveEditUnsaved && snapshotAddress==selected && currentSnapshot?.WireIndex==current.WireIndex;
            if(!liveEditUnsaved)originalSnapshotDetail=current.RawDetailPayloadHex;
            liveEditUnsaved=current.RawDetailPayloadHex!=originalSnapshotDetail;
            currentSnapshot=current;snapshotAddress=selected;snapshotFirmware=identity.Firmware;editSnapshotValid=true;
            localSnapshotSource=false;sourceBadge.Text="EXPERIMENTAL EDIT";
            SetEffects(current.Blocks);
            snapshotTime=DateTimeOffset.Now;snapshot.Text=SnapshotLabel();Filter();status.Text="Connected · Presets loaded";lastReadSucceeded=true;refreshTimer.Start();
        }
        catch(OperationCanceledException){status.Text="Read cancelled";snapshot.Text=SnapshotLabel();}
        catch(Exception ex){status.Text=ReadError(ex);snapshot.Text=SnapshotLabel();}
        finally {if(!lastReadSucceeded)await ReleaseConnectionAsync();status.Foreground=MutedBrush;End();}
    }
    internal async Task<bool> ReadSnapshotPreviewAsync(string bluetoothAddress)
    {
        address.Text=bluetoothAddress;
        await ReadAsync();
        await ReleaseConnectionAsync();snapshot.Text=SnapshotLabel();
        return lastReadSucceeded;
    }
    private string SnapshotLabel()=>snapshotTime is {} time ? localSnapshotSource?
        $"Local snapshot · {time:dd MMM yyyy, HH:mm:ss zzz} · Offline · No amplifier changes":
        $"Snapshot · {time:dd MMM yyyy, HH:mm:ss zzz} · {(connectedSession is null?"Disconnected":"Connected")}{(liveEditUnsaved?" · Live edit verified; not saved":"")}" : "No snapshot · Disconnected";
    private static string ReadError(Exception error)=>error switch {
        TransferStateChangedException => "The amplifier reported a transfer-state change. This session was stopped; reconnect only when the amplifier is stable.",
        TimeoutException => "No response. Disconnect the phone app and try reading again.",
        FormatException => "This amplifier’s response is not supported by this version.",
        _ => "Could not read the amplifier. Check Bluetooth, the address and the phone app connection."
    };
    private void Filter()
    {
        var selectedIndex=(presets.SelectedItem as PresetName)?.WireIndex;
        var term=search.Text.Trim();var filtered=names.Where(p=>p.Name.Contains(term,StringComparison.OrdinalIgnoreCase)||p.SlotLabel.Contains(term,StringComparison.OrdinalIgnoreCase)||p.WireIndex.ToString().Contains(term)).Select(p=>p with {IsCurrent=p.WireIndex==currentSnapshot?.WireIndex}).ToArray();
        presets.ItemsSource=filtered;presets.SelectedItem=filtered.FirstOrDefault(p=>p.WireIndex==(selectedIndex??currentSnapshot?.WireIndex));count.Text=$"{filtered.Length} of {names.Count} presets";
    }
    private void ResetDropdownToCurrent()
    {
        presets.SelectedItem=presets.Items.OfType<PresetName>().FirstOrDefault(p=>p.WireIndex==currentSnapshot?.WireIndex);
    }
    private async Task RecallDropdownSelectionAsync()
    {
        if(preview)return;
        await SelectPresetAsync();
        // A declined or failed recall must not present the requested slot as active.
        ResetDropdownToCurrent();
    }
    private void SetDemo()
    {
        firmware.Text="Demo";presetCount.Text="80";libraryCount.Text="10 / 10";rhythmCount.Text="70 / 10";
        currentPreset.Text="0C · Demo · Lead tone";
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
            button.Content=EffectButtonContent(effect);
            button.MinWidth=100;button.Margin=new Thickness(0,0,10,8);button.Background=Brush("#292D33");button.Foreground=Brushes.White;
            button.BorderThickness=new Thickness(0,0,0,3);button.BorderBrush=Brushes.Transparent;
            button.ToolTip=$"View {effect.Category} details · {effect.Enabled switch {true=>"Enabled",false=>"Disabled",_=>"State unknown"}}";button.Click+=(_,_)=>SelectEffect(effect);
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
        effectDetails.Children.Clear();effectDetails.Children.Add(Text(effect.Title,22,effect.Enabled==false?MutedBrush:Brush(effect.Color)));
        AddEffectPowerControl(effect);
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
        UpdateEffectPowerState();
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
            confirmed=await UseSessionAsync(target,session=>session.Set808ToneAsync(expected,value,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffects(confirmed.Blocks);
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
        presets.IsEnabled=operation is null && names.Count>0;

        var supported=!preview && operation is null && editSnapshotValid && snapshotAddress.HasValue &&
            snapshotFirmware=="V1.1.9" && currentSnapshot?.WireIndex is >=1 and <=80 && selected?.WireIndex is >=1 and <=80;
        selectPreset.IsEnabled=supported && selected!.WireIndex!=currentSnapshot!.WireIndex;
        selectionMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            liveEditUnsaved?"Recall will discard live unsaved edits only after confirmation.":
            !editSnapshotValid||snapshotFirmware!="V1.1.9"?"Connect to load the presets first.":
            selected is null?"Choose a preset from the dropdown.":
            selected.WireIndex==currentSnapshot!.WireIndex?"Already the current preset.":$"Recall {selected.SlotLabel} · Nothing is saved.";
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
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" ||
            snapshotAddress is not {} target || currentSnapshot is not {} expected ||
            presets.SelectedItem is not PresetName selected || selected.WireIndex==expected.WireIndex) return;
        if((liveEditUnsaved || HasPendingInputs()) && MessageBox.Show(this,"Discard live unsaved edits and pending inputs, and recall the selected preset?","Recall preset",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
        Begin("Checking amplifier before explicit preset recall…");
        snapshot.Text="Previous snapshot · Connecting for explicit preset recall…";
        try {
            CurrentPresetSnapshot confirmed;
            confirmed=await UseSessionAsync(target,session=>session.SelectPresetAsync(expected,(byte)selected.WireIndex,operation!.Token));
            if(confirmed.Name!=selected.Name)throw new System.IO.IOException("Preset name differs from the loaded list.");
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=false;originalSnapshotDetail=confirmed.RawDetailPayloadHex;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffects(confirmed.Blocks);
            Filter();snapshot.Text=SnapshotLabel();status.Text=$"Preset recall verified · {confirmed.SlotLabel} · Nothing saved";
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
            confirmed=await UseSessionAsync(target,session=>session.SetNoiseKillerThresholdAsync(expected,value,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffects(confirmed.Blocks);
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
