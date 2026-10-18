namespace Iamp.Protocol;

public enum TunerOutputMode { Mute = 1, Bypass = 2 }

/// <summary>Captured F15i tuner settings in the complete current-state response; other state bytes remain opaque.</summary>
public sealed record TunerSettings(int ReferenceHertz, TunerOutputMode OutputMode)
{
    public const int MinimumHertz = 430;
    public const int MaximumHertz = 445;

    public static TunerSettings Parse(ReadOnlySpan<byte> currentState)
    {
        if (currentState.Length != 92 || currentState[0] != 0x12 || currentState[1] is < 1 or > 80)
            throw new NotSupportedException("Tuner settings require a complete captured 92-byte current state and valid preset slot.");
        if (currentState[44] > MaximumHertz - MinimumHertz || currentState[45] is not (1 or 2))
            throw new NotSupportedException("Tuner state differs from the captured reference-pitch range or output modes.");
        return new(MinimumHertz + currentState[44], (TunerOutputMode)currentState[45]);
    }
}

/// <summary>Captured tuner mode commands and live settings updates. Settings preserve every unrelated state byte.</summary>
public static class TunerCommands
{
    public static byte[] CreateReference(ReadOnlySpan<byte> currentState, int referenceHertz)
    {
        _ = TunerSettings.Parse(currentState);
        if (referenceHertz is < TunerSettings.MinimumHertz or > TunerSettings.MaximumHertz)
            throw new ArgumentOutOfRangeException(nameof(referenceHertz));
        var payload = currentState.ToArray();
        payload[0] = 0x13;
        payload[44] = (byte)(referenceHertz - TunerSettings.MinimumHertz);
        return payload;
    }

    public static byte[] CreateOutputMode(ReadOnlySpan<byte> currentState, TunerOutputMode outputMode)
    {
        _ = TunerSettings.Parse(currentState);
        if (outputMode is not (TunerOutputMode.Mute or TunerOutputMode.Bypass))
            throw new ArgumentOutOfRangeException(nameof(outputMode));
        var payload = currentState.ToArray();
        payload[0] = 0x13;
        payload[45] = (byte)outputMode;
        return payload;
    }

    public static byte[] CreateEnter() => [0x41, 0x02];
    public static byte[] CreateExit() => [0x41, 0x00];
}
