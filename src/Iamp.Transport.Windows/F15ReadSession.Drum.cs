using Iamp.Protocol;

namespace Iamp.Transport.Windows;

public sealed record DrumSnapshot(CurrentPresetSnapshot Current, bool Playing);

public sealed partial class F15ReadSession
{
    private bool? drumPlaying;

    public Task<DrumSnapshot> EnterDrumAsync(CurrentPresetSnapshot expected, CancellationToken ct)
        => RunWorkflowAsync(async token =>
        {
            RequireDrumIdentity();
            var before = await ReadCurrentPresetCoreAsync(token);
            RequireUnchanged(before, expected);
            _ = DrumSettings.Parse(Convert.FromHexString(before.RawStatePayloadHex));
            await Task.Delay(TimeSpan.FromSeconds(3), token);
            drumPlaying = await DrumEntrySequence.EnterAsync(commands, token, responder: toolRequests);
            return new DrumSnapshot(before, drumPlaying.Value);
        }, ct);

    /// <summary>Mobile leaves Tools without stopping Drum playback.</summary>
    public Task ExitDrumAsync(CancellationToken ct)
        => RunWorkflowAsync(async token =>
        {
            var response = await QueryAsync([0x41, 0x00], p => p.Length > 0 && p[0] == 0x42, token);
            if (!response.AsSpan().SequenceEqual(new byte[] { 0x42, 0x01 }))
                throw new IOException("Drum Tools exit acknowledgement differs; reconnect before further edits.");
            return true;
        }, ct);

    public Task<CurrentPresetSnapshot> SetDrumPatternAsync(CurrentPresetSnapshot expected, byte id, CancellationToken ct)
        => RunWorkflowAsync(token => SetDrumStateCoreAsync(expected, s => DrumCommands.CreatePattern(s, id), token), ct);
    public Task<CurrentPresetSnapshot> SetDrumTempoAsync(CurrentPresetSnapshot expected, int tempo, CancellationToken ct)
        => RunWorkflowAsync(token => SetDrumStateCoreAsync(expected, s => DrumCommands.CreateTempo(s, tempo), token), ct);
    public Task<CurrentPresetSnapshot> SetDrumVolumeAsync(CurrentPresetSnapshot expected, int volume, CancellationToken ct)
        => RunWorkflowAsync(token => SetDrumStateCoreAsync(expected, s => DrumCommands.CreateVolume(s, volume), token), ct);

    private async Task<CurrentPresetSnapshot> SetDrumStateCoreAsync(CurrentPresetSnapshot expected,
        Func<byte[], byte[]> create, CancellationToken ct)
    {
        RequireDrumIdentity();
        var before = await ReadCurrentPresetCoreAsync(ct);
        RequireUnchanged(before, expected);
        var request = create(Convert.FromHexString(before.RawStatePayloadHex));
        var response = await QueryAsync(request, p => p.Length > 0 && p[0] == 0x14, ct);
        if (!response.AsSpan().SequenceEqual(new byte[] { 0x14, 0x01 }))
            throw new IOException("Drum edit acknowledgement differs. Reconnect before another edit; nothing was saved.");
        request[0] = 0x12;
        var after = await ReadCurrentPresetCoreAsync(ct);
        if (after.RawStatePayloadHex != Convert.ToHexString(request) || after.RawDetailPayloadHex != before.RawDetailPayloadHex)
            throw new IOException("Drum edit readback differs from the requested change or preserved preset. Reconnect; nothing was saved.");
        return after;
    }

    public Task<DrumSnapshot> SetDrumPlaybackAsync(CurrentPresetSnapshot expected, bool playing, CancellationToken ct)
        => ChangeDrumPlaybackAsync(expected, playing, 0, ct);

    public Task<DrumSnapshot> TapDrumAsync(CurrentPresetSnapshot expected, bool playing, ushort intervalMilliseconds, CancellationToken ct)
    {
        if (intervalMilliseconds == 0) throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));
        return ChangeDrumPlaybackAsync(expected, playing, intervalMilliseconds, ct);
    }

    private Task<DrumSnapshot> ChangeDrumPlaybackAsync(CurrentPresetSnapshot expected, bool playing, ushort interval, CancellationToken ct)
        => RunWorkflowAsync(async token =>
        {
            RequireDrumIdentity();
            if (drumPlaying is null) throw new InvalidOperationException("Open Drum to read its playback status first.");
            var before = await ReadCurrentPresetCoreAsync(token);
            RequireUnchanged(before, expected);
            var request = interval == 0 ? DrumCommands.CreatePlayback(playing) : DrumCommands.CreateTap(playing, interval);
            var response = await QueryAsync(request, p => p.Length > 0 && p[0] == 0x48, token);
            if (response.Length != 2 || response[1] > 1 || interval == 0 && response[1] != 0)
            { drumPlaying = null; throw new IOException("Drum playback response differs; reconnect before another command."); }
            var after = await ReadCurrentPresetCoreAsync(token);
            var beforeBytes = Convert.FromHexString(before.RawStatePayloadHex);
            var afterBytes = Convert.FromHexString(after.RawStatePayloadHex);
            _ = DrumSettings.Parse(afterBytes);
            if (interval != 0 && response[1] == 1)
            {
                // The amplifier calculates TAP tempo. Do not infer a BPM or rounding rule locally.
                beforeBytes[41] = afterBytes[41]; beforeBytes[42] = afterBytes[42];
            }
            if (!beforeBytes.AsSpan().SequenceEqual(afterBytes) || before.RawDetailPayloadHex != after.RawDetailPayloadHex)
            { drumPlaying = null; throw new IOException("Drum playback changed unrelated settings; reconnect before further edits."); }
            drumPlaying = playing;
            return new DrumSnapshot(after, playing);
        }, ct);

    private void RequireDrumIdentity()
    {
        if (confirmedIdentity?.Firmware != "V1.1.9" || confirmedIdentity.PresetSlots != 80)
            throw new NotSupportedException("Drum editing is verified only on F15i V1.1.9 with 80 preset slots.");
    }
}
