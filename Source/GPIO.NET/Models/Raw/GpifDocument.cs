namespace GPIO.NET.Models.Raw;

/// <summary>
/// Raw GPIF data model intentionally mirrors source indirection patterns.
/// </summary>
public sealed class GpifDocument
{
    public ScoreInfo Score { get; init; } = new();

    public IReadOnlyList<GpifTrack> Tracks { get; init; } = Array.Empty<GpifTrack>();

    public IReadOnlyList<GpifMasterBar> MasterBars { get; init; } = Array.Empty<GpifMasterBar>();

    public IReadOnlyDictionary<int, GpifBar> BarsById { get; init; } = new Dictionary<int, GpifBar>();

    public IReadOnlyDictionary<int, GpifVoice> VoicesById { get; init; } = new Dictionary<int, GpifVoice>();

    public IReadOnlyDictionary<int, GpifBeat> BeatsById { get; init; } = new Dictionary<int, GpifBeat>();

    public IReadOnlyDictionary<int, GpifNote> NotesById { get; init; } = new Dictionary<int, GpifNote>();

    public IReadOnlyDictionary<int, GpifRhythm> RhythmsById { get; init; } = new Dictionary<int, GpifRhythm>();
}

public sealed class ScoreInfo
{
    public string Title { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public string Album { get; init; } = string.Empty;
}

public sealed class GpifTrack
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class GpifMasterBar
{
    public int Index { get; init; }

    public string Time { get; init; } = string.Empty;

    public string BarsReferenceList { get; init; } = string.Empty;

    public string AlternateEndings { get; init; } = string.Empty;

    public bool RepeatStart { get; init; }

    public bool RepeatEnd { get; init; }

    public int RepeatCount { get; init; }

    public string SectionLetter { get; init; } = string.Empty;

    public string SectionText { get; init; } = string.Empty;

    public string Jump { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;
}

public sealed class GpifBar
{
    public int Id { get; init; }

    public string VoicesReferenceList { get; init; } = string.Empty;
}

public sealed class GpifVoice
{
    public int Id { get; init; }

    public string BeatsReferenceList { get; init; } = string.Empty;
}

public sealed class GpifBeat
{
    public int Id { get; init; }

    public int RhythmRef { get; init; }

    public string NotesReferenceList { get; init; } = string.Empty;
}

public sealed class GpifNote
{
    public int Id { get; init; }

    public int? MidiPitch { get; init; }

    public GpifNoteArticulation Articulation { get; init; } = new();
}

public sealed class GpifNoteArticulation
{
    public bool LetRing { get; init; }

    public string Vibrato { get; init; } = string.Empty;

    public bool TieOrigin { get; init; }

    public bool TieDestination { get; init; }

    public int? Trill { get; init; }

    public int? Accent { get; init; }

    public bool AntiAccent { get; init; }

    public int? InstrumentArticulation { get; init; }
}

public sealed class GpifRhythm
{
    public int Id { get; init; }

    public string NoteValue { get; init; } = string.Empty;

    public int AugmentationDots { get; init; }

    public TupletRatio? PrimaryTuplet { get; init; }

    public TupletRatio? SecondaryTuplet { get; init; }
}

public sealed class TupletRatio
{
    public int Numerator { get; init; }

    public int Denominator { get; init; }
}
