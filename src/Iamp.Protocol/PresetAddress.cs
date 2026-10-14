namespace Iamp.Protocol;

/// <summary>Zero-based banks with four slots A-D; wire indices remain one-based.</summary>
public readonly record struct PresetAddress
{
    public int WireIndex { get; }
    public int Bank => (WireIndex - 1) / 4;
    public char Slot => (char)('A' + (WireIndex - 1) % 4);
    public string Label => $"{Bank}{Slot}";
    private PresetAddress(int wireIndex) => WireIndex = wireIndex;
    public static PresetAddress FromWireIndex(int index) => index is >= 1 and <= 80 ? new(index) :
        throw new ArgumentOutOfRangeException(nameof(index));
}
