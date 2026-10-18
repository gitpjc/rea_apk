namespace Iamp.Protocol;

/// <summary>The observed mobile Tools-to-Tuner transition. A reply to 49 00 must follow that request.</summary>
public static class TunerEntrySequence
{
    public static async Task EnterAsync(CommandSession commands, CancellationToken ct,
        TimeSpan? readinessTimeout = null, ToolRequestResponder? responder = null)
    {
        var timeout = readinessTimeout ?? TimeSpan.FromSeconds(15);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(readinessTimeout));
        _ = await DrumEntrySequence.EnterAsync(commands, ct, timeout, responder);
        await ChangeModeAsync(commands, TunerCommands.CreateEnter(), ct);
    }

    private static async Task ChangeModeAsync(CommandSession commands, byte[] request, CancellationToken ct)
    {
        var response = await commands.QueryAsync(request, p => p.Length > 0 && p[0] == 0x42, ct);
        if (!response.AsSpan().SequenceEqual(new byte[] { 0x42, 0x01 }))
            throw new IOException("Tuner entry acknowledgement differs; do not repeat the transition.");
    }
}
