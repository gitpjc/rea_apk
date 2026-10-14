namespace Iamp.Protocol;

/// <summary>Observed F15i battery reply. Power-state byte remains uninterpreted.</summary>
public sealed record BatteryStatus(byte Percentage,byte RawPowerState)
{
    public static BatteryStatus Parse(ReadOnlySpan<byte> payload)
    {
        if(payload.Length!=3 || payload[0]!=0x92 || payload[1]>100)
            throw new FormatException("Battery reply differs from the observed schema.");
        return new(payload[1],payload[2]);
    }
}
