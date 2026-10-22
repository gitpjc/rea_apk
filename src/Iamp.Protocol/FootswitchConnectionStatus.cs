namespace Iamp.Protocol;

/// <summary>Observed amplifier-to-footswitch connection state, not stored pairing information.</summary>
public enum FootswitchConnectionStatus
{
    Unknown,
    Disconnected,
    Connected
}

/// <summary>
/// F15i V1.1.9 mobile off/on capture: 97 00 on F4 power-off and 97 01 on power-on.
/// No request, pairing state or additional status values are inferred from these notifications.
/// </summary>
public static class FootswitchStatusNotification
{
    public static FootswitchConnectionStatus Parse(ReadOnlySpan<byte> payload)
        =>TryParse(payload,out var status)?status:throw new FormatException("Footswitch notification differs from the observed 97 00/01 schema.");

    public static bool TryParse(ReadOnlySpan<byte> payload,out FootswitchConnectionStatus status)
    {
        status=FootswitchConnectionStatus.Unknown;
        if(payload.Length!=2||payload[0]!=0x97||payload[1]>1)return false;
        status=payload[1]==1?FootswitchConnectionStatus.Connected:FootswitchConnectionStatus.Disconnected;
        return true;
    }
}

/// <summary>Session-local read-only status. Unknown until a valid notification is observed.</summary>
public sealed class FootswitchStatusMonitor
{
    private int current;
    public FootswitchConnectionStatus Status=>(FootswitchConnectionStatus)Volatile.Read(ref current);
    public event Action<FootswitchConnectionStatus>? StatusChanged;

    public bool Observe(ReadOnlySpan<byte> payload)
    {
        if(!FootswitchStatusNotification.TryParse(payload,out var status))return false;
        if(Interlocked.Exchange(ref current,(int)status)==(int)status)return true;
        var handlers=StatusChanged;
        if(handlers is not null)
            foreach(Action<FootswitchConnectionStatus> handler in handlers.GetInvocationList())
                try {handler(status);}catch(Exception){/* A consumer must not interrupt protocol notification delivery. */}
        return true;
    }
}
