namespace GPIO.NET.Models;

public sealed class GuitarProScore
{
    public string Title { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public string Album { get; init; } = string.Empty;

    public ScoreMetadata Metadata { get; init; } = new();

    public MasterTrackMetadata MasterTrack { get; init; } = new();

    public IReadOnlyList<TrackModel> Tracks { get; init; } = Array.Empty<TrackModel>();

    /// <summary>
    /// Ordered master-bar indices representing navigation-aware playback traversal.
    /// </summary>
    public IReadOnlyList<int> PlaybackMasterBarSequence { get; init; } = Array.Empty<int>();
}

public sealed class TrackModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public TrackMetadata Metadata { get; init; } = new();

    public IReadOnlyList<MeasureModel> Measures { get; init; } = Array.Empty<MeasureModel>();
}

public sealed class ScoreMetadata
{
    public string SubTitle { get; init; } = string.Empty;

    public string Words { get; init; } = string.Empty;

    public string Music { get; init; } = string.Empty;

    public string WordsAndMusic { get; init; } = string.Empty;

    public string Copyright { get; init; } = string.Empty;

    public string Tabber { get; init; } = string.Empty;

    public string Instructions { get; init; } = string.Empty;

    public string Notices { get; init; } = string.Empty;

    public string FirstPageHeader { get; init; } = string.Empty;

    public string FirstPageFooter { get; init; } = string.Empty;

    public string PageHeader { get; init; } = string.Empty;

    public string PageFooter { get; init; } = string.Empty;

    public string ScoreSystemsDefaultLayout { get; init; } = string.Empty;

    public string ScoreSystemsLayout { get; init; } = string.Empty;

    public string ScoreZoomPolicy { get; init; } = string.Empty;

    public string ScoreZoom { get; init; } = string.Empty;

    public string MultiVoice { get; init; } = string.Empty;
}

public sealed class TrackMetadata
{
    public string ShortName { get; init; } = string.Empty;

    public string Color { get; init; } = string.Empty;

    public string SystemsDefaultLayout { get; init; } = string.Empty;

    public string SystemsLayout { get; init; } = string.Empty;

    public decimal? PalmMute { get; init; }

    public decimal? AutoAccentuation { get; init; }

    public bool AutoBrush { get; init; }

    public string PlayingStyle { get; init; } = string.Empty;

    public bool UseOneChannelPerString { get; init; }

    public int? IconId { get; init; }

    public int? ForcedSound { get; init; }

    public int[] TuningPitches { get; init; } = Array.Empty<int>();

    public string TuningInstrument { get; init; } = string.Empty;

    public string TuningLabel { get; init; } = string.Empty;

    public bool? TuningLabelVisible { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public string InstrumentSetXml { get; init; } = string.Empty;

    public string StavesXml { get; init; } = string.Empty;

    public string SoundsXml { get; init; } = string.Empty;

    public string RseXml { get; init; } = string.Empty;

    public InstrumentSetMetadata InstrumentSet { get; init; } = new();

    public IReadOnlyList<SoundMetadata> Sounds { get; init; } = Array.Empty<SoundMetadata>();

    public RseMetadata Rse { get; init; } = new();

    public string PlaybackStateXml { get; init; } = string.Empty;

    public string AudioEngineStateXml { get; init; } = string.Empty;

    public PlaybackStateMetadata PlaybackState { get; init; } = new();

    public IReadOnlyList<AutomationMetadata> Automations { get; init; } = Array.Empty<AutomationMetadata>();

    public string MidiConnectionXml { get; init; } = string.Empty;

    public string LyricsXml { get; init; } = string.Empty;

    public string AutomationsXml { get; init; } = string.Empty;

    public string TransposeXml { get; init; } = string.Empty;

    public IReadOnlyList<StaffMetadata> Staffs { get; init; } = Array.Empty<StaffMetadata>();
}

public sealed class StaffMetadata
{
    public int? Id { get; init; }

