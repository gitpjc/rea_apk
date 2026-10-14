namespace Iamp.Protocol;

/// <summary>Whole-state selection packet observed for the two captured V1.1.9 slots.</summary>
public static class PresetSelectionCommand
{
    public static byte[] CreateForAnySlot(ReadOnlySpan<byte> currentState, byte targetIndex)
    {
        CurrentPresetReadGuard.ValidateState(currentState);
        _ = PresetAddress.FromWireIndex(targetIndex);
        var request = currentState.ToArray(); request[0] = 0x13; request[1] = targetIndex;
        return request;
    }

    public static void RequireObservedPreset(int index, ReadOnlySpan<byte> detail)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(detail));
        var expected = index switch
        {
            4 => "644DEE6D3AD07F3E5C2F6ED7670E37220B28D71892B2605763C820B5E19C30CE",
            5 => "56B8474E5064A7D6F20635A13FB5E725ED2D7F5E613B5EA88AB52EA496474714",
            _ => throw new NotSupportedException("Only captured presets 4 and 5 are supported.")
        };
        if (hash != expected)
            throw new NotSupportedException("This preset differs from the captured baseline. Recall is disabled to preserve unknown edits.");
    }

    public static byte[] Create(ReadOnlySpan<byte> currentState, byte targetIndex)
    {
        if (currentState.Length != 92 || currentState[0] != 0x12 || currentState[1] is not (4 or 5))
            throw new NotSupportedException("Selection requires the captured 92-byte state and current slot 4 or 5.");
        if (targetIndex is not (4 or 5))
            throw new ArgumentOutOfRangeException(nameof(targetIndex), "Only captured slots 4 and 5 are supported.");
        var request = currentState.ToArray();
        request[0] = 0x13; request[1] = targetIndex;
        return request;
    }
}
