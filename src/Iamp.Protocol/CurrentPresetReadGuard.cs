namespace Iamp.Protocol;

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
            throw new IOException("Preset details do not match the selected preset; refresh the snapshot.");
        if (!before.SequenceEqual(after))
            throw new IOException("The amplifier state changed during the read; refresh the snapshot before editing.");
        return preset;
    }
}
