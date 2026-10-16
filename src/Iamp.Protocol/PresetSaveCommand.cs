using System.Text;

namespace Iamp.Protocol;

/// <summary>Captured F15i save envelope. Effects remain in the live preset; only destination and name are sent.</summary>
public static class PresetSaveCommand
{
    public const int MaximumNameLength = 20;

    public static byte[] EncodeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaximumNameLength || name.Any(c => c is < ' ' or > '~'))
            throw new ArgumentException("Use 1–20 printable ASCII characters. Non-ASCII preset names are not yet verified.", nameof(name));
        var bytes = new byte[MaximumNameLength];
        Encoding.ASCII.GetBytes(name).CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] Create(string firmware, ReadOnlySpan<byte> state, ReadOnlySpan<byte> detail, byte destination, string name)
    {
        ValidateSource(firmware, state, detail);
        ValidateDestination(destination);
        var payload = new byte[22]; payload[0] = 0x2F; payload[1] = destination;
        EncodeName(name).CopyTo(payload, 2);
        return payload;
    }

    public static byte[] ExpectedDetail(ReadOnlySpan<byte> detail, byte destination, string name)
    {
        _ = PresetDataParser.Parse(detail); ValidateDestination(destination);
        var result = detail.ToArray(); result[1] = destination;
        EncodeName(name).CopyTo(result, 2);
        return result;
    }

    public static byte[] ExpectedState(ReadOnlySpan<byte> state, byte destination)
    {
        CurrentPresetReadGuard.ValidateState(state); ValidateDestination(destination);
        var result = state.ToArray(); result[1] = destination;
        return result;
    }

    public static void ValidateSource(string firmware, ReadOnlySpan<byte> state, ReadOnlySpan<byte> detail)
    {
        if (firmware != "V1.1.9") throw new NotSupportedException("Preset saving is verified on F15i V1.1.9 only.");
        var decoded = CurrentPresetReadGuard.Validate(state, detail, state);
        var active = EffectChainCommands.Active(decoded.Blocks);
        if (active.Any(block => ObservedParameterCommands.Profile(block) == 0))
            throw new NotSupportedException("Preset saving requires verified built-in effect layouts. This chain contains an unsupported layout.");
    }

    private static void ValidateDestination(byte destination)
    {
        if (destination is < 1 or > 80) throw new ArgumentOutOfRangeException(nameof(destination));
    }
}
