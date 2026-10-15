namespace Iamp.Transport.Windows;

/// <summary>A complete authoritative read is available even though the requested
/// transition was not confirmed. Display it without claiming mutation success.</summary>
public sealed class EffectChainReadbackException(string message,CurrentPresetSnapshot actualSnapshot):IOException(message)
{
    public CurrentPresetSnapshot ActualSnapshot {get;}=actualSnapshot;
}
