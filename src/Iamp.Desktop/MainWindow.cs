using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    private readonly TextBlock batterySummary=Text("Battery · Not read",14,MutedBrush,new Thickness(0,12,0,0));
    private DateTimeOffset? batteryReadAt;
    private readonly TextBlock currentPreset = Text("Read the amplifier to see its current preset.", 20);
    private readonly WrapPanel effectChain = new();
    private readonly StackPanel effectDetails = new();
    private readonly StackPanel effectSelectionDetails=new();
    private readonly WrapPanel effectHeading=new();
    private readonly ScrollViewer effectParameterScroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,MaxHeight=480};
    private readonly TextBox thresholdInput = new() { Width=90, MaxLength=3 };
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

    public MainWindow(bool preview = false):this(preview,new PresetNameCacheStore(),new DesktopSettingsStore()){}

    internal MainWindow(bool preview,PresetNameCacheStore nameCacheStore):this(preview,nameCacheStore,new DesktopSettingsStore()){}

    internal MainWindow(bool preview,PresetNameCacheStore nameCacheStore,DesktopSettingsStore settingsStore)
    {
        this.preview = preview;
        presetNameCacheStore=nameCacheStore;
        desktopSettingsStore=settingsStore;
        DesktopScrollbars.Install();
        Title = "iAMP Desktop · Presets"; Width = Math.Min(1280,SystemParameters.WorkArea.Width); Height = Math.Min(900,SystemParameters.WorkArea.Height); MinWidth = 620; MinHeight = 320;
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Iamp.Desktop;component/assets/app/iamp.ico"));
        Background = BackgroundBrush; Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var root = new Grid {Background=BackgroundBrush}; root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(212) });
        root.ColumnDefinitions.Add(new ColumnDefinition()); Content = root;
        root.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());
        sharedHeader=BuildConnection();Grid.SetColumn(sharedHeader,1);root.Children.Add(sharedHeader);
        root.SizeChanged+=(_,_)=>sharedHeader.MaxHeight=Math.Max(90,root.ActualHeight*0.5);
        root.Children.Add(BuildSidebar());
        Grid.SetRowSpan(root.Children[^1],2);
        var scroll = libraryScroll;
        Grid.SetColumn(scroll, 1); root.Children.Add(scroll);
        Grid.SetColumn(historyScroll,1);root.Children.Add(historyScroll);
        Grid.SetColumn(informationScroll,1);root.Children.Add(informationScroll);
        Grid.SetColumn(mixerScroll,1);root.Children.Add(mixerScroll);BuildMixerPage();
        Grid.SetColumn(tunerScroll,1);root.Children.Add(tunerScroll);BuildTunerPage();
        Grid.SetColumn(drumScroll,1);root.Children.Add(drumScroll);BuildDrumPage();
        Grid.SetColumn(footswitchScroll,1);root.Children.Add(footswitchScroll);BuildFootswitchPage();
        Grid.SetColumn(snapshotsScroll,1);root.Children.Add(snapshotsScroll);BuildSnapshotsPage();
        Grid.SetColumn(settingsScroll,1);root.Children.Add(settingsScroll);BuildSettingsPage();
        foreach(var contentPage in new[]{libraryScroll,historyScroll,informationScroll,mixerScroll,tunerScroll,drumScroll,footswitchScroll,snapshotsScroll,settingsScroll})Grid.SetRow(contentPage,1);
        var page = new StackPanel { Margin = new Thickness(32, 0, 32, 24) }; scroll.Content = page;
        var header = new Grid(); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});header.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        var titles = new WrapPanel();
        var presetsTitle=Text("Presets",30,margin:new Thickness(0,0,18,0));
        presetsTitle.VerticalAlignment=VerticalAlignment.Center;
        titles.Children.Add(presetsTitle);
        InitializeUnsavedChangesNotice(titles);
        header.Children.Add(titles);
        page.Children.Add(header);
        var cardGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns=4, Margin = new Thickness(0,14,0,0) };
        var values = new[] { Card("FIRMWARE", firmware, "Reported by amplifier"), Card("PRESET SLOTS", presetCount, "Names read from your amp"),
            Card("GIR / GNR SLOTS", libraryCount, "Capture library capacity"), Card("DRUMS / LOOPER", rhythmCount, "Patterns and file slots") };
        for (var i=0;i<values.Length;i++) { values[i].Margin=new Thickness(0,0,12,12); cardGrid.Children.Add(values[i]); }
        var informationPage=new StackPanel {Margin=new Thickness(32,0,32,24)};
        informationPage.Children.Add(Text("Amplifier Information",30));
        informationPage.Children.Add(Text("Firmware and capacities from the latest amplifier read. Connect or refresh using the amplifier header above.",14,MutedBrush,new Thickness(0,7,0,12)));
        var informationStatus=Text("",13,MutedBrush);
        informationStatus.SetBinding(TextBlock.TextProperty,new Binding("Text") {Source=snapshot});informationPage.Children.Add(informationStatus);
        informationPage.Children.Add(batterySummary);
        informationPage.Children.Add(cardGrid);informationScroll.Content=informationPage;
        informationPage.SizeChanged+=(_,_)=>cardGrid.Columns=informationPage.ActualWidth<380?1:informationPage.ActualWidth<760?2:4;
        var currentPanel=new StackPanel();
        currentPreset.Foreground=BlueBrush;
        var currentPresetRow=new WrapPanel();
        currentPreset.Margin=new Thickness(0,0,16,0);currentPreset.VerticalAlignment=VerticalAlignment.Center;currentPresetRow.Children.Add(currentPreset);
        savePreset.Margin=new Thickness(0);savePreset.HorizontalAlignment=HorizontalAlignment.Left;
        savePreset.Click+=async(_,_)=>await SavePresetAsync();
        var presetActions=new WrapPanel{VerticalAlignment=VerticalAlignment.Center};presetActions.Children.Add(savePreset);showPresetJson.Margin=new Thickness(12,0,0,0);presetActions.Children.Add(showPresetJson);currentPresetRow.Children.Add(presetActions);currentPanel.Children.Add(currentPresetRow);
        showPresetJson.Click+=(_,_)=>ShowPresetJson();
        foreach(var action in new[]{exportPreset,importPreset}){action.Margin=new Thickness(12,0,0,0);presetActions.Children.Add(action);}
        exportPreset.Click+=async(_,_)=>await ChooseExportPresetAsync();importPreset.Click+=async(_,_)=>await ChooseImportPresetAsync();
        currentPanel.Children.Add(saveMessage);
        BuildImportRecoveryPanel();currentPanel.Children.Add(importRecoveryPanel);
        page.Children.Add(new Border { Background=Brush("#1D2B3C"),CornerRadius=new CornerRadius(8),Padding=new Thickness(14),
            Margin=new Thickness(0,10,0,0),Child=currentPanel });
        var effects=new StackPanel();effects.Children.Add(Text("Preset effects",18));
        effects.Children.Add(Text("Drag effects to reorder, or use the move buttons. Use + to add an effect. Changes are live; nothing is saved automatically.",12,MutedBrush,new Thickness(0,0,0,10)));
        effects.Children.Add(CreateChainDragSurface());effectSelectionDetails.Margin=new Thickness(0,15,0,0);effects.Children.Add(effectSelectionDetails);
        effectParameterScroll.Content=effectDetails;effectSelectionDetails.Children.Add(effectParameterScroll);
        effectParameterScroll.CanContentScroll=false;
        effectParameterScroll.PreviewMouseWheel+=(_,e)=> {
            if(e.Delta==0)return;
            var lines=SystemParameters.WheelScrollLines;
            var distance=e.Delta/120d*(lines<0?effectParameterScroll.ViewportHeight:lines*16);
            effectParameterScroll.ScrollToVerticalOffset(effectParameterScroll.VerticalOffset-distance);e.Handled=true;
        };
        libraryScroll.LayoutUpdated+=(_,_)=> {
            if(libraryScroll.Visibility!=Visibility.Visible||libraryScroll.ViewportHeight<=0||effectParameterScroll.Parent is null)return;
            // Reserve the space actually occupied by the other page controls, rather than a percentage
            // of the whole page. The complete inner viewport must fit below those controls.
            var fixedHeight=page.DesiredSize.Height-effectParameterScroll.DesiredSize.Height;
            var available=Math.Max(96,Math.Floor(libraryScroll.ViewportHeight-fixedHeight-2));
            if(Math.Abs(effectParameterScroll.MaxHeight-available)>0.5)effectParameterScroll.MaxHeight=available;
        };
        effectDetails.Children.Add(Text("Read the amplifier to see its preset effects.",13,MutedBrush));
        page.Children.Add(new Border { Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(14),Margin=new Thickness(0,10,0,0),Child=effects });
        var library = new StackPanel();
        var bar = new Grid();
        bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});bar.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        var searchLabel=Text("Search name or preset",12,MutedBrush);
        var searchField = new Grid {MinHeight=42};search.VerticalAlignment=VerticalAlignment.Center;searchField.Children.Add(search);
        clearSearch.Width=24;clearSearch.Height=26;clearSearch.Padding=new Thickness(0);
        clearSearch.Margin=new Thickness(0,0,6,0);clearSearch.HorizontalAlignment=HorizontalAlignment.Right;
        clearSearch.VerticalAlignment=VerticalAlignment.Center;clearSearch.Background=Brushes.Transparent;
        clearSearch.Visibility=Visibility.Collapsed;clearSearch.ToolTip="Clear preset search";
        System.Windows.Automation.AutomationProperties.SetName(clearSearch,"Clear preset search");
        clearSearch.Click+=(_,_)=> {search.Clear();search.Focus();};searchField.Children.Add(clearSearch);
        var effectFilterPanel=new WrapPanel();
        var effectSelectorColumn=new StackPanel();effectSelectorColumn.Children.Add(Text("Presets using effect:",12,MutedBrush));effectSelectorColumn.Children.Add(effectFilter);effectFilterPanel.Children.Add(effectSelectorColumn);
        var effectActionColumn=new StackPanel{Margin=new Thickness(8,0,0,0),VerticalAlignment=VerticalAlignment.Bottom};
        effectCacheNote.TextWrapping=TextWrapping.Wrap;effectCacheNote.MaxWidth=240;effectCacheNote.Margin=new Thickness(12,0,0,0);effectCacheNote.VerticalAlignment=VerticalAlignment.Center;
        var effectFilterActions=new WrapPanel();
        effectFilterActions.Children.Add(resetEffectFilter);refreshEffectNames.Margin=new Thickness(8,0,0,0);effectFilterActions.Children.Add(refreshEffectNames);
        effectFilterActions.Children.Add(effectCacheNote);
        effectActionColumn.Children.Add(effectFilterActions);effectFilterPanel.Children.Add(effectActionColumn);
        var filtersPanel=new WrapPanel();filtersPanel.Children.Add(effectFilterPanel);
        Grid.SetRow(filtersPanel,1);bar.Children.Add(filtersPanel);library.Children.Add(bar);
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
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="clr-namespace:Iamp.Desktop;assembly=Iamp.Desktop" TargetType="ComboBox">
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
                <MultiTrigger><MultiTrigger.Conditions><Condition Property="IsEnabled" Value="False"/><Condition Property="local:SteadyControlAppearance.KeepEnabledAppearance" Value="False"/></MultiTrigger.Conditions><Setter Property="Opacity" Value="0.55"/></MultiTrigger>
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
        InitializeEffectFilter();
        presets.MinHeight=42;presets.Width=440;presets.HorizontalAlignment=HorizontalAlignment.Left;
        presets.VerticalAlignment=VerticalAlignment.Center;presets.Margin=new Thickness(0);
        var presetChooser=new Grid {HorizontalAlignment=HorizontalAlignment.Left};
        presetChooser.ColumnDefinitions.Add(new ColumnDefinition());presetChooser.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});presetChooser.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
        presetChooser.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});presetChooser.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});presetChooser.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        Grid.SetRow(presets,1);presetChooser.Children.Add(presets);Grid.SetColumn(searchLabel,1);presetChooser.Children.Add(searchLabel);Grid.SetColumn(searchField,1);Grid.SetRow(searchField,1);presetChooser.Children.Add(searchField);Grid.SetColumn(presetEffectDetails,2);Grid.SetRow(presetEffectDetails,1);presetChooser.Children.Add(presetEffectDetails);
        presetEffectDetails.Width=180;presetEffectDetails.Margin=new Thickness(10,0,0,0);presetEffectDetails.VerticalAlignment=VerticalAlignment.Center;
        presetEffectDetails.Click+=(_,_)=> {var dialog=CreatePresetEffectDetailsDialog(CreatePresetEffectReport());dialog.Owner=this;dialog.ShowDialog();};
        System.Windows.Automation.AutomationProperties.SetName(presetEffectDetails,"Preset/Effect Details");
        bar.Children.Add(presetChooser);
        System.Windows.Automation.AutomationProperties.SetName(presets,"Choose amplifier preset");
        page.SizeChanged+=(_,_)=> {
            var available=Math.Max(120,page.ActualWidth);
            var compact=available<600;presetChooser.Width=Math.Min(924,available);
            search.Width=compact?Math.Min(280,available):Math.Min(280,Math.Max(180,(available-204)*0.4));
            Grid.SetRow(presets,compact?0:1);Grid.SetRow(presetEffectDetails,compact?0:1);Grid.SetColumnSpan(presets,compact?2:1);
            Grid.SetRow(searchLabel,compact?1:0);Grid.SetColumn(searchLabel,compact?0:1);Grid.SetColumnSpan(searchLabel,compact?3:1);
            Grid.SetRow(searchField,compact?2:1);Grid.SetColumn(searchField,compact?0:1);Grid.SetColumnSpan(searchField,compact?3:1);
            searchLabel.Margin=compact?new Thickness(0,10,0,0):new Thickness(12,0,12,0);
            searchField.HorizontalAlignment=HorizontalAlignment.Left;searchField.Margin=compact?new Thickness(0):new Thickness(12,0,12,0);
            presetEffectDetails.Margin=new Thickness(compact?10:0,0,0,0);
            presets.Width=compact?Math.Max(120,available-190):presetChooser.Width-search.Width-204;
            effectFilter.Width=Math.Min(280,available);
            filtersPanel.Width=effectFilterPanel.Width=available;filtersPanel.Margin=new Thickness(0,10,0,0);filtersPanel.HorizontalAlignment=HorizontalAlignment.Left;
            effectActionColumn.Margin=new Thickness(available<800?0:8,available<800?7:0,0,0);

        };
        page.Children.Insert(1,library);
        thresholdSlider=CreateParameterSlider(thresholdInput,"Noise Killer threshold",0,100,ApplyThresholdAsync);
        toneSlider=CreateParameterSlider(toneInput,"OD Tone",0,100,ApplyToneAsync);
        InitializeAdditionalEditors();InitializeSnapshotArchive();InitializeDiagnostics();InitializeComparison();StyleInputs(); cancel.IsEnabled=false;
        search.Padding=new Thickness(9,7,34,7);
        scan.Click += async (_,_) => await ScanAsync(); read.Click += async (_,_) => await ReadAsync(); cancel.Click += (_,_) => operation?.Cancel();
        search.TextChanged += (_,_) => {clearSearch.Visibility=search.Text.Length>0?Visibility.Visible:Visibility.Collapsed;Filter();};
        thresholdInput.TextChanged += (_,_) => UpdateThresholdState();
        applyThreshold.Click += async (_,_) => await ApplyThresholdAsync();
        toneInput.TextChanged += (_,_) => UpdateThresholdState();
        applyTone.Click += async (_,_) => await ApplyToneAsync();
        selectPreset.Click += async (_,_) => await SelectPresetAsync();
        presets.SelectionChanged += (_,_) => UpdatePresetSelectionState();
        address.TextChanged += (_,_) => InvalidateEditTarget();
        devices.SelectionChanged += (_,_) => InvalidateEditTarget();
        System.Windows.Automation.AutomationProperties.SetName(thresholdInput,"Pending Noise Killer threshold, 0 to 100");
        System.Windows.Automation.AutomationProperties.SetName(toneInput,"Pending OD Tone, 0 to 100");
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
        if (preview) SetDemo();else LoadCachedPresetNames();
        UpdatePresetSelectionState();
        InitializeWindowSettings();
    }

    private UIElement BuildSidebar()
    {
        var side = new StackPanel { Margin = new Thickness(22,28,18,20) };
        side.Children.Add(Text("iAMP",36)); side.Children.Add(Text("DESKTOP COMPANION",10,MutedBrush,new Thickness(0,2,0,0)));
        side.Children.Add(Text($"Version {DesktopVersion.Current}",12,MutedBrush,new Thickness(0,7,0,26)));
        ConfigureNavigation();
        side.Children.Add(libraryNavigation);side.Children.Add(informationNavigation);side.Children.Add(mixerNavigation);
        var toolsSection=new StackPanel {Margin=new Thickness(0,6,0,4)};
        System.Windows.Automation.AutomationProperties.SetName(toolsSection,"Tools");
        toolsSection.Children.Add(Text("Tools",13,MutedBrush,new Thickness(14,0,0,4)));
        drumNavigation.Padding=tunerNavigation.Padding=footswitchNavigation.Padding=new Thickness(28,7,14,7);
        toolsSection.Children.Add(tunerNavigation);
        toolsSection.Children.Add(drumNavigation);toolsSection.Children.Add(footswitchNavigation);side.Children.Add(toolsSection);
        side.Children.Add(historyNavigation);
        side.Children.Add(snapshotsNavigation);
        exportDiagnostics.Margin=new Thickness(0,18,0,0);
        exportDiagnostics.HorizontalAlignment=HorizontalAlignment.Stretch;
        side.Children.Add(exportDiagnostics);
        settingsNavigation.Margin=new Thickness(0,8,0,2);side.Children.Add(settingsNavigation);
        return new Border { Background=Brush("#1A1D22"), BorderBrush=Brush("#30353D"), BorderThickness=new Thickness(0,0,1,0), Child=new ScrollViewer {Content=side,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled} };
    }

    private FrameworkElement BuildConnection()
    {
        var panel = new StackPanel();
        amplifierHeading.FontWeight=FontWeights.Bold;
        var headingRow=new WrapPanel();headingRow.Children.Add(amplifierHeading);
        connectionProgressNotice.Children.Add(connectionProgress);
        connectionProgressNotice.Children.Add(connectionProgressLabel);
        System.Windows.Automation.AutomationProperties.SetName(connectionProgress,"Scanning, connecting and loading amplifier");
        headingRow.Children.Add(connectionProgressNotice);panel.Children.Add(headingRow);panel.Children.Add(amplifierPreset);
        read.Background=BlueBrush; read.Foreground=Brush("#101C2B");
        var row = new Grid {HorizontalAlignment=HorizontalAlignment.Left};
        row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(1,GridUnitType.Star),MinWidth=60,MaxWidth=360});row.Children.Add(devices);
        foreach(var button in new[]{scan,disconnect,read,cancel}){
            row.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});Grid.SetColumn(button,row.ColumnDefinitions.Count-1);
            button.Height=36;button.Margin=new Thickness(8,0,0,4);row.Children.Add(button);
        }
        devices.Width=double.NaN;
        devices.Height=36;devices.VerticalAlignment=VerticalAlignment.Top;devices.Margin=new Thickness(0,0,0,4);
        scan.ToolTip="Turn on the amp and disconnect phone iAMP before connecting. Use one control app at a time.";
        panel.Children.Add(new ScrollViewer {Content=row,Margin=new Thickness(0,8,0,0),HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled});
        var manual = new WrapPanel { Margin=new Thickness(0,7,0,0) }; manual.Children.Add(Text("Bluetooth address",12,MutedBrush,new Thickness(0,8,12,0)));
        manual.Children.Add(address); address.ToolTip="12 hexadecimal digits, with optional colons or hyphens";
        manual.Children.Add(Text("Optional · overrides selection",12,MutedBrush,new Thickness(12,8,0,0)));
        amplifierDiscovery.Content=manual;panel.Children.Add(amplifierDiscovery);
        void FitConnectionProgress()
        {
            connectionProgressLabel.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            connectionProgress.Width=Math.Min(120,Math.Max(40,panel.ActualWidth-20-connectionProgressLabel.DesiredSize.Width));
        }
        void FitConnectionRow()
        {
            var actionWidth=0d;
            foreach(var button in new[]{scan,disconnect,read,cancel}){
                button.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));actionWidth+=button.DesiredSize.Width;
            }
            row.Width=Math.Max(panel.ActualWidth,60+actionWidth);
        }
        panel.SizeChanged+=(_,_)=>{FitConnectionRow();FitConnectionProgress();};
        cancel.IsVisibleChanged+=(_,_)=>FitConnectionRow();read.SizeChanged+=(_,_)=>FitConnectionRow();
        connectionProgressLabel.SizeChanged+=(_,_)=>FitConnectionProgress();
        return new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,
            Content=new Border { Background=PanelBrush, CornerRadius=new CornerRadius(10), Padding=new Thickness(14), Margin=new Thickness(32,12,32,0), Child=panel }};
    }

    private static Border Card(string label,TextBlock value,string caption)
    {
        var stack=new StackPanel(); stack.Children.Add(Text(label,10,MutedBrush)); value.Margin=new Thickness(0,8,0,8); stack.Children.Add(value); stack.Children.Add(Text(caption,11,MutedBrush));
        return new Border { Background=PanelBrush, CornerRadius=new CornerRadius(8), Padding=new Thickness(13), Child=stack };
    }
    private void StyleInputs()
    {
        foreach(var box in new[]{address,search,thresholdInput,toneInput}.Concat(AdditionalEditors.Select(e=>e.Input))) { box.Background=Brush("#15191F");box.Foreground=Brushes.White;box.BorderBrush=Brush("#46505E");box.Padding=new Thickness(9,7,9,7);box.CaretBrush=Brushes.White; }
        devices.Padding=new Thickness(7,2,7,2); devices.Foreground=Brush("#15171B");
        devices.VerticalContentAlignment=VerticalAlignment.Center;devices.SnapsToDevicePixels=true;devices.UseLayoutRounding=true;
        devices.Template=(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="clr-namespace:Iamp.Desktop;assembly=Iamp.Desktop" TargetType="ComboBox">
              <Grid>
                <ToggleButton x:Name="SelectorToggle" BorderBrush="#ADADAD" Focusable="False" ClickMode="Press" IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}">
                  <ToggleButton.Template><ControlTemplate TargetType="ToggleButton">
                    <Border x:Name="SelectorSurface" Background="#ECECEC" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1">
                      <TextBlock Text="▾" Foreground="#333333" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,8,0"/>
                    </Border>
                    <ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="SelectorSurface" Property="Background" Value="#E0E8EF"/></Trigger></ControlTemplate.Triggers>
                  </ControlTemplate></ToggleButton.Template>
                </ToggleButton>
                <ContentPresenter x:Name="SelectedAmplifierPresenter" ClipToBounds="True" Margin="8,0,28,0" IsHitTestVisible="False" VerticalAlignment="Center" HorizontalAlignment="Left" Content="{Binding SelectedItem.DisplayName, RelativeSource={RelativeSource TemplatedParent}}"/>
                <Popup x:Name="PART_Popup" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False">
                  <Border Background="#F2F2F2" BorderBrush="#ADADAD" BorderThickness="1" MinWidth="{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}" MaxHeight="280">
                    <ScrollViewer CanContentScroll="True" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled"><ItemsPresenter/></ScrollViewer>
                  </Border>
                </Popup>
              </Grid>
              <ControlTemplate.Triggers>
                <MultiTrigger><MultiTrigger.Conditions><Condition Property="IsEnabled" Value="False"/><Condition Property="local:SteadyControlAppearance.KeepEnabledAppearance" Value="False"/></MultiTrigger.Conditions><Setter Property="Opacity" Value="0.55"/></MultiTrigger>
                <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="SelectorToggle" Property="BorderBrush" Value="#4C9EFF"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate>
            """);
    }
    private void Begin(string message)
    {
        operation=new CancellationTokenSource(TimeSpan.FromSeconds(75)); scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=false;
        cancel.IsEnabled=true; status.Text=message; status.Foreground=BlueBrush;
        UpdateDiagnosticsState();
        UpdateComparisonState();UpdateConnectionControls();
        UpdateThresholdState();
        UpdateEffectFilterState();
        UpdateTunerState();
        UpdateDrumState();
    }
    private void End()
    {
        connectionReadInProgress=false;
        if(!connectionScanInProgress){connectionProgress.IsIndeterminate=false;connectionProgressNotice.Visibility=Visibility.Collapsed;}
        operation?.Dispose(); operation=null; scan.IsEnabled=read.IsEnabled=devices.IsEnabled=address.IsEnabled=!preview;cancel.IsEnabled=false;
        FinishSteadyOperation();
        UpdateDiagnosticsState();
        UpdateComparisonState();UpdateConnectionControls();
        UpdateThresholdState();
        UpdateEffectFilterState();
        UpdateTunerState();
        UpdateDrumState();
    }
    private async Task ScanAsync()
    {
        if(tunerActive || tunerStarting || operation is not null || preview)return;
        BeginConnectionScan();
        try {
            if(drumReady&&!await LeaveDrumAsync()){status.Text=drumNotice??"Leave Drum before changing the amplifier connection.";return;}
            Begin("Scanning Bluetooth…");var connectAfterScan=false;
            try { await ReleaseConnectionAsync();editSnapshotValid=false;var found=await F15DeviceDiscovery.ScanAsync(operation!.Token); devices.ItemsSource=found; if(found.Count>0)devices.SelectedIndex=0;
                status.Text=found.Count>0?$"Found {found.Count} amplifier(s)":"No amplifier found. Scan again or enter an address.";
                connectAfterScan=found.Count==1&&string.IsNullOrWhiteSpace(address.Text); }
            catch(OperationCanceledException){status.Text="Scan cancelled";}
            catch(Exception ex){status.Text=$"Scan failed: {ex.Message}";}
            finally { status.Foreground=MutedBrush; End(); }
            if(connectAfterScan&&!preview)await ReadAsync();
        }
        finally {FinishConnectionScan();}
    }
    private async Task ReadAsync()
    {
        if(operation is not null || preview || tunerActive || tunerStarting)return;
        lastReadSucceeded=false;
        editSnapshotValid=false;UpdateThresholdState();
        var input=address.Text.Replace(":","").Replace("-","").Replace(" ","");
        ulong selected;
        if(input.Length>0) {
            if(input.Length!=12||!ulong.TryParse(input,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out selected)) {status.Text="Enter a Bluetooth address with 12 hexadecimal digits.";return;}
        } else if(devices.SelectedItem is F15DiscoveredDevice device) selected=device.Address;
        else {status.Text="Select an amplifier or enter its Bluetooth address.";return;}
        if(drumReady&&!await LeaveDrumAsync()){status.Text=drumNotice??"Leave Drum before refreshing the amplifier.";return;}
        presetSaveNotice=null;BeginConnectionRead(); snapshot.Text=names.Count>0?"Previous snapshot · Reading new data…":"Reading amplifier…";
        batterySummary.Text="Battery · Not read";batteryReadAt=null;
        try {
            await ReleaseConnectionAsync();
            F15Identity identity; IReadOnlyList<PresetName> loaded; CurrentPresetSnapshot current;
            IReadOnlyList<DecodedPreset> effectRecords=Array.Empty<DecodedPreset>();
            {
                var session=await F15ReadSession.ConnectAsync(selected,operation!.Token,diagnostics);
                if(closing){await session.DisposeAsync();throw new OperationCanceledException();}
                connectedSession=session;connectedAddress=selected;
                WatchFootswitchSession(session);
                status.Text="Reading amplifier information…"; identity=await session.ReadIdentityAsync(operation.Token);
                if(identity.Firmware=="V1.1.9"){
                    status.Text="Reading footswitch status…";await session.ReadFootswitchStatusAsync(operation.Token);
                }
                status.Text="Reading preset names…"; loaded=await session.ReadPresetNamesAsync(operation.Token);
                status.Text="Reading current preset…";current=await session.ReadCurrentPresetAsync(operation.Token);
                if(identity.Firmware=="V1.1.9") {
                    status.Text="Reading battery…";SetBatteryDisplay(await session.ReadBatteryAsync(operation.Token));
                    effectRecords=await LoadPresetEffectsAsync(session,current,selected,identity.Firmware,operation.Token);
                } else {batterySummary.Text="Battery · Not verified for this firmware";effectCacheNote.Text="Effect indexing is not verified for this firmware";}
            }
            firmware.Text=identity.Firmware;presetCount.Text=identity.PresetSlots.ToString();libraryCount.Text=$"{identity.GirSlots} / {identity.GnrSlots}";
            rhythmCount.Text=$"{identity.DrumPatterns} / {identity.LooperFiles}";names=loaded;SetPresetEffectIndex(effectRecords);Filter();
            currentPreset.Text=$"{current.SlotLabel} · {current.Name}";
            liveEditUnsaved=liveEditUnsaved && snapshotAddress==selected && currentSnapshot?.WireIndex==current.WireIndex;
            if(!liveEditUnsaved)originalSnapshotDetail=current.RawDetailPayloadHex;
            liveEditUnsaved=current.RawDetailPayloadHex!=originalSnapshotDetail;
            var preservePending=snapshotAddress==selected && snapshotFirmware==identity.Firmware &&
                currentSnapshot?.RawStatePayloadHex==current.RawStatePayloadHex && currentSnapshot.RawDetailPayloadHex==current.RawDetailPayloadHex;
            currentSnapshot=current;snapshotAddress=selected;snapshotFirmware=identity.Firmware;editSnapshotValid=true;
            UpdateMixerState(resetPending:!preservePending);
            localSnapshotSource=false;
            CacheLivePresetNames();
            if(preservePending)SetEffectsPreservingPending(current.Blocks);else SetEffects(current.Blocks);
            snapshotTime=DateTimeOffset.Now;snapshot.Text=SnapshotLabel();Filter();status.Text="Connected · Presets loaded";lastReadSucceeded=true;refreshTimer.Start();
        }
        catch(OperationCanceledException){status.Text="Read cancelled";snapshot.Text=SnapshotLabel();}
        catch(Exception ex){status.Text=ReadError(ex);snapshot.Text=SnapshotLabel();}
        finally {
            try {if(!lastReadSucceeded)await ReleaseConnectionAsync();}
            finally {status.Foreground=MutedBrush;End();UpdateImportRecoveryAfterRead(lastReadSucceeded,selected);}
        }
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
        $"Snapshot · {time:dd MMM yyyy, HH:mm:ss zzz} · {(connectedSession is null?"Disconnected":"Connected")}{(HasPresetStateChanges()?" · Live edit verified; not saved":"")}" : "No snapshot · Disconnected";
    private static string ReadError(Exception error)=>error switch {
        TransferStateChangedException => "The amplifier reported a transfer-state change. This session was stopped; reconnect only when the amplifier is stable.",
        CurrentPresetChangedException => "The amplifier kept changing while reading. Leave its controls still and try again.",
        TimeoutException => "No response. Disconnect the phone app and try reading again.",
        FormatException => "This amplifier’s response is not supported by this version.",
        _ => "Could not read the amplifier. Check Bluetooth, the address and the phone app connection."
    };
    private void Filter()
    {
        // Keep an in-progress popup choice; the closed field must follow confirmed amp state.
        var selectedIndex=presets.IsDropDownOpen?(presets.SelectedItem as PresetName)?.WireIndex:
            currentSnapshot?.WireIndex??(presets.SelectedItem as PresetName)?.WireIndex;
        var term=search.Text.Trim();var filtered=names.Where(p=>PresetEffectSearch.MatchesText(p.WireIndex,p.Name,term) && MatchesEffectFilter(p)).Select(p=>p with {IsCurrent=p.WireIndex==currentSnapshot?.WireIndex}).ToArray();
        presets.ItemsSource=filtered;presets.SelectedItem=filtered.FirstOrDefault(p=>p.WireIndex==(selectedIndex??currentSnapshot?.WireIndex));count.Text=$"{filtered.Length} of {names.Count} presets"+(presetNamesAreCached?" · Cached":"");
    }
    private void ResetDropdownToCurrent()
    {
        presets.SelectedItem=presets.Items.OfType<PresetName>().FirstOrDefault(p=>p.WireIndex==currentSnapshot?.WireIndex);
    }
    private async Task RecallDropdownSelectionAsync()
    {
        if(preview || presetNamesAreCached && !editSnapshotValid)return;
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
        UpdateIndexedCurrentEffects();
        initializedParameterPositions.Clear();
        pendingParameterValues.Clear();parameterTargets.Clear();
        displayedEffect=null;
        effectChain.Children.Clear();effectButtons.Clear();effectDetails.Children.Clear();effectSelectionDetails.Children.Clear();
        foreach(var block in blocks.Where(b=>b.TypeCode!=0)) {
            var effect=EffectPresentation.From(block);var button=Button(effect.Category);
            button.Content=EffectButtonContent(effect);
            button.MinWidth=100;button.Margin=new Thickness(0,0,10,8);button.Background=Brush("#292D33");button.Foreground=Brushes.White;
            button.BorderThickness=new Thickness(0,0,0,3);button.BorderBrush=Brushes.Transparent;
            button.ToolTip=$"View {effect.Category} details · {effect.Enabled switch {true=>"Enabled",false=>"Disabled",_=>"State unknown"}}";button.Click+=(_,_)=>SelectEffect(effect);
            effectButtons.Add((button,effect));effectChain.Children.Add(button);
            EnableEffectDragging(button,effect);
        }
        AddChainControls();
        if(effectButtons.Count>0)SelectEffect(effectButtons[0].Effect);
        else {effectSelectionDetails.Children.Add(effectParameterScroll);effectDetails.Children.Add(Text("No preset effect details available.",13,MutedBrush));}
    }
    private void SelectEffect(EffectPresentation effect)
    {
        CaptureParameterInputs();
        var wasRestoring=restoringParameterInputs;restoringParameterInputs=true;
        try {
        AssignParameterTarget(effect.Source);
        var initializeInputs=initializedParameterPositions.Add(effect.Source.Position);
        DetachSlider(thresholdSlider);DetachSlider(toneSlider);
        if(thresholdInput.Parent is Panel oldInputParent)oldInputParent.Children.Remove(thresholdInput);
        if(applyThreshold.Parent is Panel oldApplyParent)oldApplyParent.Children.Remove(applyThreshold);
        if(toneInput.Parent is Panel oldToneParent)oldToneParent.Children.Remove(toneInput);
        if(applyTone.Parent is Panel oldToneApplyParent)oldToneApplyParent.Children.Remove(applyTone);
        DetachAdditionalEditorControls();
        foreach(var item in effectButtons)item.Button.BorderBrush=ReferenceEquals(item.Effect,effect)?Brushes.White:Brushes.Transparent;
        effectDetails.Children.Clear();effectSelectionDetails.Children.Clear();effectHeading.Children.Clear();
        var effectTitle=Text(effect.Title,22,effect.Enabled==false?MutedBrush:Brush(effect.Color),new Thickness(0,0,16,4));effectTitle.VerticalAlignment=VerticalAlignment.Center;
        effectHeading.Children.Add(effectTitle);effectSelectionDetails.Children.Add(effectHeading);
        AddEffectPowerControl(effect);
        AddChainSelectionActions();
        effectSelectionDetails.Children.Add(effectParameterScroll);effectParameterScroll.ScrollToTop();
        if(effect.Parameters.Count==0) {effectDetails.Children.Add(Text("Parameter details not available in this version.",13,MutedBrush,new Thickness(0,8,0,0)));return;}
        if(effect.Source.TypeCode==4 && effect.Source.Values.Count==6 && effect.Source.Values[1]==2) {
            thresholdInput.Text=PendingParameterText(effect.Source,2);
            AddMobileParameterSlider(thresholdSlider,thresholdInput,"Threshold",effect.Color);
            UpdateThresholdState();
        }
        AddAdditionalEditors(effect.Source,initializeInputs);
        if(!effectDetails.Children.OfType<Slider>().Any()) {
            foreach(var parameter in effect.Parameters) {
                var row=new Grid {Margin=new Thickness(0,12,0,0),HorizontalAlignment=HorizontalAlignment.Left};
                row.SetBinding(FrameworkElement.WidthProperty,new Binding("ActualWidth"){Source=effectDetails,Converter=new HalfWidthConverter()});
                row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
                row.Children.Add(new TextBlock {Text=parameter.Name,FontFamily=new FontFamily("Segoe UI"),FontSize=14,FontWeight=FontWeights.ExtraBold,Foreground=Brushes.White,TextTrimming=TextTrimming.CharacterEllipsis});
                var value=Text(parameter.Value.ToString(CultureInfo.InvariantCulture),14);Grid.SetColumn(value,1);row.Children.Add(value);effectDetails.Children.Add(row);
            }
        }
        }
        finally {restoringParameterInputs=wasRestoring;if(!restoringParameterInputs)UpdateThresholdState();}
    }
    private PresetEffectBlock? EditableNoiseKiller()
    {
        return FindEditableParameterBlock(ObservedParameter.NoiseThreshold);
    }
    private PresetEffectBlock? FindEditableParameterBlock(ObservedParameter parameter)
    {
        if(currentSnapshot is not {} current)return null;
        var definition=ObservedParameterCommands.Definition(parameter);
        if(!parameterTargets.TryGetValue(definition.Position,out var position))return ObservedParameterCommands.FindSupportedBlock(current.Blocks,parameter);
        var block=current.Blocks.FirstOrDefault(b=>b.Position==position);if(block is null)return null;
        try {ObservedParameterCommands.Create(block,parameter,definition.Minimum);return block;}catch(NotSupportedException){return null;}
    }
    private void InvalidateEditTarget()
    {
        if(editSnapshotValid && operation is null)status.Text="Target changed · Read the amplifier before editing.";
        editSnapshotValid=false;UpdateThresholdState();
    }
    private bool TryPendingThreshold(out ushort value)=>ushort.TryParse(thresholdInput.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) && value <=100;
    private void UpdateThresholdState()
    {
        var block=EditableNoiseKiller();
        var supported=!preview && editSnapshotValid && snapshotAddress.HasValue && snapshotFirmware=="V1.1.9" && block is not null;
        thresholdInput.IsEnabled=supported && operation is null;
        applyThreshold.IsEnabled=supported && operation is null && TryPendingThreshold(out var value) && value!=block!.Values[2];
        thresholdMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            !supported?"Read a supported V1.1.9 preset before applying a threshold.":
            !TryPendingThreshold(out value)?"Enter a whole number from 0 to 100.":
            value==block!.Values[2]?$"Matches confirmed snapshot · Level stays at {block.Values[3]}.":$"Pending {value} · Confirmed {block.Values[2]} · Release the slider or choose Apply. Nothing is saved.";
        UpdatePresetSelectionState();
        UpdateToneState();
        UpdateAdditionalEditorStates();
        UpdateSnapshotArchiveState();
        UpdateSaveState();
        UpdateEffectPowerState();
        UpdateMixerState();
        UpdateTunerState();
    }
    private PresetEffectBlock? Editable808Tone()
    {
        return FindEditableParameterBlock(ObservedParameter.OdTone);
    }
    private static bool IsSupported808ToneBlock(PresetEffectBlock? block)
    {
        if(block is null)return false;
        try {ObservedParameterCommands.Create(block,ObservedParameter.OdTone,0);return true;}
        catch(NotSupportedException){return false;}
    }
    private bool TryPendingTone(out ushort value)=>ushort.TryParse(toneInput.Text,NumberStyles.None,CultureInfo.InvariantCulture,out value) && value <=100;
    private void UpdateToneState()
    {
        var block=Editable808Tone();
        var supported=!preview && editSnapshotValid && snapshotAddress.HasValue && snapshotFirmware=="V1.1.9" && block is not null;
        toneInput.IsEnabled=supported && operation is null;
        applyTone.IsEnabled=supported && operation is null && TryPendingTone(out var value) && value!=block!.Values[3];
        toneMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            !supported?"Read a supported V1.1.9 OD preset before applying Tone.":!TryPendingTone(out value)?"Enter a whole number from 0 to 100.":
            value==block!.Values[3]?$"Matches confirmed snapshot · Gain stays at {block.Values[2]} and Vol at {block.Values[4]}.":
            $"Pending {value} · Confirmed {block.Values[3]} · Release the slider or choose Apply. Nothing is saved.";
    }
    private async Task ApplyToneAsync()
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" || snapshotAddress is not {} target ||
            currentSnapshot is not {} expected || Editable808Tone() is not {} block || !TryPendingTone(out var value) || value==block.Values[3])return;
        BeginSteadyOperation("Checking amplifier before applying OD Tone…");snapshot.Text="Previous snapshot · Connecting for explicit live edit…";
        try {
            CurrentPresetSnapshot confirmed;
            confirmed=await UseSessionAsync(target,session=>session.SetParameterAtPositionAsync(expected,block.Position,ObservedParameter.OdTone,value,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffectsPreservingPending(confirmed.Blocks);
            var toneEffect=effectButtons.FirstOrDefault(item=>item.Effect.Source.Position==block.Position && item.Effect.Source.TypeCode==1).Effect;
            if(toneEffect is not null)SelectEffect(toneEffect);
            snapshot.Text=SnapshotLabel();status.Text=liveEditUnsaved?$"Live unsaved edit verified · OD Tone {value}":
                $"OD Tone {value} verified · Matches original snapshot · Nothing saved";
            lastMutationSucceeded=true;
        }
        catch(OperationCanceledException) {editSnapshotValid=false;status.Text="Tone edit interrupted · Outcome may be unknown. Read the amplifier before another edit.";snapshot.Text=SnapshotLabel();}
        catch(Exception) {editSnapshotValid=false;status.Text="Tone edit not confirmed. Read the amplifier before another edit. Nothing was saved.";snapshot.Text=SnapshotLabel();}
        finally {status.Foreground=MutedBrush;End();}
    }
    private void UpdatePresetSelectionState()
    {
        var selected=presets.SelectedItem as PresetName;
        presets.IsEnabled=operation is null && !tunerActive && !tunerStarting && names.Count>0;

        var supported=!preview && operation is null && editSnapshotValid && snapshotAddress.HasValue &&
            snapshotFirmware=="V1.1.9" && currentSnapshot?.WireIndex is >=1 and <=80 && selected?.WireIndex is >=1 and <=80;
        selectPreset.IsEnabled=supported && selected!.WireIndex!=currentSnapshot!.WireIndex;
        selectionMessage.Text=preview?"Preview only · No amplifier commands":operation is not null?"Operation in progress…":
            HasPresetStateChanges()?"Recall will discard live unsaved edits only after confirmation.":
            !editSnapshotValid||snapshotFirmware!="V1.1.9"?presetNamesAreCached?"Cached names · Connect to refresh names and recall a preset.":"Connect to load the presets first.":
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
    private Task SelectPresetAsync()=>RecallPresetAsync(presets.SelectedItem as PresetName);
    private async Task RecallPresetAsync(PresetName? requested)
    {
        lastMutationSucceeded=false;
        if(preview || operation is not null || !editSnapshotValid || snapshotFirmware!="V1.1.9" ||
            snapshotAddress is not {} target || currentSnapshot is not {} expected ||
            requested is not PresetName selected || selected.WireIndex==expected.WireIndex) return;
        if((HasPresetStateChanges() || HasPendingInputs()) && CreatePresetRecallDialog(selected).ShowDialog()!=true)return;
        BeginSteadyOperation("Checking amplifier before explicit preset recall…",loadingPreset:true);
        snapshot.Text="Previous snapshot · Connecting for explicit preset recall…";
        try {
            CurrentPresetSnapshot confirmed;
            confirmed=await UseSessionAsync(target,session=>session.SelectPresetAsync(expected,(byte)selected.WireIndex,operation!.Token));
            if(confirmed.Name!=selected.Name)throw new System.IO.IOException("Preset name differs from the loaded list.");
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=false;originalSnapshotDetail=confirmed.RawDetailPayloadHex;snapshotTime=DateTimeOffset.Now;
            ResetPresetEntryBaseline(confirmed);
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
        BeginSteadyOperation("Checking amplifier before applying threshold…");
        snapshot.Text="Previous snapshot · Connecting for explicit live edit…";
        try {
            CurrentPresetSnapshot confirmed;
            confirmed=await UseSessionAsync(target,session=>session.SetParameterAtPositionAsync(expected,block.Position,ObservedParameter.NoiseThreshold,value,operation!.Token));
            currentSnapshot=confirmed;editSnapshotValid=true;liveEditUnsaved=confirmed.RawDetailPayloadHex!=originalSnapshotDetail;snapshotTime=DateTimeOffset.Now;
            currentPreset.Text=$"{confirmed.SlotLabel} · {confirmed.Name}";SetEffectsPreservingPending(confirmed.Blocks);
            var noiseEffect=effectButtons.FirstOrDefault(item=>item.Effect.Source.Position==block.Position && item.Effect.Source.TypeCode==4).Effect;
            if(noiseEffect is not null)SelectEffect(noiseEffect);
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
    internal static Button Button(string text)
    {
        var button=new Button{Content=text,Padding=new Thickness(14,8,14,8),Background=Brush("#343B46"),Foreground=Brushes.White,BorderBrush=Brushes.Transparent,BorderThickness=new Thickness(2),Cursor=System.Windows.Input.Cursors.Hand,HorizontalContentAlignment=HorizontalAlignment.Center};
        button.Template=(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:local="clr-namespace:Iamp.Desktop;assembly=Iamp.Desktop" TargetType="Button">
              <Border x:Name="Surface" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="5" Padding="{TemplateBinding Padding}">
                <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center"/>
              </Border>
              <ControlTemplate.Triggers>
                <MultiTrigger><MultiTrigger.Conditions><Condition Property="IsEnabled" Value="False"/><Condition Property="local:SteadyControlAppearance.KeepEnabledAppearance" Value="False"/></MultiTrigger.Conditions><Setter TargetName="Surface" Property="Opacity" Value="0.45"/></MultiTrigger>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Opacity" Value="0.8"/></Trigger>
                <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#4C9EFF"/><Setter TargetName="Surface" Property="BorderThickness" Value="2"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        return button;
    }
}
