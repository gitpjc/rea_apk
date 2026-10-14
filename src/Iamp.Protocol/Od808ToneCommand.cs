namespace Iamp.Protocol;

/// <summary>Compatibility wrapper for the shared captured-parameter encoder.</summary>
public static class Od808ToneCommand
{
    public static byte[] Create(PresetEffectBlock block, ushort tone)
        => ObservedParameterCommands.Create(block, ObservedParameter.OdTone, tone);
}
