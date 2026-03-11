namespace Motif.Models;

public sealed class Score : ExtensibleModel
{
    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    public IReadOnlyList<Track> Tracks { get; set; } = Array.Empty<Track>();

    /// <summary>
    /// Score-owned master-bar timeline used for playback/navigation and other timeline-global state.
    /// </summary>
    public IReadOnlyList<TimelineBar> TimelineBars { get; set; } = Array.Empty<TimelineBar>();

    /// <summary>
    /// True when playback should treat the score as beginning with a pickup bar.
    /// </summary>
    public bool Anacrusis { get; set; }

    /// <summary>
    /// Ordered master-bar indices representing derived navigation-aware playback traversal.
    /// Call <see cref="Motif.ScoreNavigation.RebuildPlaybackSequence(Motif.Models.Score)"/> after
    /// traversal-affecting edits, or <see cref="Motif.ScoreNavigation.EnsurePlaybackSequence(Motif.Models.Score)"/>
    /// when reading the cached value.
    /// </summary>
    public IReadOnlyList<int> PlaybackMasterBarSequence { get; set; } = Array.Empty<int>();
}

public sealed class TimelineBar : ExtensibleModel
{
    public int Index { get; set; }

    public string TimeSignature { get; set; } = string.Empty;

    public bool DoubleBar { get; set; }

    public bool FreeTime { get; set; }

    public string TripletFeel { get; set; } = string.Empty;

    public bool RepeatStart { get; set; }

    public bool RepeatStartAttributePresent { get; set; }

    public bool RepeatEnd { get; set; }

    public bool RepeatEndAttributePresent { get; set; }

    public int RepeatCount { get; set; }

    public bool RepeatCountAttributePresent { get; set; }

    public string AlternateEndings { get; set; } = string.Empty;

    public string SectionLetter { get; set; } = string.Empty;

    public string SectionText { get; set; } = string.Empty;

    public bool HasExplicitEmptySection { get; set; }

    public string Jump { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> DirectionProperties { get; set; } = new Dictionary<string, string>();

    public int? KeyAccidentalCount { get; set; }

    public string KeyMode { get; set; } = string.Empty;

    public string KeyTransposeAs { get; set; } = string.Empty;

    public IReadOnlyList<FermataMetadata> Fermatas { get; set; } = Array.Empty<FermataMetadata>();

    public IReadOnlyDictionary<string, int> XProperties { get; set; } = new Dictionary<string, int>();
}

public sealed class Track : ExtensibleModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<Staff> Staves { get; set; } = Array.Empty<Staff>();
}

public sealed class Staff : ExtensibleModel
{
    public int StaffIndex { get; set; }

    public IReadOnlyList<StaffMeasure> Measures { get; set; } = Array.Empty<StaffMeasure>();
}

public sealed class StaffMeasure : ExtensibleModel
{
    public int Index { get; set; }

    public int StaffIndex { get; set; }

    public string Clef { get; set; } = string.Empty;

