using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Iamp.Protocol;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private sealed record EffectFilterOption(byte? TypeCode,ushort? Model,string Name,string Category="")
    {
        public string DisplayName=>TypeCode.HasValue?$"{Category} · {Name}":Name;
    }
    private readonly ComboBox effectFilter=new() {Width=280,MinHeight=35,MaxDropDownHeight=360,IsTextSearchEnabled=true,Background=PanelBrush,Foreground=Brushes.White};
    private readonly Button resetEffectFilter=Button("Reset"),refreshEffectNames=Button("Refresh effect names");
    private readonly TextBlock effectCacheNote=Text("",12,MutedBrush);
    private PresetEffectCacheStore effectCacheStore=new();
    private IReadOnlyDictionary<int,IReadOnlyList<PresetEffectBlock>> presetEffectIndex=new Dictionary<int,IReadOnlyList<PresetEffectBlock>>();

    private void InitializeEffectFilter()
    {
        effectFilter.ItemContainerStyle=presets.ItemContainerStyle;
        effectFilter.ItemTemplate=(DataTemplate)XamlReader.Parse("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><TextBlock Text="{Binding DisplayName}" Margin="8,6" TextTrimming="CharacterEllipsis"/></DataTemplate>
            """);
        effectFilter.Template=(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="clr-namespace:Iamp.Desktop;assembly=Iamp.Desktop" TargetType="ComboBox">
              <Grid>
                <ToggleButton Focusable="False" IsChecked="{Binding IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}" ClickMode="Press">
                  <ToggleButton.Template><ControlTemplate TargetType="ToggleButton">
                    <Border Background="#20242B" BorderBrush="#46505E" BorderThickness="1" CornerRadius="5">
                      <TextBlock Text="▾" Foreground="White" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,10,0"/>
                    </Border>
                  </ControlTemplate></ToggleButton.Template>
                </ToggleButton>
                <ContentPresenter Content="{TemplateBinding SelectionBoxItem}" ContentTemplate="{TemplateBinding ItemTemplate}" Margin="2,0,28,0" IsHitTestVisible="False" VerticalAlignment="Center"/>
                <Popup x:Name="PART_Popup" IsOpen="{TemplateBinding IsDropDownOpen}" Placement="Bottom" AllowsTransparency="True" Focusable="False">
                  <Border Background="#20242B" BorderBrush="#46505E" BorderThickness="1" MinWidth="{Binding ActualWidth,RelativeSource={RelativeSource TemplatedParent}}" MaxHeight="{TemplateBinding MaxDropDownHeight}">
                    <ScrollViewer CanContentScroll="True" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/></ScrollViewer>
                  </Border>
                </Popup>
              </Grid>
              <ControlTemplate.Triggers><MultiTrigger><MultiTrigger.Conditions><Condition Property="IsEnabled" Value="False"/><Condition Property="local:SteadyControlAppearance.KeepEnabledAppearance" Value="False"/></MultiTrigger.Conditions><Setter Property="Opacity" Value="0.55"/></MultiTrigger></ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        TextSearch.SetTextPath(effectFilter,"Name");
        System.Windows.Automation.AutomationProperties.SetName(effectFilter,"Filter presets by effect, including disabled effects");
        effectFilter.ItemsSource=new[]{new EffectFilterOption(null,null,"Any effect")}
            .Concat(PresetEffectSearch.Catalog.Select(e=>new EffectFilterOption(e.TypeCode,e.Model,e.Name,e.Category))).ToArray();
        effectFilter.SelectedIndex=0;
        effectFilter.SelectionChanged+=(_,_)=> {
            if(presets.IsDropDownOpen){cancelPresetDropdown=true;presets.IsDropDownOpen=false;}
            Filter();
            UpdateEffectFilterState();
        };
        resetEffectFilter.ToolTip="Reset effect filter to Any effect; keep the text search";
        resetEffectFilter.Click+=(_,_)=> {effectFilter.SelectedIndex=0;effectFilter.Focus();};
        refreshEffectNames.ToolTip="Reread which effects each preset uses. Built-in model names are already stored with the app.";
        refreshEffectNames.Click+=async(_,_)=>await RefreshEffectNamesAsync();
        System.Windows.Automation.AutomationProperties.SetName(resetEffectFilter,"Reset effect filter to Any effect");
        System.Windows.Automation.AutomationProperties.SetName(refreshEffectNames,"Refresh cached preset effect names from amplifier");
        UpdateEffectFilterState();
    }

    private void UpdateEffectFilterState()
    {
        effectFilter.IsEnabled=operation is null && presetEffectIndex.Count>0;
        resetEffectFilter.IsEnabled=operation is null && effectFilter.SelectedIndex>0;
        refreshEffectNames.IsEnabled=!preview && operation is null && !localSnapshotSource && editSnapshotValid &&
            snapshotFirmware=="V1.1.9" && connectedSession is not null && snapshotAddress.HasValue && snapshotAddress==connectedAddress;
        effectFilter.ToolTip=presetEffectIndex.Count>0?"Search preset contents, including disabled effects. Any effect clears this filter.":
            "Connect to a V1.1.9 amplifier to load preset effects, or open a local snapshot.";
    }

    private bool MatchesEffectFilter(PresetName preset)
        =>effectFilter.SelectedItem is not EffectFilterOption {TypeCode:{} type,Model:{} model} ||
            presetEffectIndex.TryGetValue(preset.WireIndex,out var blocks) && PresetEffectSearch.Contains(blocks,type,model);

    private void SetPresetEffectIndex(IEnumerable<DecodedPreset> records)
    {
        presetEffectIndex=PresetEffectSearch.CreateIndex(records);
        if(presetEffectIndex.Count==0)effectFilter.SelectedIndex=0;
        UpdateEffectFilterState();
    }

    private void UpdateIndexedCurrentEffects()
    {
        if(currentSnapshot is not {} current || presetEffectIndex.Count==0)return;
        var updated=presetEffectIndex.ToDictionary(p=>p.Key,p=>p.Value);
        updated[current.WireIndex]=current.Blocks;presetEffectIndex=updated;
    }

    private async Task<IReadOnlyList<DecodedPreset>> LoadPresetEffectsAsync(F15ReadSession session,CurrentPresetSnapshot current,ulong target,string firmware,CancellationToken ct)
    {
        if(effectCacheStore.Load(target,firmware) is {} cached) {
            effectCacheNote.Text=$"Cached effects · Updated {cached.RefreshedAt.ToLocalTime():dd MMM, HH:mm}";
            return PresetEffectCacheCodec.ToRecords(cached);
        }
        var records=await session.ReadPresetEffectsAsync(current,EffectReadProgress(),ct);
        effectCacheNote.Text=effectCacheStore.Save(target,firmware,records)?"Effects cached · Refresh when needed":"Effects loaded · Cache could not be saved";
        return records;
    }

    private IProgress<int> EffectReadProgress()
    {
        var active=operation;
        return new Progress<int>(n=> {if(active is not null && ReferenceEquals(operation,active))status.Text=$"Reading preset effects… {n}/80";});
    }

    private async Task RefreshEffectNamesAsync()
    {
        if(!refreshEffectNames.IsEnabled || connectedSession is not {} session || snapshotAddress is not {} target ||
            snapshotFirmware is not {} firmware || currentSnapshot is not {} expected)return;
        Begin("Refreshing effect names…");
        try {
            var records=await session.ReadPresetEffectsAsync(expected,EffectReadProgress(),operation!.Token);
            operation.Token.ThrowIfCancellationRequested();
            var saved=effectCacheStore.Save(target,firmware,records);
            SetPresetEffectIndex(records);UpdateIndexedCurrentEffects();Filter();
            effectCacheNote.Text=saved?"Effects refreshed and cached":"Effects refreshed · Cache could not be saved";
            status.Text="Effect names refreshed · All 80 presets updated";
        }
        catch(OperationCanceledException) {
            status.Text="Effect refresh cancelled · Previous effect data kept";
            await ReleaseConnectionAsync();editSnapshotValid=false;
        }
        catch(Exception) {
            status.Text="Could not refresh effects · Previous effect data kept. Reconnect to try again.";
            await ReleaseConnectionAsync();editSnapshotValid=false;
        }
        finally {status.Foreground=MutedBrush;snapshot.Text=SnapshotLabel();End();}
    }
}
