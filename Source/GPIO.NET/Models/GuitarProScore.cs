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
    public string ScoreXml { get; init; } = string.Empty;

    public string[] ExplicitEmptyOptionalElements { get; init; } = Array.Empty<string>();

    public string GpVersion { get; init; } = string.Empty;

    public string GpRevisionXml { get; init; } = string.Empty;

    public string GpRevisionRequired { get; init; } = string.Empty;

    public string GpRevisionRecommended { get; init; } = string.Empty;

    public string GpRevisionValue { get; init; } = string.Empty;

    public string EncodingDescription { get; init; } = string.Empty;

    public string ScoreViewsXml { get; init; } = string.Empty;

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

    public string PageSetupXml { get; init; } = string.Empty;

    public string MultiVoice { get; init; } = string.Empty;

    public string BackingTrackXml { get; init; } = string.Empty;

    public string AudioTracksXml { get; init; } = string.Empty;

    public string AssetsXml { get; init; } = string.Empty;
}

public sealed class TrackMetadata
{
    public string Xml { get; init; } = string.Empty;

    public string ShortName { get; init; } = string.Empty;

    public bool HasExplicitEmptyShortName { get; init; }

    public string Color { get; init; } = string.Empty;

    public string SystemsDefaultLayout { get; init; } = string.Empty;

    public string SystemsLayout { get; init; } = string.Empty;

    public bool HasExplicitEmptySystemsLayout { get; init; }

    public decimal? PalmMute { get; init; }

    public decimal? AutoAccentuation { get; init; }

    public bool AutoBrush { get; init; }

    public bool LetRingThroughout { get; init; }

    public string PlayingStyle { get; init; } = string.Empty;

    public bool UseOneChannelPerString { get; init; }

    public int? IconId { get; init; }

    public int? ForcedSound { get; init; }

    public int[] TuningPitches { get; init; } = Array.Empty<int>();

    public string TuningInstrument { get; init; } = string.Empty;

    public string TuningLabel { get; init; } = string.Empty;

    public bool? TuningLabelVisible { get; init; }

    public bool HasTrackTuningProperty { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public string InstrumentSetXml { get; init; } = string.Empty;

    public string StavesXml { get; init; } = string.Empty;

    public string SoundsXml { get; init; } = string.Empty;

    public string RseXml { get; init; } = string.Empty;

    public string NotationPatchXml { get; init; } = string.Empty;

    public InstrumentSetMetadata InstrumentSet { get; init; } = new();

    public IReadOnlyList<SoundMetadata> Sounds { get; init; } = Array.Empty<SoundMetadata>();

    public RseMetadata Rse { get; init; } = new();

    public string PlaybackStateXml { get; init; } = string.Empty;

    public string AudioEngineStateXml { get; init; } = string.Empty;

    public PlaybackStateMetadata PlaybackState { get; init; } = new();

    public AudioEngineStateMetadata AudioEngineState { get; init; } = new();

    public IReadOnlyList<AutomationMetadata> Automations { get; init; } = Array.Empty<AutomationMetadata>();

    public string MidiConnectionXml { get; init; } = string.Empty;

    public string LyricsXml { get; init; } = string.Empty;

    public string AutomationsXml { get; init; } = string.Empty;

    public string TransposeXml { get; init; } = string.Empty;

    public MidiConnectionMetadata MidiConnection { get; init; } = new();

    public LyricsMetadata Lyrics { get; init; } = new();

    public TransposeMetadata Transpose { get; init; } = new();

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

    public IReadOnlyList<InstrumentElementMetadata> Elements { get; init; } = Array.Empty<InstrumentElementMetadata>();
}

public sealed class InstrumentElementMetadata
{
    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string SoundbankName { get; init; } = string.Empty;

    public IReadOnlyList<InstrumentArticulationMetadata> Articulations { get; init; } = Array.Empty<InstrumentArticulationMetadata>();
}

public sealed class InstrumentArticulationMetadata
{
    public string Name { get; init; } = string.Empty;

    public int? StaffLine { get; init; }

    public string Noteheads { get; init; } = string.Empty;

    public string TechniquePlacement { get; init; } = string.Empty;

    public string TechniqueSymbol { get; init; } = string.Empty;

    public string InputMidiNumbers { get; init; } = string.Empty;

