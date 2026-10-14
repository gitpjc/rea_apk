namespace Iamp.Protocol;

/// <summary>Compatibility wrapper for the shared captured-parameter encoder.</summary>
public static class NoiseKillerCommand
{
    public static byte[] Create(PresetEffectBlock block, ushort threshold)
        => ObservedParameterCommands.Create(block, ObservedParameter.NoiseThreshold, threshold);
}
