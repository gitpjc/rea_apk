using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Iamp.Protocol;
using Microsoft.Win32;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button compareSnapshot = Button("Compare snapshot");
    private void InitializeComparison()
    {
        compareSnapshot.ToolTip = "Compare a local file with the displayed snapshot without changing either.";
        compareSnapshot.Click += async (_, _) =>
        {
            if (!compareSnapshot.IsEnabled) return;
            var picker = new OpenFileDialog { Filter = SnapshotFilter, CheckFileExists = true, Multiselect = false, Title = "Compare snapshot with a local file" };
            if (picker.ShowDialog(this) != true) return;
            var result = await CompareSnapshotFromPathAsync(picker.FileName);
            if (result is not null) CreateComparisonWindow(result).ShowDialog();
        };
        UpdateComparisonState();
    }
    private void UpdateComparisonState() => compareSnapshot.IsEnabled = !preview && operation is null && currentSnapshot is not null;
    private string DisplayedSnapshotJson() => SnapshotArchiveCodec.Encode(snapshotFirmware!, snapshotTime!.Value,
        Convert.FromHexString(currentSnapshot!.RawStatePayloadHex), Convert.FromHexString(currentSnapshot.RawDetailPayloadHex));

    private async Task<ReadableSnapshotComparison?> CompareSnapshotFromPathAsync(string path)
    {
        if (!compareSnapshot.IsEnabled) return null;
        Begin("Comparing local snapshot…");
        try
        {
            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var bytes = new byte[MaximumSnapshotFileBytes + 1]; var count = 0;
            while (count < bytes.Length)
            {
                var read = await input.ReadAsync(bytes.AsMemory(count), operation!.Token);
                if (read == 0) break;
                count += read;
            }
            if (count > MaximumSnapshotFileBytes) throw new FormatException("Snapshot exceeds 64 KiB.");
            var start = count >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            var fileJson=new UTF8Encoding(false,true).GetString(bytes,start,count-start);
            var displayedJson=DisplayedSnapshotJson();
            var stored=SnapshotComparison.Compare(fileJson,displayedJson);
            var result=new ReadableSnapshotComparison(stored,SnapshotArchiveCodec.Decode(fileJson),SnapshotArchiveCodec.Decode(displayedJson),Path.GetFileName(path),localSnapshotSource);
            operation!.Token.ThrowIfCancellationRequested();
            status.Text = "Snapshot comparison complete · No amplifier changes";
            return result;
        }
        catch (OperationCanceledException) { status.Text = "Comparison cancelled · Displayed snapshot preserved"; return null; }
        catch (Exception) { status.Text = "Could not compare this file. Use a valid local snapshot no larger than 64 KiB."; return null; }
        finally { status.Foreground = MutedBrush; End(); }
    }

    private Window CreateComparisonWindow(ReadableSnapshotComparison result)
    {
        var dialog = new Window { Title = "Snapshot comparison", Width = Math.Min(1080,SystemParameters.WorkArea.Width), Height = Math.Min(780,SystemParameters.WorkArea.Height), MinWidth = 620, MinHeight = 400,
            Owner = IsVisible ? this : null, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = BackgroundBrush, Foreground = System.Windows.Media.Brushes.White, FontFamily = FontFamily };
        var panel = new Grid();
        panel.RowDefinitions.Add(new() { Height = GridLength.Auto }); panel.RowDefinitions.Add(new()); panel.RowDefinitions.Add(new() { Height = GridLength.Auto });panel.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var heading = new StackPanel(); heading.Children.Add(Text("Snapshot comparison", 24));
        heading.Children.Add(Text(result.RawPayloadsMatch&&result.Stored.FirmwareMatches?"These snapshots match.":"Compare each preset’s effects and settings below. Highlighted values differ for the same effect model.",14,BlueBrush,new Thickness(0,10,0,0)));
        heading.Children.Add(Text("Read-only · Uses the displayed snapshot without refreshing or changing the amplifier. Each column keeps its own effect order.",12,MutedBrush,new Thickness(0,8,0,16)));panel.Children.Add(heading);
        var columns=new Grid();columns.ColumnDefinitions.Add(new());columns.ColumnDefinitions.Add(new(){Width=new GridLength(16)});columns.ColumnDefinitions.Add(new());
        var fileColumn=CreateComparisonPresetColumn("SAVED SNAPSHOT",result.File,result.Displayed,result.FileName);
        var currentColumn=CreateComparisonPresetColumn(result.DisplayedIsLocal?"DISPLAYED SNAPSHOT":"CURRENT PRESET · LAST READ",result.Displayed,result.File,null);
        columns.Children.Add(fileColumn);Grid.SetColumn(currentColumn,2);columns.Children.Add(currentColumn);Grid.SetRow(columns,1);panel.Children.Add(columns);
        var table = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
            Background = PanelBrush, Foreground = System.Windows.Media.Brushes.White, RowBackground = PanelBrush, AlternatingRowBackground = BackgroundBrush,
            HeadersVisibility = DataGridHeadersVisibility.Column, RowHeight = 32, ItemsSource = result.Differences,
            HorizontalGridLinesBrush = Brush("#353D48"), VerticalGridLinesBrush = Brush("#353D48") };
        foreach (var (label, property, width) in new[] { ("FIELD", "Field", 2d), ("FILE", "FileValue", 1d), ("DISPLAYED", "DisplayedValue", 1d) })
            table.Columns.Add(new DataGridTextColumn { Header = label, Binding = new Binding(property), MinWidth = width == 2 ? 320 : 180, Width = new DataGridLength(width, DataGridLengthUnitType.Star) });
        var header = new Style(typeof(DataGridColumnHeader)); header.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#2C323C")));
        header.Setters.Add(new Setter(Control.ForegroundProperty, MutedBrush)); header.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8)));
        table.ColumnHeaderStyle = header;
        var technical=new Expander{Header="Technical details",Foreground=MutedBrush,Margin=new Thickness(0,12,0,0),Content=table,MaxHeight=190};
        table.MinHeight=100;Grid.SetRow(technical,2);panel.Children.Add(technical);
        var close = Button("Close"); close.IsCancel = close.IsDefault = true; close.HorizontalAlignment = HorizontalAlignment.Right;
        close.Margin = new Thickness(0, 12, 0, 0); close.Click += (_, _) => dialog.Close(); Grid.SetRow(close, 3); panel.Children.Add(close);
        dialog.Content = new Border { Padding = new Thickness(20), Background = BackgroundBrush, Child = panel }; return dialog;
    }

    private UIElement CreateComparisonPresetColumn(string label,SnapshotArchive own,SnapshotArchive other,string? fileName)
    {
        var content=new StackPanel();content.Children.Add(Text(label,12,BlueBrush));
        var title=Text($"{PresetAddress.FromWireIndex(own.Preset.WireIndex).Label} · {own.Preset.Name}",20,null,new Thickness(0,6,0,8));title.FontWeight=FontWeights.Bold;content.Children.Add(title);
        content.Children.Add(Text($"Firmware {own.Firmware} · Captured {own.CapturedAt.ToLocalTime():dd MMM yyyy, HH:mm}",12,MutedBrush));
        if(fileName is not null)content.Children.Add(Text(fileName,12,MutedBrush,new Thickness(0,5,0,0)));
        var blocks=own.Preset.Blocks.Where(b=>b.TypeCode!=0).OrderBy(b=>b.Position).ToArray();
        content.Children.Add(Text($"{blocks.Length} effects",13,MutedBrush,new Thickness(0,10,0,12)));
        foreach(var block in blocks)
        {
            var shown=EffectPresentation.From(block);var matching=ReadableSnapshotComparison.FindMatchingEffect(block,own.Preset.Blocks,other.Preset.Blocks);
            var counterpart=matching is null?null:EffectPresentation.From(matching);
            var card=new StackPanel();var category=Text($"{block.Position} · {shown.Category}",18,Brush(shown.Color));category.FontWeight=FontWeights.Bold;card.Children.Add(category);
            card.Children.Add(Text(shown.Title,15,null,new Thickness(0,4,0,0)));
            var state=ReadableSnapshotComparison.State(shown);var stateDiff=counterpart is not null&&counterpart.Enabled!=shown.Enabled;
            card.Children.Add(Text(state+(stateDiff?" · Different":""),12,stateDiff?BlueBrush:MutedBrush,new Thickness(0,5,0,8)));
            if(counterpart is null)card.Children.Add(Text("Different model or not present in the other preset",12,MutedBrush,new Thickness(0,0,0,8)));
            else if(matching!.Position!=block.Position)card.Children.Add(Text($"Position {matching.Position} in the other preset",12,MutedBrush,new Thickness(0,0,0,8)));
            string? previousGroup=null;
            foreach(var parameter in shown.Parameters)
            {
                if(parameter.Group is {} group&&group!=previousGroup)
                {var groupTitle=Text(group,14,null,new Thickness(0,8,0,5));groupTitle.FontWeight=FontWeights.Bold;card.Children.Add(groupTitle);}
                previousGroup=parameter.Group;
                var comparable=counterpart?.Parameters.FirstOrDefault(p=>p.ValueIndex==parameter.ValueIndex&&p.Name==parameter.Name&&p.Group==parameter.Group);
                var changed=comparable is not null&&(comparable.Value!=parameter.Value||comparable.IsOff!=parameter.IsOff||comparable.Unit!=parameter.Unit);
                var row=new Grid {Margin=new Thickness(0,3,0,3)};row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
                row.Children.Add(Text(ReadableSnapshotComparison.Label(parameter)+":",13,MutedBrush,new Thickness(0,0,12,0)));
                var value=Text(ReadableSnapshotComparison.Format(parameter),14,changed?BlueBrush:null);if(changed){value.FontWeight=FontWeights.Bold;value.ToolTip="Different from the same effect model in the other preset";}
                Grid.SetColumn(value,1);row.Children.Add(value);card.Children.Add(row);
            }
            if(shown.Parameters.Count==0)card.Children.Add(Text("Named settings are unavailable for this model/layout.",13,MutedBrush));
            content.Children.Add(new Border {Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(14),Margin=new Thickness(0,0,0,12),Child=card});
        }
        if(blocks.Length==0)content.Children.Add(Text("No effects in this preset.",14,MutedBrush));
        return new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
    }

    internal async Task<Window> PrepareComparisonPreviewAsync(string inputPath, string outputPath)
    {
        if (!await LoadSnapshotFromPathAsync(inputPath)) throw new FormatException("Preview needs a valid local snapshot.");
        var before = currentSnapshot; var input = thresholdInput.Text;
        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!; Directory.CreateDirectory(directory);
        var changedPath = Path.Combine(directory, $".comparison-{Guid.NewGuid():N}.json");
        try
        {
            var state = Convert.FromHexString(before!.RawStatePayloadHex); state[2] ^= 1;
            var detail = Convert.FromHexString(before.RawDetailPayloadHex);
            var block = before.Blocks.First(b => b.Values.Count >= 3);
            detail = PresetDataParser.WithParameterValue(detail, block.Position, 2, (ushort)(block.Values[2] ^ 1));
            await File.WriteAllTextAsync(changedPath, SnapshotArchiveCodec.Encode(snapshotFirmware!, snapshotTime!.Value, state, detail));
            var result = await CompareSnapshotFromPathAsync(changedPath) ?? throw new InvalidOperationException("Comparison failed.");
            if (result.Differences.Count != 2 || result.RawPayloadsMatch || !ReferenceEquals(currentSnapshot, before) || thresholdInput.Text != input)
                throw new InvalidOperationException("Comparison failed to detect differences or changed the displayed snapshot.");
            RequireLocalArchiveReadOnly();
            await File.WriteAllTextAsync(changedPath, "{}");
            if (await CompareSnapshotFromPathAsync(changedPath) is not null || !ReferenceEquals(currentSnapshot, before))
                throw new InvalidOperationException("Malformed comparison changed the snapshot.");
            Console.WriteLine("Comparison QA passed: differences detected; malformed file rejected; snapshot preserved and all writes disabled.");
            return CreateComparisonWindow(result);
        }
        finally { File.Delete(changedPath); }
    }
}
