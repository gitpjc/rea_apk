using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Iamp.Protocol;
using Microsoft.Win32;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly DiagnosticJournal diagnostics = new();
    private readonly Button exportDiagnostics = Button("Export diagnostics");

    private void InitializeDiagnostics()
    {
        exportDiagnostics.ToolTip = "Export recent connection and command timings. No addresses, preset data or error text.";
        System.Windows.Automation.AutomationProperties.SetName(exportDiagnostics, "Export recent connection diagnostics to a local file");
        exportDiagnostics.Click += async (_, _) =>
        {
            if (!exportDiagnostics.IsEnabled) return;
            var dialog = new SaveFileDialog
            {
                Filter = "iAMP diagnostics (*.iamp-diagnostics.json)|*.iamp-diagnostics.json",
                DefaultExt = ".iamp-diagnostics.json", AddExtension = true, OverwritePrompt = true,
                FileName = "iamp-desktop.iamp-diagnostics.json", Title = "Export connection diagnostics"
            };
            if (dialog.ShowDialog(this) == true) await ExportDiagnosticsToPathAsync(dialog.FileName);
        };
        UpdateDiagnosticsState();
    }

    private void UpdateDiagnosticsState() => exportDiagnostics.IsEnabled = !preview && operation is null;

    /// <summary>Explicit hardware read QA only. Uses normal Read and local export handlers; no mutation workflow.</summary>
    internal async Task<bool> ReadDiagnosticsPreviewAsync(string bluetoothAddress, string outputPath)
    {
        var prefix = Path.GetFullPath(outputPath);
        if (new[] { prefix, prefix + ".iamp-diagnostics.json", prefix + ".iamp-snapshot.json" }.Any(File.Exists))
            throw new IOException("Use a new output path for each read check; existing evidence is preserved.");
        var success = await ReadSnapshotPreviewAsync(bluetoothAddress);
        var readStatus = status.Text;
        Directory.CreateDirectory(Path.GetDirectoryName(prefix)!);
        var exported = await ExportDiagnosticsToPathAsync(prefix + ".iamp-diagnostics.json");
        if (success)
        {
            var archivePath = prefix + ".iamp-snapshot.json";
            await ExportSnapshotToPathAsync(archivePath);
            var archive = SnapshotArchiveCodec.Decode(await File.ReadAllTextAsync(archivePath));
            if (currentSnapshot is not {} current ||
                archive.CapturedAt != snapshotTime || archive.Firmware != snapshotFirmware ||
                Convert.ToHexString(archive.StatePayload) != current.RawStatePayloadHex ||
                Convert.ToHexString(archive.DetailPayload) != current.RawDetailPayloadHex)
                throw new InvalidOperationException("Read preview export differs from the displayed snapshot.");
        }
        status.Text = readStatus;
        return success && exported;
    }

    private async Task<bool> ExportDiagnosticsToPathAsync(string path)
    {
        if (preview || operation is not null) return false;
        Begin("Exporting connection diagnostics…");
        string? temporary = null;
        try
        {
            var json = diagnostics.Export(DesktopVersion.Current);
            var destination = Path.GetFullPath(path);
            temporary = destination + $".{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(temporary, json, new UTF8Encoding(false), operation!.Token);
            operation.Token.ThrowIfCancellationRequested();
            File.Move(temporary, destination, true); temporary = null;
            status.Text = "Diagnostics exported locally · No amplifier changes";
            return true;
        }
        catch (OperationCanceledException) { status.Text = "Diagnostics export cancelled"; return false; }
        catch (Exception) { status.Text = "Could not export diagnostics. Check the destination and try again."; return false; }
        finally
        {
            if (temporary is not null)
                try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            status.Foreground = MutedBrush; End();
        }
    }

    /// <summary>Offline QA: simulated writer only; actual export handler and privacy/unchanged UI assertions.</summary>
    internal async Task PrepareDiagnosticsPreviewAsync(string outputPath)
    {
        var before = currentSnapshot; var target = snapshotAddress; var valid = editSnapshotValid;
        var pendingText = thresholdInput.Text;
        address.Text = "AA:BB:CC:DD:EE:FF";
        diagnostics.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Started);
        diagnostics.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Completed, 42);
        using (var simulated = new CommandSession((_, _) => Task.CompletedTask, diagnostics: diagnostics))
        {
            await simulated.WriteAsync([0x25, .. Encoding.UTF8.GetBytes("PRIVATE-PRESET-CONTENT")], default);
            simulated.Receive(FrameCodec.Encode([0x9B, 0]));
        }
        var destination = Path.GetFullPath(outputPath) + ".iamp-diagnostics.json";
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (!await ExportDiagnosticsToPathAsync(destination)) throw new IOException("Offline diagnostics export failed.");
        var json = await File.ReadAllTextAsync(destination);
        using var document = JsonDocument.Parse(json);
        var events = document.RootElement.GetProperty("events").EnumerateArray().ToArray();
        if (document.RootElement.GetProperty("appVersion").GetString() != DesktopVersion.Current ||
            !events.Any(e => e.GetProperty("outcome").GetString() == "TransferState") ||
            json.Contains("PRIVATE-PRESET-CONTENT", StringComparison.Ordinal) || json.Contains(address.Text, StringComparison.Ordinal) ||
            !ReferenceEquals(before, currentSnapshot) || target != snapshotAddress || valid != editSnapshotValid || pendingText != thresholdInput.Text)
            throw new InvalidOperationException("Diagnostics export privacy or UI preservation check failed.");
        status.Text = "Offline diagnostics export verified · No amplifier changes";
        Console.WriteLine("Diagnostics QA passed: actual export includes typed events and version; addresses/payloads excluded; snapshot preserved.");
    }
}
