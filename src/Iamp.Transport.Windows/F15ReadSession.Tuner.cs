using Iamp.Protocol;

namespace Iamp.Transport.Windows;

public sealed partial class F15ReadSession
{
    private int tunerEnabled;
    private CurrentPresetSnapshot? tunerSnapshot;
    public bool IsTunerActive => Volatile.Read(ref tunerEnabled) != 0;
    public bool IsAvailable => Volatile.Read(ref disposed) == 0 && commands.IsAvailable && workflows.IsAvailable;
    public event Action<byte[]>? TunerPayloadReceived;

    private void OnUnsolicitedPayload(byte[] payload)
    {
        ObserveFootswitchStatus(payload);
        if (DiagnosticJournal.TunerNotificationKind(payload) is {} kind)
            diagnostics?.Record(kind, DiagnosticOutcome.Completed);
        if (IsTunerActive && payload.Length > 0 && (payload[0] == 0x45 || payload.Length == 2 && payload[0] == 0x43 && payload[1] is 0 or 2))
            TunerPayloadReceived?.Invoke(payload);
    }

    public Task<CurrentPresetSnapshot> EnterTunerAsync(CurrentPresetSnapshot expected, CancellationToken ct)
        => workflows.RunAsync(async token =>
        {
            commands.ThrowIfUnavailable();
            if (IsTunerActive) return tunerSnapshot!;
            if (confirmedIdentity?.Firmware != "V1.1.9")
                throw new NotSupportedException("Read the connected F15i V1.1.9 identity before starting its tuner.");
            var before = await ReadCurrentPresetCoreAsync(token);
            RequireTunerSnapshot(before, expected);
            _ = TunerSettings.Parse(Convert.FromHexString(before.RawStatePayloadHex));
            // Immediate entry after startup/state synchronization can be overwritten by the amp's delayed mode update.
            // A three-second quiet interval passed the bounded startup comparisons; do not poll during this interval.
            await Task.Delay(TimeSpan.FromSeconds(3), token);
            tunerSnapshot = before;
            // Entry may take effect before its acknowledgement. Retain uncertainty on failure.
            Volatile.Write(ref tunerEnabled, 1);
            try { await TunerEntrySequence.EnterAsync(commands, token, responder: toolRequests); }
            catch
            {
                // A partial Tools entry can already have changed the amp's screen. Exit once if transport is usable.
                if (commands.IsAvailable)
                {
                    try
                    {
                        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await ChangeTunerModeAsync(TunerCommands.CreateExit(), cleanup.Token);
                        Volatile.Write(ref tunerEnabled, 0);
                    }
                    catch (Exception) { /* Preserve the uncertain state; the caller reports it and requires reconnect. */ }
                }
                throw;
            }
            return before;
        }, ct);

    /// <summary>Retain the captured mobile battery polling during tuning; never read after a queued exit.</summary>
    public Task<BatteryStatus?> PollTunerAsync(CancellationToken ct)
        => workflows.RunAsync(async token =>
        {
            if (!IsTunerActive) return null;
            commands.ThrowIfUnavailable();
            return (BatteryStatus?)BatteryStatus.Parse(await QueryAsync([0x91], p => p.Length > 0 && p[0] == 0x92, token));
        }, ct);

    public Task<CurrentPresetSnapshot> SetTunerReferenceAsync(int hertz, CancellationToken ct)
        => SetTunerSettingsAsync(state => TunerCommands.CreateReference(state, hertz), ct);

    public Task<CurrentPresetSnapshot> SetTunerOutputModeAsync(TunerOutputMode mode, CancellationToken ct)
        => SetTunerSettingsAsync(state => TunerCommands.CreateOutputMode(state, mode), ct);

    private Task<CurrentPresetSnapshot> SetTunerSettingsAsync(Func<byte[], byte[]> create, CancellationToken ct)
        => workflows.RunAsync(async token =>
        {
            commands.ThrowIfUnavailable();
            if (!IsTunerActive || tunerSnapshot is null)
                throw new InvalidOperationException("Start the tuner before changing its settings.");
            var before = await ReadCurrentPresetCoreAsync(token);
            RequireTunerSnapshot(before, tunerSnapshot);
            var request = create(Convert.FromHexString(before.RawStatePayloadHex));
            var response = await QueryAsync(request, p => p.Length > 0 && p[0] == 0x14, token);
            if (!response.AsSpan().SequenceEqual(new byte[] { 0x14, 0x01 }))
                throw new IOException("Tuner settings acknowledgement differs; do not repeat the change.");
            request[0] = 0x12;
            var after = await ReadCurrentPresetCoreAsync(token);
            if (after.RawStatePayloadHex != Convert.ToHexString(request) || after.RawDetailPayloadHex != before.RawDetailPayloadHex)
                throw new IOException("Tuner settings readback differs; reconnect before another edit.");
            tunerSnapshot = after;
            return after;
        }, ct);

    public Task<CurrentPresetSnapshot> ExitTunerAsync(CancellationToken ct)
        => workflows.RunAsync(async token =>
        {
            commands.ThrowIfUnavailable();
            if (IsTunerActive)
            {
                await ChangeTunerModeAsync(TunerCommands.CreateExit(), token);
                Volatile.Write(ref tunerEnabled, 0);
            }
            var after = await ReadCurrentPresetCoreAsync(token);
            if (tunerSnapshot is not null) RequireTunerSnapshot(after, tunerSnapshot);
            tunerSnapshot = null;
            return after;
        }, ct);

    private async Task ChangeTunerModeAsync(byte[] request, CancellationToken ct)
    {
        var response = await QueryAsync(request, p => p.Length > 0 && p[0] == 0x42, ct);
        if (!response.AsSpan().SequenceEqual(new byte[] { 0x42, 0x01 }))
            throw new IOException("Tuner mode acknowledgement differs. Check the amplifier; the command will not be replayed.");
    }

    private static void RequireTunerSnapshot(CurrentPresetSnapshot actual, CurrentPresetSnapshot expected)
    {
        if (actual.RawStatePayloadHex != expected.RawStatePayloadHex || actual.RawDetailPayloadHex != expected.RawDetailPayloadHex)
            throw new IOException("The amplifier changed during tuning. Reconnect to read its current settings; nothing was saved.");
    }
}
