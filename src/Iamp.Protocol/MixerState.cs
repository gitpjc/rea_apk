using System.Globalization;

namespace Iamp.Protocol;

public enum MixerChannel { Bluetooth, UsbRecording, Instrument, Master }

public sealed record MixerChannelSpec(MixerChannel Channel, string Name, int MuteOffset,
    IReadOnlyList<int> LevelOffsets, decimal Offset)
{
    public byte Minimum => 0;
    public byte Maximum => 72;
    public decimal Scale => 0.5m;
    public string Unit => "dB";
    public decimal DisplayValue(byte raw)
    {
        if (raw > Maximum) throw new ArgumentOutOfRangeException(nameof(raw));
        return raw * Scale + Offset;
    }
    public string Format(byte raw) => DisplayValue(raw).ToString("0.0", CultureInfo.InvariantCulture);
    public bool TryParse(string text, out byte raw)
    {
        raw = 0;
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var value) || value < Offset || value > Maximum * Scale + Offset) return false;
        var encoded = (value - Offset) / Scale;
        if (encoded != decimal.Truncate(encoded)) return false;
        raw = (byte)encoded;
        return true;
    }
}

public sealed record MixerChannelState(MixerChannelSpec Spec, bool Muted, IReadOnlyList<byte> RawLevels)
{
    public byte RawLevel => RawLevels[0];
    public bool HasMatchingLevels => RawLevels.All(value => value == RawLevel);
}

/// <summary>Captured F15i current-state mixer fields. Unmapped bytes remain opaque.</summary>
public sealed class MixerState
{
    private readonly byte[] state;
    private MixerState(byte[] state, IReadOnlyList<MixerChannelState> channels)
    { this.state = state; Channels = channels; }

    public static IReadOnlyList<MixerChannelSpec> Specs { get; } = Array.AsReadOnly<MixerChannelSpec>([
        new(MixerChannel.Bluetooth, "Bluetooth", 29, Array.AsReadOnly<int>([30]), -36m),
        new(MixerChannel.UsbRecording, "USB REC", 31, Array.AsReadOnly<int>([32]), -36m),
        new(MixerChannel.Instrument, "Instrument", 33, Array.AsReadOnly<int>([34]), -36m),
        new(MixerChannel.Master, "Master", 35, Array.AsReadOnly<int>([37, 38]), -30m)
    ]);
    public byte WireIndex => state[1];
    public string RawStatePayloadHex => Convert.ToHexString(state);
    public IReadOnlyList<MixerChannelState> Channels { get; }
    public static MixerChannelSpec GetSpec(MixerChannel channel) => Specs.FirstOrDefault(s => s.Channel == channel)
        ?? throw new ArgumentOutOfRangeException(nameof(channel));
    public MixerChannelState Get(MixerChannel channel)
    { _ = GetSpec(channel); return Channels.Single(s => s.Spec.Channel == channel); }
    public byte[] ToArray() => state.ToArray();

    public static MixerState Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 92 || payload[0] != 0x12 || payload[1] is < 1 or > 80)
            throw new NotSupportedException("Mixer requires a complete captured 92-byte current state and valid preset slot.");
        var copy = payload.ToArray();
        var channels = new List<MixerChannelState>();
        foreach (var spec in Specs)
        {
            if (copy[spec.MuteOffset] > 1 || spec.LevelOffsets.Any(offset => copy[offset] > spec.Maximum))
                throw new NotSupportedException("Mixer state differs from the captured Boolean flags or level ranges.");
            channels.Add(new(spec, copy[spec.MuteOffset] == 1,
                Array.AsReadOnly(spec.LevelOffsets.Select(offset => copy[offset]).ToArray())));
        }
        return new(copy, channels.AsReadOnly());
    }
}
