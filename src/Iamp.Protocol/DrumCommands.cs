using System.Buffers.Binary;

namespace Iamp.Protocol;

/// <summary>Observed mobile Drum edits preserve every unrelated current-state byte.</summary>
public static class DrumCommands
{
    public static byte[] CreatePattern(ReadOnlySpan<byte> currentState, byte patternId)
    {
        _ = DrumPatternCatalog.Get(patternId);
        var payload = CreateStateEdit(currentState);
        payload[43] = patternId;
        return payload;
    }

    public static byte[] CreateTempo(ReadOnlySpan<byte> currentState, int tempo)
    {
        if (tempo is < DrumSettings.MinimumTempo or > DrumSettings.MaximumTempo)
            throw new ArgumentOutOfRangeException(nameof(tempo));
        var payload = CreateStateEdit(currentState);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(41, 2), (ushort)tempo);
        return payload;
    }

    public static byte[] CreateVolume(ReadOnlySpan<byte> currentState, int volume)
    {
        if (volume is < 0 or > DrumSettings.MaximumVolume) throw new ArgumentOutOfRangeException(nameof(volume));
        var payload = CreateStateEdit(currentState);
        payload[40] = (byte)volume;
        return payload;
    }

    public static byte[] CreatePlayback(bool playing) => [0x47, (byte)(playing ? 1 : 0), 0, 0];

    public static byte[] CreateTap(bool playing, ushort intervalMilliseconds)
    {
        if (intervalMilliseconds == 0) throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));
        var payload = CreatePlayback(playing);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), intervalMilliseconds);
        return payload;
    }

    private static byte[] CreateStateEdit(ReadOnlySpan<byte> state)
    {
        _ = DrumSettings.Parse(state);
        var payload = state.ToArray(); payload[0] = 0x13;
        return payload;
    }
}
