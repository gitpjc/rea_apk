namespace Iamp.Protocol;

public sealed class CurrentPresetChangedException(string message) : IOException(message);

/// <summary>Validates the state/detail/state read sequence without interpreting unknown state fields.</summary>
public static class CurrentPresetReadGuard
{
    public static void ValidateState(ReadOnlySpan<byte> state)
    {
        if (state.Length != 92 || state[0] != 0x12 || state[1] is < 1 or > 80)
            throw new FormatException("Current-state response differs from the observed 92-byte schema.");
    }

    public static DecodedPreset Validate(ReadOnlySpan<byte> before, ReadOnlySpan<byte> detail,
        ReadOnlySpan<byte> after)
    {
        ValidateState(before);
        ValidateState(after);
        var preset = PresetDataParser.Parse(detail);
        if (preset.WireIndex != before[1])
            throw new CurrentPresetChangedException("Preset details do not match the selected preset; refresh the snapshot.");
        if (!before.SequenceEqual(after))
            throw new CurrentPresetChangedException("The amplifier state changed during the read; refresh the snapshot before editing.");
        return preset;
    }

    /// <summary>Retry only a completely received but inconsistent read; never replay mutations or transport failures.</summary>
    public static async Task<(byte[] State, byte[] Detail, DecodedPreset Preset)> ReadStableAsync(
        Func<CancellationToken, Task<byte[]>> readState,
        Func<byte, CancellationToken, Task<byte[]>> readDetail, CancellationToken ct)
    {
        for(var attempt=0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var before=await readState(ct);ValidateState(before);
            var detail=await readDetail(before[1],ct);
            var after=await readState(ct);
            ct.ThrowIfCancellationRequested();
            try {return (before,detail,Validate(before,detail,after));}
            catch(CurrentPresetChangedException) when(attempt<2)
            {
                await Task.Delay(100,ct);
            }
        }
    }
}