    public string OutputRseSound { get; init; } = string.Empty;

    public int? OutputMidiNumber { get; init; }
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

    public SoundRseMetadata Rse { get; init; } = new();
}

public sealed class SoundRseMetadata
{
    public string SoundbankPatch { get; init; } = string.Empty;

    public string SoundbankSet { get; init; } = string.Empty;

    public string ElementsSettingsXml { get; init; } = string.Empty;

    public SoundRsePickupsMetadata Pickups { get; init; } = new();

    public IReadOnlyList<RseEffectMetadata> EffectChain { get; init; } = Array.Empty<RseEffectMetadata>();
}

public sealed class SoundRsePickupsMetadata
{
    public string OverloudPosition { get; init; } = string.Empty;

    public string Volumes { get; init; } = string.Empty;

    public string Tones { get; init; } = string.Empty;
}

public sealed class RseMetadata
{
    public string Bank { get; init; } = string.Empty;

    public string ChannelStripVersion { get; init; } = string.Empty;

    public string ChannelStripParameters { get; init; } = string.Empty;

    public IReadOnlyList<AutomationMetadata> Automations { get; init; } = Array.Empty<AutomationMetadata>();
}

public sealed class RseEffectMetadata
{
    public string Id { get; init; } = string.Empty;

    public bool Bypass { get; init; }

    public string Parameters { get; init; } = string.Empty;
}

public sealed class PlaybackStateMetadata
{
    public string Value { get; init; } = string.Empty;
}

public sealed class AudioEngineStateMetadata
{
    public string Value { get; init; } = string.Empty;
}

public sealed class MidiConnectionMetadata
{
    public int? Port { get; init; }

    public int? PrimaryChannel { get; init; }

    public int? SecondaryChannel { get; init; }

    public bool? ForceOneChannelPerString { get; init; }
}

public sealed class LyricsMetadata
{
    public bool? Dispatched { get; init; }

    public IReadOnlyList<LyricsLineMetadata> Lines { get; init; } = Array.Empty<LyricsLineMetadata>();
}

public sealed class LyricsLineMetadata
{
    public string Text { get; init; } = string.Empty;

    public int? Offset { get; init; }
}

public sealed class TransposeMetadata
{
    public int? Chromatic { get; init; }

    public int? Octave { get; init; }
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
    public string Xml { get; init; } = string.Empty;

    public int[] TrackIds { get; init; } = Array.Empty<int>();

    public string AutomationsXml { get; init; } = string.Empty;

    public IReadOnlyList<AutomationMetadata> Automations { get; init; } = Array.Empty<AutomationMetadata>();

    /// <summary>
    /// Unified, time-ordered automation events synthesized from master-track and per-track automation lists.
    /// </summary>
    public IReadOnlyList<AutomationTimelineEventMetadata> AutomationTimeline { get; init; } = Array.Empty<AutomationTimelineEventMetadata>();

    /// <summary>
    /// Ordered dynamic change points synthesized from beat-level dynamic markings.
    /// </summary>
    public IReadOnlyList<DynamicEventMetadata> DynamicMap { get; init; } = Array.Empty<DynamicEventMetadata>();

    public bool Anacrusis { get; init; }

    public string RseXml { get; init; } = string.Empty;

    public MasterTrackRseMetadata Rse { get; init; } = new();

    public IReadOnlyList<TempoEventMetadata> TempoMap { get; init; } = Array.Empty<TempoEventMetadata>();
}

public sealed class MasterTrackRseMetadata
{
    public IReadOnlyList<RseEffectMetadata> MasterEffects { get; init; } = Array.Empty<RseEffectMetadata>();
}

public sealed class TempoEventMetadata
{
    public int? Bar { get; init; }

    public int? Position { get; init; }

    public decimal? Bpm { get; init; }

    public int? DenominatorHint { get; init; }
}

public sealed class AutomationTimelineEventMetadata
{
    public AutomationScopeKind Scope { get; init; } = AutomationScopeKind.MasterTrack;

    public int? TrackId { get; init; }

    public string Type { get; init; } = string.Empty;

    public bool? Linear { get; init; }

    public int? Bar { get; init; }

    public int? Position { get; init; }

