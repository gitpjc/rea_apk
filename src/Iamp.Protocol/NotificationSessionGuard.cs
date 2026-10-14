namespace Iamp.Protocol;

/// <summary>An observed transfer-state notification permanently invalidated the current command session.</summary>
public sealed class TransferStateChangedException : IOException
{
    public TransferStateChangedException():base(NotificationSessionGuard.FaultMessage) {}
}

/// <summary>Rejects a session after an observed transfer-state discriminator; status values never re-enable it.
/// Callers synchronize this decoder/guard with request registration and write initiation.</summary>
public sealed class NotificationSessionGuard
{
    public const string FaultMessage="The amplifier reported transfer-state notification 9B. This session is invalid; no further commands will be sent. Automatic replay is disabled.";
    private readonly FrameStreamDecoder decoder=new();
    public bool IsInvalid {get;private set;}

    public IReadOnlyList<ProtocolFrame> ProcessNotification(ReadOnlySpan<byte> bytes)
    {
        ThrowIfInvalid();
        var frames=decoder.Push(bytes);
        // Inspect the whole decoded notification before releasing any matching response.
        if(frames.Any(frame=>frame.Payload.Length>0 && frame.Payload[0]==0x9B)) {
            IsInvalid=true;decoder.Reset();
            ThrowIfInvalid();
        }
        return frames;
    }
    public void ThrowIfInvalid()
    {
        if(IsInvalid)throw new TransferStateChangedException();
    }
    /// <summary>Drops buffered fragments only. Transfer-state invalidation remains permanent.</summary>
    public void ResetDecoder()=>decoder.Reset();
}
