using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Iamp.Protocol;

public enum DiagnosticOperation { Connection, Session, IdentityRead, PresetNamesRead, CurrentStateRead, PresetDetailRead, LiveUpdate, PresetRecall, PresetSave, OtherCommand,
    ToolsEntry, TunerEntry, TunerExit, ToolsRequest, ToolsReply, BatteryRead, TunerReportedActive, TunerReportedInactive, TunerSignal, TunerNoSignal, TunerUiSignal, TunerUiNoSignal, ProtocolSetupRead }
public enum DiagnosticOutcome { Started, Completed, Failed, Cancelled, TimedOut, Disconnected, TransferState, Disposed, Rejected, InvalidResponse }
public sealed record DiagnosticEvent(long Sequence, DateTimeOffset At, DiagnosticOperation Operation,
    DiagnosticOutcome Outcome, long ElapsedMilliseconds);

/// <summary>Bounded, in-memory, allowlisted events. Never accepts payloads, identifiers, paths or error text.</summary>
public sealed class DiagnosticJournal
{
    private readonly object gate = new();
    private readonly Queue<DiagnosticEvent> entries = new();
    private readonly int capacity;
    private long sequence;
    private long dropped;

    public DiagnosticJournal(int capacity = 256)
    {
        if (capacity is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.capacity = capacity;
    }

    public void Record(DiagnosticOperation operation, DiagnosticOutcome outcome, long elapsedMilliseconds = 0)
    {
        if (!Enum.IsDefined(operation) || !Enum.IsDefined(outcome) || elapsedMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(operation));
        lock (gate)
        {
            if (entries.Count == capacity) { entries.Dequeue(); dropped++; }
            entries.Enqueue(new(++sequence, DateTimeOffset.UtcNow, operation, outcome, elapsedMilliseconds));
        }
    }

    public string Export(string appVersion)
    {
        if (appVersion is null || appVersion.Length > 64 || !Regex.IsMatch(appVersion,
            @"\A[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?\z"))
            throw new ArgumentException("An application version is required.", nameof(appVersion));
        lock (gate)
            return JsonSerializer.Serialize(new
            {
                format = "iamp-desktop-diagnostics", schemaVersion = 1, appVersion,
                exportedAt = DateTimeOffset.UtcNow, capacity, droppedEvents = dropped, events = entries.ToArray()
            }, new JsonSerializerOptions
            {
                WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            });
    }

    public static DiagnosticOperation CommandKind(ReadOnlySpan<byte> payload) => payload.Length == 0 ? DiagnosticOperation.OtherCommand : payload[0] switch
    {
        0x01 => DiagnosticOperation.IdentityRead, 0x29 => DiagnosticOperation.PresetNamesRead,
        0x11 => DiagnosticOperation.CurrentStateRead, 0x21 => DiagnosticOperation.PresetDetailRead,
        0x25 => DiagnosticOperation.LiveUpdate, 0x13 => DiagnosticOperation.PresetRecall,
        0x2F => DiagnosticOperation.PresetSave,
        0x41 when payload.Length == 2 && payload[1] == 1 => DiagnosticOperation.ToolsEntry,
        0x41 when payload.Length == 2 && payload[1] == 2 => DiagnosticOperation.TunerEntry,
        0x41 when payload.Length == 2 && payload[1] == 0 => DiagnosticOperation.TunerExit,
        0x4A when payload.Length == 2 && payload[1] == 1 => DiagnosticOperation.ToolsReply,
        0x91 when payload.Length == 1 => DiagnosticOperation.BatteryRead,
        0x03 when payload.Length == 1 => DiagnosticOperation.ProtocolSetupRead,
        _ => DiagnosticOperation.OtherCommand
    };

    public static DiagnosticOperation? TunerNotificationKind(ReadOnlySpan<byte> payload)
    {
        if (payload.SequenceEqual(new byte[] { 0x49, 0x00 })) return DiagnosticOperation.ToolsRequest;
        if (payload.SequenceEqual(new byte[] { 0x43, 0x00 })) return DiagnosticOperation.TunerReportedInactive;
        if (payload.SequenceEqual(new byte[] { 0x43, 0x02 })) return DiagnosticOperation.TunerReportedActive;
        if (TunerReading.TryParse(payload, out var reading))
            return reading!.Value.HasSignal ? DiagnosticOperation.TunerSignal : DiagnosticOperation.TunerNoSignal;
        return null;
    }

    public static DiagnosticOutcome OutcomeFor(Exception error, bool timedOut = false) => error switch
    {
        TransferStateChangedException => DiagnosticOutcome.TransferState,
        ObjectDisposedException => DiagnosticOutcome.Disposed,
        OperationCanceledException => timedOut ? DiagnosticOutcome.TimedOut : DiagnosticOutcome.Cancelled,
        TimeoutException => DiagnosticOutcome.TimedOut,
        FormatException => DiagnosticOutcome.InvalidResponse,
        _ => DiagnosticOutcome.Failed
    };
}