    public string SimileMark { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> BarProperties { get; set; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> BarXProperties { get; set; } = new Dictionary<string, int>();

    public IReadOnlyList<Voice> Voices { get; set; } = Array.Empty<Voice>();

    public IReadOnlyList<Beat> Beats { get; set; } = Array.Empty<Beat>();
}

public sealed class Voice : ExtensibleModel
{
    public int VoiceIndex { get; set; }

    public IReadOnlyList<Beat> Beats { get; set; } = Array.Empty<Beat>();
}

public sealed class TupletRatio
{
    public int Numerator { get; set; }

    public int Denominator { get; set; }
}

public sealed class FermataMetadata
{
    public string Type { get; set; } = string.Empty;

    public string Offset { get; set; } = string.Empty;

    public decimal? Length { get; set; }
}

public sealed class Beat : ExtensibleModel
{
    public int Id { get; set; }

    public string GraceType { get; set; } = string.Empty;

    public string Dynamic { get; set; } = string.Empty;

    public string Wah { get; set; } = string.Empty;

    public string Golpe { get; set; } = string.Empty;

    public bool Slashed { get; set; }

    public string Hairpin { get; set; } = string.Empty;

    public string Ottavia { get; set; } = string.Empty;

    public bool? LegatoOrigin { get; set; }

    public bool? LegatoDestination { get; set; }

    public string PickStrokeDirection { get; set; } = string.Empty;

    public string VibratoWithTremBarStrength { get; set; } = string.Empty;

    public bool Slapped { get; set; }

    public bool Popped { get; set; }

    public bool PalmMuted { get; set; }

    public bool Brush { get; set; }

    public bool BrushIsUp { get; set; }

    public bool Arpeggio { get; set; }

    public int? BrushDurationTicks { get; set; }

    public bool Rasgueado { get; set; }

    public string RasgueadoPattern { get; set; } = string.Empty;

    public bool DeadSlapped { get; set; }

    public bool Tremolo { get; set; }

    public string TremoloValue { get; set; } = string.Empty;

    public string FreeText { get; set; } = string.Empty;

    public WhammyBar? WhammyBar { get; set; }

    public IReadOnlyDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> XProperties { get; set; } = new Dictionary<string, int>();

    public decimal Offset { get; set; }

    public decimal Duration { get; set; }

    public IReadOnlyList<Note> Notes { get; set; } = Array.Empty<Note>();

    public IReadOnlyList<int> MidiPitches { get; set; } = Array.Empty<int>();
}

public sealed class Note : ExtensibleModel
{
    public int Id { get; set; }

    public int? Velocity { get; set; }

    public int? MidiPitch { get; set; }

    public PitchValue? ConcertPitch { get; set; }

    public PitchValue? TransposedPitch { get; set; }

    public bool ShowStringNumber { get; set; }

    public int? StringNumber { get; set; }

    public IReadOnlyDictionary<string, int> XProperties { get; set; } = new Dictionary<string, int>();

    public decimal Duration { get; set; }

    public bool TieExtendedFromPrevious { get; set; }

    public NoteArticulation Articulation { get; set; } = new();
}

public sealed class PitchValue
{
    public string Step { get; set; } = string.Empty;

    public string Accidental { get; set; } = string.Empty;

    public int? Octave { get; set; }
}

public sealed class NoteArticulation
{
    public string LeftFingering { get; set; } = string.Empty;

    public string RightFingering { get; set; } = string.Empty;

    public string Ornament { get; set; } = string.Empty;

    public bool LetRing { get; set; }

    public string Vibrato { get; set; } = string.Empty;

    public bool TieOrigin { get; set; }

    public bool TieDestination { get; set; }

    public int? Trill { get; set; }

    public TrillSpeedKind TrillSpeed { get; set; } = TrillSpeedKind.None;

    public int? Accent { get; set; }

    public bool AntiAccent { get; set; }

    public string AntiAccentValue { get; set; } = string.Empty;

    public bool PalmMuted { get; set; }

    public bool Muted { get; set; }

    public bool Tapped { get; set; }

    public bool LeftHandTapped { get; set; }

    public bool HopoOrigin { get; set; }

    public bool HopoDestination { get; set; }

    public HopoTypeKind HopoType { get; set; } = HopoTypeKind.None;

    public int? HopoOriginNoteId { get; set; }

    public int? HopoDestinationNoteId { get; set; }

    public IReadOnlyList<SlideType> Slides { get; set; } = Array.Empty<SlideType>();

    public Harmonic? Harmonic { get; set; }

    public Bend? Bend { get; set; }
}

[Flags]
public enum SlideType
{
    None = 0,
    Shift = 1,
    Legato = 2,
    OutDown = 4,
    OutUp = 8,
    IntoFromBelow = 16,
    IntoFromAbove = 32,
    Unknown64 = 64,
    Unknown128 = 128
}

public enum HarmonicTypeKind
{
    Unknown = 0,
    NoHarmonic = 1,
    Natural = 2,
    Artificial = 3,
    Pinch = 4,
    Tap = 5,
    Semi = 6,
    Feedback = 7
}

public enum BendTypeKind
{
    Unknown = 0,
    None = 1,
    Hold = 2,
    Prebend = 3,
    Bend = 4,
    Release = 5,
    BendAndRelease = 6,
    PrebendAndBend = 7,
    PrebendAndRelease = 8
}

public enum HopoTypeKind
{
    None = 0,
    HammerOn = 1,
    PullOff = 2,
    Legato = 3
}

public sealed class Harmonic
{
    public int? Type { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public HarmonicTypeKind Kind { get; set; } = HarmonicTypeKind.Unknown;

    public decimal? Fret { get; set; }

    public bool Enabled { get; set; }
}

public sealed class Bend
{
    public bool Enabled { get; set; }

    public BendTypeKind Type { get; set; } = BendTypeKind.Unknown;

    public decimal? OriginOffset { get; set; }

    public decimal? OriginValue { get; set; }

    public decimal? MiddleOffset1 { get; set; }

    public decimal? MiddleOffset2 { get; set; }

    public decimal? MiddleValue { get; set; }

    public decimal? DestinationOffset { get; set; }

    public decimal? DestinationValue { get; set; }
}

public sealed class WhammyBar
{
    public bool Enabled { get; set; }

    public bool Extended { get; set; }

    public decimal? OriginValue { get; set; }

    public decimal? MiddleValue { get; set; }

    public decimal? DestinationValue { get; set; }

    public decimal? OriginOffset { get; set; }

    public decimal? MiddleOffset1 { get; set; }

    public decimal? MiddleOffset2 { get; set; }

    public decimal? DestinationOffset { get; set; }
}

public enum TrillSpeedKind
{
    None = 0,
    Sixteenth = 1,
    ThirtySecond = 2,
    SixtyFourth = 3,
    OneHundredTwentyEighth = 4
}
