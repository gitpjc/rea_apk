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

    private async Task<SnapshotComparisonResult?> CompareSnapshotFromPathAsync(string path)
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
            var result = SnapshotComparison.Compare(new UTF8Encoding(false, true).GetString(bytes, start, count - start), DisplayedSnapshotJson());
            operation!.Token.ThrowIfCancellationRequested();
            status.Text = "Snapshot comparison complete · No amplifier changes";
            return result;
        }
        catch (OperationCanceledException) { status.Text = "Comparison cancelled · Displayed snapshot preserved"; return null; }
        catch (Exception) { status.Text = "Could not compare this file. Use a valid local snapshot no larger than 64 KiB."; return null; }
        finally { status.Foreground = MutedBrush; End(); }
    }

    private Window CreateComparisonWindow(SnapshotComparisonResult result)
    {
        var dialog = new Window { Title = "Snapshot comparison", Width = 860, Height = 600, MinWidth = 600, MinHeight = 360,
            Owner = IsVisible ? this : null, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = BackgroundBrush, Foreground = System.Windows.Media.Brushes.White, FontFamily = FontFamily };
        var panel = new Grid();
        panel.RowDefinitions.Add(new() { Height = GridLength.Auto }); panel.RowDefinitions.Add(new()); panel.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var heading = new StackPanel(); heading.Children.Add(Text("Snapshot comparison", 24));
        heading.Children.Add(Text(result.RawPayloadsMatch ? result.FirmwareMatches ? "All stored values and firmware match." : "Stored values match; firmware differs." : $"{result.Differences.Count} differences found.", 16, BlueBrush, new Thickness(0, 12, 0, 0)));
        heading.Children.Add(Text("File → displayed snapshot. Read-only; this does not refresh the amp or prove saved storage. Unmapped fields have no assumed units.", 12, MutedBrush, new Thickness(0, 8, 0, 16))); panel.Children.Add(heading);
        var table = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false,
            Background = PanelBrush, Foreground = System.Windows.Media.Brushes.White, RowBackground = PanelBrush, AlternatingRowBackground = BackgroundBrush,
            HeadersVisibility = DataGridHeadersVisibility.Column, RowHeight = 32, ItemsSource = result.Differences,
            HorizontalGridLinesBrush = Brush("#353D48"), VerticalGridLinesBrush = Brush("#353D48") };
        foreach (var (label, property, width) in new[] { ("FIELD", "Field", 2d), ("FILE", "FileValue", 1d), ("DISPLAYED", "DisplayedValue", 1d) })
            table.Columns.Add(new DataGridTextColumn { Header = label, Binding = new Binding(property), MinWidth = width == 2 ? 320 : 180, Width = new DataGridLength(width, DataGridLengthUnitType.Star) });
        var header = new Style(typeof(DataGridColumnHeader)); header.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#2C323C")));
        header.Setters.Add(new Setter(Control.ForegroundProperty, MutedBrush)); header.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8)));
        table.ColumnHeaderStyle = header; Grid.SetRow(table, 1); panel.Children.Add(table);
        var close = Button("Close"); close.IsCancel = close.IsDefault = true; close.HorizontalAlignment = HorizontalAlignment.Right;
        close.Margin = new Thickness(0, 16, 0, 0); close.Click += (_, _) => dialog.Close(); Grid.SetRow(close, 2); panel.Children.Add(close);
        dialog.Content = new Border { Padding = new Thickness(24), Background = BackgroundBrush, Child = panel }; return dialog;
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