    public bool? Visible { get; init; }

    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Parsed first numeric token from <see cref="Value"/>, when available.
    /// </summary>
    public decimal? NumericValue { get; init; }

    /// <summary>
    /// Parsed second integer token from <see cref="Value"/>, when available.
    /// Tempo automations commonly use this as a denominator/reference hint.
    /// </summary>
    public int? ReferenceHint { get; init; }

    /// <summary>
    /// Tempo-specific projection when <see cref="Type"/> is tempo.
    /// </summary>
    public TempoEventMetadata? Tempo { get; init; }
}

public enum AutomationScopeKind
{
    MasterTrack = 0,
    Track = 1
}

public sealed class DynamicEventMetadata
{
    public int TrackId { get; init; }

    public int MeasureIndex { get; init; }

    public int VoiceIndex { get; init; }

    public int BeatId { get; init; }

    public decimal BeatOffset { get; init; }

    public string Dynamic { get; init; } = string.Empty;

    public DynamicKind Kind { get; init; } = DynamicKind.Unknown;
}

public enum DynamicKind
{
    Unknown = 0,
    PPP = 1,
    PP = 2,
    P = 3,
    MP = 4,
    MF = 5,
    F = 6,
    FF = 7,
    FFF = 8
}

public sealed class MeasureModel
{
    public string MasterBarXml { get; init; } = string.Empty;

    public string BarXml { get; init; } = string.Empty;

    public int Index { get; init; }

    public string TimeSignature { get; init; } = string.Empty;

    public bool DoubleBar { get; init; }

    public bool FreeTime { get; init; }

    public string TripletFeel { get; init; } = string.Empty;

    public int SourceBarId { get; init; }

    public string Clef { get; init; } = string.Empty;

    public string SimileMark { get; init; } = string.Empty;

    public bool RepeatStart { get; init; }

    public bool RepeatStartAttributePresent { get; init; }

    public bool RepeatEnd { get; init; }

    public bool RepeatEndAttributePresent { get; init; }

    public int RepeatCount { get; init; }

    public bool RepeatCountAttributePresent { get; init; }

    public string AlternateEndings { get; init; } = string.Empty;

    public string SectionLetter { get; init; } = string.Empty;

    public string SectionText { get; init; } = string.Empty;

    public bool HasExplicitEmptySection { get; init; }

