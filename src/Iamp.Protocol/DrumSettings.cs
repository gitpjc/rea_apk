using System.Buffers.Binary;

namespace Iamp.Protocol;

/// <summary>Drum fields observed in complete F15i V1.1.9 current-state snapshots.</summary>
public sealed record DrumSettings(ushort Tempo, byte Volume, DrumPattern Pattern)
{
    public const int MinimumTempo = 40, MaximumTempo = 260;
    // The mobile popup's observed top selection is 99.
    public const int MaximumVolume = 99;

    public static DrumSettings Parse(ReadOnlySpan<byte> state)
    {
        if (state.Length != 92 || state[0] != 0x12 || state[1] is < 1 or > 80)
            throw new NotSupportedException("Drum requires a complete captured current state.");
        var tempo = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(41, 2));
        if (tempo is < MinimumTempo or > MaximumTempo || state[40] > MaximumVolume ||
            state[43] >= DrumPatternCatalog.All.Count)
            throw new NotSupportedException("Drum settings differ from the observed mobile ranges.");
        return new(tempo, state[40], DrumPatternCatalog.Get(state[43]));
    }
}
