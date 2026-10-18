namespace Iamp.Protocol;

public sealed record DrumPattern(byte Id, string Name)
{
    public string DisplayName => $"{Id + 1:00} {Name}";
}

/// <summary>All 70 labels and zero-based identifiers observed in mobile iAMP on F15i V1.1.9.</summary>
public static class DrumPatternCatalog
{
    // POP010 is the mobile app's actual tenth label.
    private static readonly string[] Names =
    [
        "POP01",
        "POP02",
        "POP03",
        "POP04",
        "POP05",
        "POP06",
        "POP07",
        "POP08",
        "POP09",
        "POP010",
        "ROCK01",
        "ROCK02",
        "ROCK03",
        "ROCK04",
        "ROCK05",
        "ROCK06",
        "ROCK07",
        "ROCK08",
        "ROCK09",
        "ROCK10",
        "BLUES01",
        "BLUES02",
        "BLUES03",
        "BLUES04",
        "BLUES05",
        "BLUES06",
        "BLUES07",
        "BLUES08",
        "BLUES09",
        "BLUES10",
        "FUNK01",
        "FUNK02",
        "FUNK03",
        "FUNK04",
        "FUNK05",
        "FUNK06",
        "FUNK07",
        "FUNK08",
        "FUNK09",
        "FUNK10",
        "JAZZ01",
        "JAZZ02",
        "JAZZ03",
        "JAZZ04",
        "JAZZ05",
        "JAZZ06",
        "JAZZ07",
        "JAZZ08",
        "JAZZ09",
        "JAZZ10",
        "METAL01",
        "METAL02",
        "METAL03",
        "METAL04",
        "METAL05",
        "METAL06",
        "METAL07",
        "METAL08",
        "METAL09",
        "METAL10",
        "METRONOME01",
        "METRONOME02",
        "METRONOME03",
        "METRONOME04",
        "METRONOME05",
        "METRONOME06",
        "METRONOME07",
        "METRONOME08",
        "METRONOME09",
        "METRONOME10"
    ];
    public static IReadOnlyList<DrumPattern> All { get; } = Array.AsReadOnly(
        Names.Select((name, index) => new DrumPattern((byte)index, name)).ToArray());

    public static DrumPattern Get(byte id) => id < All.Count ? All[id]
        : throw new ArgumentOutOfRangeException(nameof(id));
}