    public string Jump { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> DirectionProperties { get; init; } = new Dictionary<string, string>();

    public string DirectionsXml { get; init; } = string.Empty;

    public int? KeyAccidentalCount { get; init; }

    public string KeyMode { get; init; } = string.Empty;

    public string KeyTransposeAs { get; init; } = string.Empty;

    public IReadOnlyList<FermataMetadata> Fermatas { get; init; } = Array.Empty<FermataMetadata>();

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public string MasterBarXPropertiesXml { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> BarProperties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> BarXProperties { get; init; } = new Dictionary<string, int>();

    public string BarXPropertiesXml { get; init; } = string.Empty;

    public IReadOnlyList<MeasureStaffModel> AdditionalStaffBars { get; init; } = Array.Empty<MeasureStaffModel>();

    public IReadOnlyList<MeasureVoiceModel> Voices { get; init; } = Array.Empty<MeasureVoiceModel>();

    public IReadOnlyList<BeatModel> Beats { get; init; } = Array.Empty<BeatModel>();
}

public sealed class MeasureStaffModel
{
    public string BarXml { get; init; } = string.Empty;

    public int StaffIndex { get; init; }

    public int SourceBarId { get; init; }

    public string Clef { get; init; } = string.Empty;

    public string SimileMark { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> BarProperties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> BarXProperties { get; init; } = new Dictionary<string, int>();

    public string BarXPropertiesXml { get; init; } = string.Empty;

    public IReadOnlyList<MeasureVoiceModel> Voices { get; init; } = Array.Empty<MeasureVoiceModel>();

    public IReadOnlyList<BeatModel> Beats { get; init; } = Array.Empty<BeatModel>();
}

public sealed class MeasureVoiceModel
{
    public string Xml { get; init; } = string.Empty;

    public int VoiceIndex { get; init; }

    public int SourceVoiceId { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> DirectionTags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<BeatModel> Beats { get; init; } = Array.Empty<BeatModel>();
}

public sealed class RhythmShapeModel
{
    public string Xml { get; init; } = string.Empty;

    public string NoteValue { get; init; } = string.Empty;

    public int AugmentationDots { get; init; }

    public bool AugmentationDotUsesCountAttribute { get; init; }

    public int[] AugmentationDotCounts { get; init; } = Array.Empty<int>();

    public TupletRatioModel? PrimaryTuplet { get; init; }

    public TupletRatioModel? SecondaryTuplet { get; init; }
}

public sealed class TupletRatioModel
{
    public int Numerator { get; init; }

    public int Denominator { get; init; }
}

public sealed class FermataMetadata
{
    public string Type { get; init; } = string.Empty;

    public string Offset { get; init; } = string.Empty;

    public decimal? Length { get; init; }
}

public sealed class BeatModel
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public int SourceRhythmId { get; init; } = -1;

    public RhythmShapeModel? SourceRhythm { get; init; }

    public string GraceType { get; init; } = string.Empty;

    public string Dynamic { get; init; } = string.Empty;

    public string TransposedPitchStemOrientation { get; init; } = string.Empty;

    public string UserTransposedPitchStemOrientation { get; init; } = string.Empty;

    public bool HasTransposedPitchStemOrientationUserDefinedElement { get; init; }

    public string ConcertPitchStemOrientation { get; init; } = string.Empty;

    public string Wah { get; init; } = string.Empty;

    public string Golpe { get; init; } = string.Empty;

    public string Fadding { get; init; } = string.Empty;

    public bool Slashed { get; init; }

    public string Hairpin { get; init; } = string.Empty;

    public string Variation { get; init; } = string.Empty;

    public string Ottavia { get; init; } = string.Empty;

    public bool? LegatoOrigin { get; init; }

    public bool? LegatoDestination { get; init; }

    public string LyricsXml { get; init; } = string.Empty;

    public string PickStrokeDirection { get; init; } = string.Empty;

    public string VibratoWithTremBarStrength { get; init; } = string.Empty;

    public bool Slapped { get; init; }

    public bool Popped { get; init; }

    public bool PalmMuted { get; init; }

    public bool Brush { get; init; }

    public bool BrushIsUp { get; init; }

    public bool Arpeggio { get; init; }

    public int? BrushDurationTicks { get; init; }

    public string BrushDurationXPropertyId { get; init; } = string.Empty;

    public bool HasExplicitBrushDurationXProperty { get; init; }

    public bool Rasgueado { get; init; }

    public string RasgueadoPattern { get; init; } = string.Empty;

    public bool DeadSlapped { get; init; }

    public bool Tremolo { get; init; }

    public string TremoloValue { get; init; } = string.Empty;

    public string ChordId { get; init; } = string.Empty;

    public string FreeText { get; init; } = string.Empty;

    public WhammyBarModel? WhammyBar { get; init; }

    public bool WhammyUsesElement { get; init; }

    public bool WhammyExtendUsesElement { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> VoiceProperties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> VoiceDirectionTags { get; init; } = Array.Empty<string>();

    public decimal Offset { get; init; }

    public decimal Duration { get; init; }

    public IReadOnlyList<NoteModel> Notes { get; init; } = Array.Empty<NoteModel>();

    public IReadOnlyList<int> MidiPitches { get; init; } = Array.Empty<int>();
}

public sealed class NoteModel
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public int? Velocity { get; init; }

    public int? MidiPitch { get; init; }

    public int? SourceMidiPitch { get; init; }

    public int? SourceTransposedMidiPitch { get; init; }

    public PitchValueModel? ConcertPitch { get; init; }

    public PitchValueModel? TransposedPitch { get; init; }

    public int? SourceFret { get; init; }

    public int? SourceStringNumber { get; init; }

    public bool ShowStringNumber { get; init; }

    public int? StringNumber { get; init; }

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; init; } = string.Empty;

    public decimal Duration { get; set; }

    public bool TieExtendedFromPrevious { get; set; }

    public NoteArticulationModel Articulation { get; init; } = new();
}

public sealed class PitchValueModel
{
    public string Step { get; init; } = string.Empty;

    public string Accidental { get; init; } = string.Empty;

    public int? Octave { get; init; }
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

    public string AntiAccentValue { get; init; } = string.Empty;

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
