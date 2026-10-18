namespace Iamp.Protocol;

/// <summary>The observed mobile Tools-to-Tuner transition. A reply to 49 00 must follow that request.</summary>
public static class TunerEntrySequence
{
    public static async Task EnterAsync(CommandSession commands, CancellationToken ct,
        TimeSpan? readinessTimeout = null, ToolRequestResponder? responder = null)
    {
        var timeout = readinessTimeout ?? TimeSpan.FromSeconds(15);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(readinessTimeout));
        using var ownedResponder = responder is null ? new ToolRequestResponder(commands) : null;
        var requests = responder ?? ownedResponder!;
        var requested = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnRequest(byte[] payload)
        {
            if (payload.AsSpan().SequenceEqual(new byte[] { 0x49, 0x00 })) requested.TrySetResult(requests.LastAcknowledgement);
        }
        commands.UnsolicitedPayloadReceived += OnRequest;
        try
        {
            await ChangeModeAsync(commands, [0x41, 0x01], ct);
            var acknowledged = await requested.Task.WaitAsync(timeout, ct);
            await acknowledged.WaitAsync(timeout, ct);
            await ChangeModeAsync(commands, TunerCommands.CreateEnter(), ct);
        }
        finally { commands.UnsolicitedPayloadReceived -= OnRequest; }
    }

    private static async Task ChangeModeAsync(CommandSession commands, byte[] request, CancellationToken ct)
    {
        var response = await commands.QueryAsync(request, p => p.Length > 0 && p[0] == 0x42, ct);
        if (!response.AsSpan().SequenceEqual(new byte[] { 0x42, 0x01 }))
            throw new IOException("Tuner entry acknowledgement differs; do not repeat the transition.");
    }
}