    public string Cref { get; init; } = string.Empty;

    public int[] TuningPitches { get; init; } = Array.Empty<int>();

    public int? CapoFret { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public string Xml { get; init; } = string.Empty;
}

public sealed class InstrumentSetMetadata
{
    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public int? LineCount { get; init; }
}

public sealed class SoundMetadata
{
    public string Name { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public int? MidiLsb { get; init; }

    public int? MidiMsb { get; init; }

    public int? MidiProgram { get; init; }
}

public sealed class RseMetadata
{
    public string ChannelStripVersion { get; init; } = string.Empty;

    public string ChannelStripParameters { get; init; } = string.Empty;
}

public sealed class PlaybackStateMetadata
{
    public string Value { get; init; } = string.Empty;
}

public sealed class AutomationMetadata
{
    public string Type { get; init; } = string.Empty;

    public bool? Linear { get; init; }

    public int? Bar { get; init; }

    public int? Position { get; init; }

    public bool? Visible { get; init; }

    public string Value { get; init; } = string.Empty;
}

public sealed class MasterTrackMetadata
{
    public int[] TrackIds { get; init; } = Array.Empty<int>();

    public IReadOnlyList<AutomationMetadata> Automations { get; init; } = Array.Empty<AutomationMetadata>();

    public bool Anacrusis { get; init; }

    public string RseXml { get; init; } = string.Empty;

    public IReadOnlyList<TempoEventMetadata> TempoMap { get; init; } = Array.Empty<TempoEventMetadata>();
}

public sealed class TempoEventMetadata
{
    public int? Bar { get; init; }

    public int? Position { get; init; }

    public decimal? Bpm { get; init; }

    public int? DenominatorHint { get; init; }
}

public sealed class MeasureModel
{
    public int Index { get; init; }

    public string TimeSignature { get; init; } = string.Empty;

    public int SourceBarId { get; init; }

    public string Clef { get; init; } = string.Empty;

    public bool RepeatStart { get; init; }

    public bool RepeatEnd { get; init; }

    public int RepeatCount { get; init; }

    public string AlternateEndings { get; init; } = string.Empty;

    public string SectionLetter { get; init; } = string.Empty;

    public string SectionText { get; init; } = string.Empty;

    public string Jump { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> DirectionProperties { get; init; } = new Dictionary<string, string>();

    public int? KeyAccidentalCount { get; init; }

    public string KeyMode { get; init; } = string.Empty;

    public string KeyTransposeAs { get; init; } = string.Empty;

    public IReadOnlyList<FermataMetadata> Fermatas { get; init; } = Array.Empty<FermataMetadata>();

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, string> BarProperties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<MeasureVoiceModel> Voices { get; init; } = Array.Empty<MeasureVoiceModel>();

    public IReadOnlyList<BeatModel> Beats { get; init; } = Array.Empty<BeatModel>();
}

public sealed class MeasureVoiceModel
{
    public int VoiceIndex { get; init; }

    public int SourceVoiceId { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> DirectionTags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<BeatModel> Beats { get; init; } = Array.Empty<BeatModel>();
}

public sealed class FermataMetadata
{
    public string Type { get; init; } = string.Empty;

    public string Offset { get; init; } = string.Empty;

    public decimal? Length { get; init; }
}

public sealed class BeatModel
{
    public int Id { get; init; }

    public string GraceType { get; init; } = string.Empty;

    public string PickStrokeDirection { get; init; } = string.Empty;

    public string VibratoWithTremBarStrength { get; init; } = string.Empty;

    public bool Slapped { get; init; }

    public bool Popped { get; init; }

    public bool PalmMuted { get; init; }

    public bool Brush { get; init; }

    public bool BrushIsUp { get; init; }

    public bool Arpeggio { get; init; }

    public int? BrushDurationTicks { get; init; }

