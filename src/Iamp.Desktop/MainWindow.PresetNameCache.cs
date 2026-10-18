using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly PresetNameCacheStore presetNameCacheStore;
    private bool presetNamesAreCached;

    private void LoadCachedPresetNames()
    {
        if(presetNameCacheStore.Load() is not {} cached)return;
        names=cached.Presets.Select(p=>new PresetName(p.WireIndex,p.Name)).ToArray();presetNamesAreCached=true;
        Filter();status.Text="Offline · Last-read preset names loaded";
        count.ToolTip=$"Preset names cached {cached.ReadAt.ToLocalTime():dd MMM yyyy, HH:mm}. Connect to refresh.";
    }

    private void CacheLivePresetNames()
    {
        if(preview||localSnapshotSource||names.Count!=80)return;
        presetNamesAreCached=false;
        count.ToolTip=presetNameCacheStore.Save(names)?"Last-read preset names are available offline.":
            "Preset names are available for this session; the offline cache could not be updated.";
    }
}
