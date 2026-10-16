namespace Iamp.Protocol;

/// <summary>Live F15i mixer updates preserve the current preset index and every unrelated state byte.</summary>
public static class MixerCommands
{
    public static byte[] CreateLevel(ReadOnlySpan<byte> currentState, MixerChannel channel, byte level)
    {
        var state = MixerState.Parse(currentState);
        var mixer = RequireTarget(state, channel);
        if (level < mixer.Spec.Minimum || level > mixer.Spec.Maximum)
            throw new ArgumentOutOfRangeException(nameof(level));
        var payload = state.ToArray(); payload[0] = 0x13;
        foreach (var offset in mixer.Spec.LevelOffsets) payload[offset] = level;
        return payload;
    }
    public static byte[] CreateMute(ReadOnlySpan<byte> currentState, MixerChannel channel, bool muted)
    {
        var state = MixerState.Parse(currentState);
        var mixer = RequireTarget(state, channel);
        var payload = state.ToArray(); payload[0] = 0x13;
        payload[mixer.Spec.MuteOffset] = (byte)(muted ? 1 : 0);
        return payload;
    }
    private static MixerChannelState RequireTarget(MixerState state, MixerChannel channel)
    {
        var mixer = state.Get(channel);
        if (channel == MixerChannel.Master && !mixer.HasMatchingLevels)
            throw new NotSupportedException("Master editing requires matching captured paired levels.");
        return mixer;
    }
}
