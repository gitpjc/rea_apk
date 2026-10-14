using System.Text;
using System.Text.Json;
using Iamp.Protocol;

namespace Iamp.Protocol.Tests;

public class DiagnosticJournalTests
{
    private static JsonElement[] Events(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("events").EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    [Fact]
    public void ExportHasOnlyAllowlistedMetadataAndBoundedEvents()
    {
        var journal = new DiagnosticJournal(capacity: 2);
        journal.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Started);
        journal.Record(DiagnosticOperation.Connection, DiagnosticOutcome.Completed, 42);
        journal.Record(DiagnosticOperation.Session, DiagnosticOutcome.Disposed);
        using var document = JsonDocument.Parse(journal.Export("1.3.0"));
        var root = document.RootElement;
        Assert.Equal(new[] { "format", "schemaVersion", "appVersion", "exportedAt", "capacity", "droppedEvents", "events" }, root.EnumerateObject().Select(p => p.Name));
        Assert.Equal("iamp-desktop-diagnostics", root.GetProperty("format").GetString());
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(1, root.GetProperty("droppedEvents").GetInt64());
        var events = root.GetProperty("events").EnumerateArray().ToArray();
        Assert.Equal(new long[] { 2, 3 }, events.Select(e => e.GetProperty("sequence").GetInt64()));
        Assert.All(events, e => Assert.Equal(new[] { "sequence", "at", "operation", "outcome", "elapsedMilliseconds" }, e.EnumerateObject().Select(p => p.Name)));
        Assert.Equal(42, events[0].GetProperty("elapsedMilliseconds").GetInt64());
    }

    [Fact]
    public async Task ActualDispatcherDoesNotRecordPayloadsOrExceptionDetails()
    {
        const string secret = "AA:BB:CC:DD:EE:FF private-tone C:\\Users\\Private\\snapshot.json";
        var journal = new DiagnosticJournal();
        using var session = new CommandSession((_, _) => Task.FromException(new IOException(secret)), diagnostics: journal);
        await Assert.ThrowsAsync<IOException>(() => session.WriteAsync([0x2F, .. Encoding.UTF8.GetBytes(secret)], default));
        var json = journal.Export("1.3.0");
        Assert.DoesNotContain(secret, json);
        Assert.DoesNotContain("private-tone", json);
        Assert.DoesNotContain("AA:BB", json);
        Assert.DoesNotContain("Private", json);
        var events = Events(json);
        Assert.Contains(events, e => e.GetProperty("operation").GetString() == "PresetSave" && e.GetProperty("outcome").GetString() == "Started");
        Assert.Contains(events, e => e.GetProperty("operation").GetString() == "PresetSave" && e.GetProperty("outcome").GetString() == "Failed");
    }

    [Fact]
    public async Task DispatcherRecordsCompletionAndTransferFailureWithoutRawNotification()
    {
        var journal = new DiagnosticJournal();
        using var session = new CommandSession((_, _) => Task.CompletedTask, diagnostics: journal);
        await session.WriteAsync([0x25, 1, 4, 12], default);
        session.Receive(FrameCodec.Encode([0x9B, 0]));
        await Assert.ThrowsAsync<TransferStateChangedException>(() => session.WriteAsync([0x11], default));
        var events = Events(journal.Export("1.3.0"));
        Assert.Contains(events, e => e.GetProperty("outcome").GetString() == "Completed");
        Assert.Contains(events, e => e.GetProperty("outcome").GetString() == "TransferState");
        Assert.Contains(events, e => e.GetProperty("outcome").GetString() == "Rejected");
    }

    [Fact]
    public async Task ResponseTimeoutIsDistinguishedFromUserCancellation()
    {
        var journal = new DiagnosticJournal();
        using var session = new CommandSession((_, _) => Task.CompletedTask, TimeSpan.FromMilliseconds(100), journal);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.QueryAsync([0x11], _ => true, default).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains(Events(journal.Export("1.3.0")), e => e.GetProperty("outcome").GetString() == "TimedOut");
    }

    [Fact]
    public async Task ConcurrentRecordingKeepsUniqueOrderedSequencesAndExactDropCount()
    {
        var journal = new DiagnosticJournal(capacity: 16);
        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() => journal.Record(DiagnosticOperation.Session, DiagnosticOutcome.Completed))));
        using var document = JsonDocument.Parse(journal.Export("1.3.0"));
        Assert.Equal(84, document.RootElement.GetProperty("droppedEvents").GetInt64());
        Assert.Equal(Enumerable.Range(85, 16).Select(n => (long)n), Events(journal.Export("1.3.0")).Select(e => e.GetProperty("sequence").GetInt64()));
    }

    [Theory]
    [InlineData("C:/Users/Private")]
    [InlineData("AA:BB:CC:DD:EE:FF")]
    [InlineData("1.3.0\nprivate")]
    public void RejectsNonVersionExportMetadata(string value)
        => Assert.Throws<ArgumentException>(() => new DiagnosticJournal().Export(value));

    [Fact]
    public void RejectsInvalidBoundsAndUntypedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiagnosticJournal(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiagnosticJournal(4097));
        var journal = new DiagnosticJournal();
        Assert.Throws<ArgumentOutOfRangeException>(() => journal.Record((DiagnosticOperation)999, DiagnosticOutcome.Completed));
        Assert.Throws<ArgumentOutOfRangeException>(() => journal.Record(DiagnosticOperation.Session, (DiagnosticOutcome)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => journal.Record(DiagnosticOperation.Session, DiagnosticOutcome.Completed, -1));
    }
}
