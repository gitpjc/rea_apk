namespace Iamp.Protocol;

/// <summary>A captured tuner note and raw deviation step, distinct from the 17 visible segments. No verified cents or frequency value.</summary>
public readonly record struct TunerReading(byte NoteCode, byte MeterPosition)
{
    public bool HasSignal => NoteCode != 0;
    public string NoteName => NoteCode switch
    {
        0 => "",
        1 => "B",
        2 => "C",
        3 => "C#",
        4 => "D",
        5 => "D#",
        6 => "E",
        7 => "F",
        8 => "F#",
        9 => "G",
        10 => "G#",
        11 => "A",
        12 => "A#",
        _ => ""
    };

    // Raw 16 is centered. Mobile reserves the center display, then groups pairs of deviation steps.
    // Keep the original byte; clamp only the display projection, never treat raw bytes as segment indices.
    public int DisplayPosition
    {
        get
        {
            var deviation = (int)MeterPosition - 16;
            return deviation == 0 ? 8 : Math.Clamp(8 - Math.Sign(deviation) * (1 + Math.Abs(deviation) / 2), 0, 16);
        }
    }
    public bool IsInTune => HasSignal && MeterPosition is >= 15 and <= 17;
    public string Direction => !HasSignal ? "No signal" : IsInTune ? "Perfect!" : MeterPosition < 16 ? "Low!" : "High!";
    public IReadOnlyList<int> DisplayPositions => !HasSignal ? [] : MeterPosition switch
    {
        15 => [8, 9],
        16 => Enumerable.Range(0, 17).ToArray(),
        17 => [7, 8],
        _ => [DisplayPosition]
    };

    public static bool TryParse(ReadOnlySpan<byte> payload, out TunerReading? reading)
    {
        reading = null;
        if (payload.Length != 3 || payload[0] != 0x45 || payload[1] > 12 || (payload[1] == 0 && payload[2] != 0))
            return false;
        reading = new(payload[1], payload[2]);
        return true;
    }
}