    public bool Rasgueado { get; init; }

    public bool DeadSlapped { get; init; }

    public bool Tremolo { get; init; }

    public string TremoloValue { get; init; } = string.Empty;

    public string ChordId { get; init; } = string.Empty;

    public string FreeText { get; init; } = string.Empty;

    public WhammyBarModel? WhammyBar { get; init; }

    public IReadOnlyDictionary<string, string> VoiceProperties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> VoiceDirectionTags { get; init; } = Array.Empty<string>();

    public decimal Offset { get; init; }

    public decimal Duration { get; init; }

    public IReadOnlyList<NoteModel> Notes { get; init; } = Array.Empty<NoteModel>();

    public IReadOnlyList<int> MidiPitches { get; init; } = Array.Empty<int>();
}

public sealed class NoteModel
{
    public int Id { get; init; }

    public int? MidiPitch { get; init; }

    public int? StringNumber { get; init; }

    public decimal Duration { get; set; }

    public bool TieExtendedFromPrevious { get; set; }

    public NoteArticulationModel Articulation { get; init; } = new();
}

public sealed class NoteArticulationModel
{
    public string LeftFingering { get; init; } = string.Empty;

    public string RightFingering { get; init; } = string.Empty;

    public string Ornament { get; init; } = string.Empty;

    public bool LetRing { get; init; }

    public string Vibrato { get; init; } = string.Empty;

    public bool TieOrigin { get; init; }

    public bool TieDestination { get; init; }

    public int? Trill { get; init; }

    public TrillSpeedKind TrillSpeed { get; init; } = TrillSpeedKind.None;

    public int? Accent { get; init; }

    public bool AntiAccent { get; init; }

    public int? InstrumentArticulation { get; init; }

    public bool PalmMuted { get; init; }

    public bool Muted { get; init; }

    public bool Tapped { get; init; }

    public bool LeftHandTapped { get; init; }

    public bool HopoOrigin { get; init; }

    public bool HopoDestination { get; init; }

    public HopoTypeKind HopoType { get; init; } = HopoTypeKind.None;

    public int? HopoOriginNoteId { get; init; }

    public int? HopoDestinationNoteId { get; init; }

    public int? SlideFlags { get; init; }

    public IReadOnlyList<SlideType> Slides { get; init; } = Array.Empty<SlideType>();

    public HarmonicModel? Harmonic { get; init; }

    public BendModel? Bend { get; init; }
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

public sealed class HarmonicModel
{
    public int? Type { get; init; }

    public string TypeName { get; init; } = string.Empty;

    public HarmonicTypeKind Kind { get; init; } = HarmonicTypeKind.Unknown;

    public decimal? Fret { get; init; }

    public bool Enabled { get; init; }
}

public sealed class BendModel
{
    public bool Enabled { get; init; }

    public BendTypeKind Type { get; init; } = BendTypeKind.Unknown;

    public decimal? OriginOffset { get; init; }

    public decimal? OriginValue { get; init; }

    public decimal? MiddleOffset1 { get; init; }

    public decimal? MiddleOffset2 { get; init; }

    public decimal? MiddleValue { get; init; }

    public decimal? DestinationOffset { get; init; }

    public decimal? DestinationValue { get; init; }
}

public sealed class WhammyBarModel
{
    public bool Enabled { get; init; }

    public bool Extended { get; init; }

    public decimal? OriginValue { get; init; }

    public decimal? MiddleValue { get; init; }

    public decimal? DestinationValue { get; init; }

    public decimal? OriginOffset { get; init; }

    public decimal? MiddleOffset1 { get; init; }

    public decimal? MiddleOffset2 { get; init; }

    public decimal? DestinationOffset { get; init; }
}

public enum TrillSpeedKind
{
    None = 0,
    Sixteenth = 1,
    ThirtySecond = 2,
    SixtyFourth = 3,
    OneHundredTwentyEighth = 4
}
