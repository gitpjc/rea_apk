namespace Iamp.Protocol;

/// <summary>Explicit mobile-observed F4 pairing control; acknowledgement is not proof of a connection.</summary>
public static class FootswitchCommands
{
    /// <summary>Mobile startup read observed to publish separate 97 status and 04 reply payloads.</summary>
    public static byte[] CreateStatusReadCommand()=>[0x03];

    public static void RequireStatusReadReply(ReadOnlySpan<byte> payload)
    {
        if(payload.Length!=6||payload[0]!=0x04)
            throw new IOException("Amplifier startup-status reply differs from the observed six-byte 04 schema.");
        // The remaining global fields are deliberately uninterpreted; only 97 reports F4 connection.
    }

    public static byte[] CreatePairingCommand(bool pairing)=>[0x95,(byte)(pairing?1:0)];

    public static void RequirePairingAcknowledgement(ReadOnlySpan<byte> payload)
    {
        if(!payload.SequenceEqual(new byte[]{0x96,1}))
            throw new IOException("Footswitch pairing acknowledgement differs from the observed 96 01 response. Do not repeat the command automatically.");
    }
}
