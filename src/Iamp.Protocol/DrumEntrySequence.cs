namespace Iamp.Protocol;

/// <summary>Observed Tools entry reports stopped/playing Drum state in 49 00/01 before the 4A reply.</summary>
public static class DrumEntrySequence
{
    public static async Task<bool> EnterAsync(CommandSession commands, CancellationToken ct,
        TimeSpan? readinessTimeout = null, ToolRequestResponder? responder = null)
    {
        var timeout = readinessTimeout ?? TimeSpan.FromSeconds(15);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(readinessTimeout));
        using var owned = responder is null ? new ToolRequestResponder(commands) : null;
        var requests = responder ?? owned!;
        var status = new TaskCompletionSource<(bool Playing, Task Acknowledgement)>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnStatus(byte[] p)
        {
            if (p.Length == 2 && p[0] == 0x49 && p[1] <= 1)
                status.TrySetResult((p[1] == 1, requests.LastAcknowledgement));
        }
        commands.UnsolicitedPayloadReceived += OnStatus;
        try
        {
            var response = await commands.QueryAsync([0x41, 0x01], p => p.Length > 0 && p[0] == 0x42, ct);
            if (!response.AsSpan().SequenceEqual(new byte[] { 0x42, 0x01 }))
                throw new IOException("Drum Tools entry acknowledgement differs; do not repeat the transition.");
            var observed = await status.Task.WaitAsync(timeout, ct);
            await observed.Acknowledgement.WaitAsync(timeout, ct);
            return observed.Playing;
        }
        finally { commands.UnsolicitedPayloadReceived -= OnStatus; }
    }
}
