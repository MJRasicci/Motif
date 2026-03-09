namespace Motif.Models;

public sealed class GuitarProScore : ExtensibleModel
{
    public string Title { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    public ScoreMetadata Metadata { get; set; } = new();

    public MasterTrackMetadata MasterTrack { get; set; } = new();

    public IReadOnlyList<TrackModel> Tracks { get; set; } = Array.Empty<TrackModel>();

    /// <summary>
    /// Ordered master-bar indices representing navigation-aware playback traversal.
    /// </summary>
    public IReadOnlyList<int> PlaybackMasterBarSequence { get; set; } = Array.Empty<int>();
}

public sealed class TrackModel : ExtensibleModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public TrackMetadata Metadata { get; set; } = new();

    public IReadOnlyList<MeasureModel> Measures { get; set; } = Array.Empty<MeasureModel>();
}

public sealed class ScoreMetadata
{
    public string ScoreXml { get; set; } = string.Empty;

    public string[] ExplicitEmptyOptionalElements { get; set; } = Array.Empty<string>();

    public string GpVersion { get; set; } = string.Empty;

    public string GpRevisionXml { get; set; } = string.Empty;

    public string GpRevisionRequired { get; set; } = string.Empty;

    public string GpRevisionRecommended { get; set; } = string.Empty;

    public string GpRevisionValue { get; set; } = string.Empty;

    public string EncodingDescription { get; set; } = string.Empty;

    public string ScoreViewsXml { get; set; } = string.Empty;

    public string SubTitle { get; set; } = string.Empty;

    public string Words { get; set; } = string.Empty;

    public string Music { get; set; } = string.Empty;

    public string WordsAndMusic { get; set; } = string.Empty;

    public string Copyright { get; set; } = string.Empty;

    public string Tabber { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public string Notices { get; set; } = string.Empty;

    public string FirstPageHeader { get; set; } = string.Empty;

    public string FirstPageFooter { get; set; } = string.Empty;

    public string PageHeader { get; set; } = string.Empty;

    public string PageFooter { get; set; } = string.Empty;

    public string ScoreSystemsDefaultLayout { get; set; } = string.Empty;

    public string ScoreSystemsLayout { get; set; } = string.Empty;

    public string ScoreZoomPolicy { get; set; } = string.Empty;

    public string ScoreZoom { get; set; } = string.Empty;

    public string PageSetupXml { get; set; } = string.Empty;

    public string MultiVoice { get; set; } = string.Empty;

    public string BackingTrackXml { get; set; } = string.Empty;

    public string AudioTracksXml { get; set; } = string.Empty;

    public string AssetsXml { get; set; } = string.Empty;
}

public sealed class TrackMetadata
{
    public string Xml { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public bool HasExplicitEmptyShortName { get; set; }

    public string Color { get; set; } = string.Empty;

    public string SystemsDefaultLayout { get; set; } = string.Empty;

    public string SystemsLayout { get; set; } = string.Empty;

    public bool HasExplicitEmptySystemsLayout { get; set; }

    public decimal? PalmMute { get; set; }

    public decimal? AutoAccentuation { get; set; }

    public bool AutoBrush { get; set; }

    public bool LetRingThroughout { get; set; }

    public string PlayingStyle { get; set; } = string.Empty;

    public bool UseOneChannelPerString { get; set; }

    public int? IconId { get; set; }

    public int? ForcedSound { get; set; }

    public int[] TuningPitches { get; set; } = Array.Empty<int>();

    public string TuningInstrument { get; set; } = string.Empty;

    public string TuningLabel { get; set; } = string.Empty;

    public bool? TuningLabelVisible { get; set; }

    public bool HasTrackTuningProperty { get; set; }

    public IReadOnlyDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    public string InstrumentSetXml { get; set; } = string.Empty;

    public string StavesXml { get; set; } = string.Empty;

    public string SoundsXml { get; set; } = string.Empty;

    public string RseXml { get; set; } = string.Empty;

    public string NotationPatchXml { get; set; } = string.Empty;

    public InstrumentSetMetadata InstrumentSet { get; set; } = new();

    public IReadOnlyList<SoundMetadata> Sounds { get; set; } = Array.Empty<SoundMetadata>();

    public RseMetadata Rse { get; set; } = new();

    public string PlaybackStateXml { get; set; } = string.Empty;

    public string AudioEngineStateXml { get; set; } = string.Empty;

    public PlaybackStateMetadata PlaybackState { get; set; } = new();

    public AudioEngineStateMetadata AudioEngineState { get; set; } = new();

    public IReadOnlyList<AutomationMetadata> Automations { get; set; } = Array.Empty<AutomationMetadata>();

    public string MidiConnectionXml { get; set; } = string.Empty;

    public string LyricsXml { get; set; } = string.Empty;

    public string AutomationsXml { get; set; } = string.Empty;

    public string TransposeXml { get; set; } = string.Empty;

    public MidiConnectionMetadata MidiConnection { get; set; } = new();

    public LyricsMetadata Lyrics { get; set; } = new();

    public TransposeMetadata Transpose { get; set; } = new();

    public IReadOnlyList<StaffMetadata> Staffs { get; set; } = Array.Empty<StaffMetadata>();
}

public sealed class StaffMetadata
{
    public int? Id { get; set; }

    public string Cref { get; set; } = string.Empty;

    public int[] TuningPitches { get; set; } = Array.Empty<int>();

    public int? CapoFret { get; set; }

    public IReadOnlyDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    public string Xml { get; set; } = string.Empty;
}

public sealed class InstrumentSetMetadata
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int? LineCount { get; set; }

    public IReadOnlyList<InstrumentElementMetadata> Elements { get; set; } = Array.Empty<InstrumentElementMetadata>();
}

public sealed class InstrumentElementMetadata
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string SoundbankName { get; set; } = string.Empty;

    public IReadOnlyList<InstrumentArticulationMetadata> Articulations { get; set; } = Array.Empty<InstrumentArticulationMetadata>();
}

public sealed class InstrumentArticulationMetadata
{
    public string Name { get; set; } = string.Empty;

    public int? StaffLine { get; set; }

    public string Noteheads { get; set; } = string.Empty;

    public string TechniquePlacement { get; set; } = string.Empty;

    public string TechniqueSymbol { get; set; } = string.Empty;

    public string InputMidiNumbers { get; set; } = string.Empty;

    public string OutputRseSound { get; set; } = string.Empty;

    public int? OutputMidiNumber { get; set; }
}

public sealed class SoundMetadata
{
    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? MidiLsb { get; set; }

    public int? MidiMsb { get; set; }

    public int? MidiProgram { get; set; }

    public SoundRseMetadata Rse { get; set; } = new();
}

public sealed class SoundRseMetadata
{
    public string SoundbankPatch { get; set; } = string.Empty;

    public string SoundbankSet { get; set; } = string.Empty;

    public string ElementsSettingsXml { get; set; } = string.Empty;

    public SoundRsePickupsMetadata Pickups { get; set; } = new();

    public IReadOnlyList<RseEffectMetadata> EffectChain { get; set; } = Array.Empty<RseEffectMetadata>();
}

public sealed class SoundRsePickupsMetadata
{
    public string OverloudPosition { get; set; } = string.Empty;

    public string Volumes { get; set; } = string.Empty;

    public string Tones { get; set; } = string.Empty;
}

public sealed class RseMetadata
{
    public string Bank { get; set; } = string.Empty;

    public string ChannelStripVersion { get; set; } = string.Empty;

    public string ChannelStripParameters { get; set; } = string.Empty;

    public IReadOnlyList<AutomationMetadata> Automations { get; set; } = Array.Empty<AutomationMetadata>();
}

public sealed class RseEffectMetadata
{
    public string Id { get; set; } = string.Empty;

    public bool Bypass { get; set; }

    public string Parameters { get; set; } = string.Empty;
}

public sealed class PlaybackStateMetadata
{
    public string Value { get; set; } = string.Empty;
}

public sealed class AudioEngineStateMetadata
{
    public string Value { get; set; } = string.Empty;
}

public sealed class MidiConnectionMetadata
{
    public int? Port { get; set; }

    public int? PrimaryChannel { get; set; }

    public int? SecondaryChannel { get; set; }

    public bool? ForceOneChannelPerString { get; set; }
}

public sealed class LyricsMetadata
{
    public bool? Dispatched { get; set; }

    public IReadOnlyList<LyricsLineMetadata> Lines { get; set; } = Array.Empty<LyricsLineMetadata>();
}

public sealed class LyricsLineMetadata
{
    public string Text { get; set; } = string.Empty;

    public int? Offset { get; set; }
}

public sealed class TransposeMetadata
{
    public int? Chromatic { get; set; }

    public int? Octave { get; set; }
}

public sealed class AutomationMetadata
{
    public string Type { get; set; } = string.Empty;

    public bool? Linear { get; set; }

    public int? Bar { get; set; }

    public int? Position { get; set; }

    public bool? Visible { get; set; }

    public string Value { get; set; } = string.Empty;
}

public sealed class MasterTrackMetadata
{
    public string Xml { get; set; } = string.Empty;

    public int[] TrackIds { get; set; } = Array.Empty<int>();

    public string AutomationsXml { get; set; } = string.Empty;

    public IReadOnlyList<AutomationMetadata> Automations { get; set; } = Array.Empty<AutomationMetadata>();

    /// <summary>
    /// Unified, time-ordered automation events synthesized from master-track and per-track automation lists.
    /// </summary>
    public IReadOnlyList<AutomationTimelineEventMetadata> AutomationTimeline { get; set; } = Array.Empty<AutomationTimelineEventMetadata>();

    /// <summary>
    /// Ordered dynamic change points synthesized from beat-level dynamic markings.
    /// </summary>
    public IReadOnlyList<DynamicEventMetadata> DynamicMap { get; set; } = Array.Empty<DynamicEventMetadata>();

    public bool Anacrusis { get; set; }

    public string RseXml { get; set; } = string.Empty;

    public MasterTrackRseMetadata Rse { get; set; } = new();

    public IReadOnlyList<TempoEventMetadata> TempoMap { get; set; } = Array.Empty<TempoEventMetadata>();
}

public sealed class MasterTrackRseMetadata
{
    public IReadOnlyList<RseEffectMetadata> MasterEffects { get; set; } = Array.Empty<RseEffectMetadata>();
}

public sealed class TempoEventMetadata
{
    public int? Bar { get; set; }

    public int? Position { get; set; }

    public decimal? Bpm { get; set; }

    public int? DenominatorHint { get; set; }
}

public sealed class AutomationTimelineEventMetadata
{
    public AutomationScopeKind Scope { get; set; } = AutomationScopeKind.MasterTrack;

    public int? TrackId { get; set; }

    public string Type { get; set; } = string.Empty;

    public bool? Linear { get; set; }

    public int? Bar { get; set; }

    public int? Position { get; set; }

    public bool? Visible { get; set; }

    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Parsed first numeric token from <see cref="Value"/>, when available.
    /// </summary>
    public decimal? NumericValue { get; set; }

    /// <summary>
    /// Parsed second integer token from <see cref="Value"/>, when available.
    /// Tempo automations commonly use this as a denominator/reference hint.
    /// </summary>
    public int? ReferenceHint { get; set; }

    /// <summary>
    /// Tempo-specific projection when <see cref="Type"/> is tempo.
    /// </summary>
    public TempoEventMetadata? Tempo { get; set; }
}

public enum AutomationScopeKind
{
    MasterTrack = 0,
    Track = 1
}

public sealed class DynamicEventMetadata
{
    public int TrackId { get; set; }

    public int MeasureIndex { get; set; }

    public int VoiceIndex { get; set; }

    public int BeatId { get; set; }

    public decimal BeatOffset { get; set; }

    public string Dynamic { get; set; } = string.Empty;

    public DynamicKind Kind { get; set; } = DynamicKind.Unknown;
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

public sealed class MeasureModel : ExtensibleModel
{
    public string MasterBarXml { get; set; } = string.Empty;

    public string BarXml { get; set; } = string.Empty;

    public int Index { get; set; }

    public string TimeSignature { get; set; } = string.Empty;

    public bool DoubleBar { get; set; }

    public bool FreeTime { get; set; }

    public string TripletFeel { get; set; } = string.Empty;

    public int SourceBarId { get; set; }

    public string Clef { get; set; } = string.Empty;

    public string SimileMark { get; set; } = string.Empty;

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

    public string DirectionsXml { get; set; } = string.Empty;

    public int? KeyAccidentalCount { get; set; }

    public string KeyMode { get; set; } = string.Empty;

    public string KeyTransposeAs { get; set; } = string.Empty;

    public IReadOnlyList<FermataMetadata> Fermatas { get; set; } = Array.Empty<FermataMetadata>();

    public IReadOnlyDictionary<string, int> XProperties { get; set; } = new Dictionary<string, int>();

    public string MasterBarXPropertiesXml { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> BarProperties { get; set; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> BarXProperties { get; set; } = new Dictionary<string, int>();

    public string BarXPropertiesXml { get; set; } = string.Empty;

    public IReadOnlyList<MeasureStaffModel> AdditionalStaffBars { get; set; } = Array.Empty<MeasureStaffModel>();

    public IReadOnlyList<MeasureVoiceModel> Voices { get; set; } = Array.Empty<MeasureVoiceModel>();

    public IReadOnlyList<BeatModel> Beats { get; set; } = Array.Empty<BeatModel>();
}

public sealed class MeasureStaffModel : ExtensibleModel
{
    public string BarXml { get; set; } = string.Empty;

    public int StaffIndex { get; set; }

    public int SourceBarId { get; set; }

    public string Clef { get; set; } = string.Empty;

    public string SimileMark { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> BarProperties { get; set; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> BarXProperties { get; set; } = new Dictionary<string, int>();

    public string BarXPropertiesXml { get; set; } = string.Empty;

    public IReadOnlyList<MeasureVoiceModel> Voices { get; set; } = Array.Empty<MeasureVoiceModel>();

    public IReadOnlyList<BeatModel> Beats { get; set; } = Array.Empty<BeatModel>();
}

public sealed class MeasureVoiceModel : ExtensibleModel
{
    public string Xml { get; set; } = string.Empty;

    public int VoiceIndex { get; set; }

    public int SourceVoiceId { get; set; }

    public IReadOnlyDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    public IReadOnlyList<string> DirectionTags { get; set; } = Array.Empty<string>();

    public IReadOnlyList<BeatModel> Beats { get; set; } = Array.Empty<BeatModel>();
}

public sealed class RhythmShapeModel
{
    public string Xml { get; set; } = string.Empty;

    public string NoteValue { get; set; } = string.Empty;

    public int AugmentationDots { get; set; }

    public bool AugmentationDotUsesCountAttribute { get; set; }

    public int[] AugmentationDotCounts { get; set; } = Array.Empty<int>();

    public TupletRatioModel? PrimaryTuplet { get; set; }

    public TupletRatioModel? SecondaryTuplet { get; set; }
}

public sealed class TupletRatioModel
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

public sealed class BeatModel : ExtensibleModel
{
    public string Xml { get; set; } = string.Empty;

    public int Id { get; set; }

    public int SourceRhythmId { get; set; } = -1;

    public RhythmShapeModel? SourceRhythm { get; set; }

    public string GraceType { get; set; } = string.Empty;

    public string Dynamic { get; set; } = string.Empty;

    public string TransposedPitchStemOrientation { get; set; } = string.Empty;

    public string UserTransposedPitchStemOrientation { get; set; } = string.Empty;

    public bool HasTransposedPitchStemOrientationUserDefinedElement { get; set; }

    public string ConcertPitchStemOrientation { get; set; } = string.Empty;

    public string Wah { get; set; } = string.Empty;

    public string Golpe { get; set; } = string.Empty;

    public string Fadding { get; set; } = string.Empty;

    public bool Slashed { get; set; }

    public string Hairpin { get; set; } = string.Empty;

    public string Variation { get; set; } = string.Empty;

    public string Ottavia { get; set; } = string.Empty;

    public bool? LegatoOrigin { get; set; }

    public bool? LegatoDestination { get; set; }

    public string LyricsXml { get; set; } = string.Empty;

    public string PickStrokeDirection { get; set; } = string.Empty;

    public string VibratoWithTremBarStrength { get; set; } = string.Empty;

    public bool Slapped { get; set; }

    public bool Popped { get; set; }

    public bool PalmMuted { get; set; }

    public bool Brush { get; set; }

    public bool BrushIsUp { get; set; }

    public bool Arpeggio { get; set; }

    public int? BrushDurationTicks { get; set; }

    public string BrushDurationXPropertyId { get; set; } = string.Empty;

    public bool HasExplicitBrushDurationXProperty { get; set; }

    public bool Rasgueado { get; set; }

    public string RasgueadoPattern { get; set; } = string.Empty;

    public bool DeadSlapped { get; set; }

    public bool Tremolo { get; set; }

    public string TremoloValue { get; set; } = string.Empty;

    public string ChordId { get; set; } = string.Empty;

    public string FreeText { get; set; } = string.Empty;

    public WhammyBarModel? WhammyBar { get; set; }

    public bool WhammyUsesElement { get; set; }

    public bool WhammyExtendUsesElement { get; set; }

    public IReadOnlyDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> XProperties { get; set; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> VoiceProperties { get; set; } = new Dictionary<string, string>();

    public IReadOnlyList<string> VoiceDirectionTags { get; set; } = Array.Empty<string>();

    public decimal Offset { get; set; }

    public decimal Duration { get; set; }

    public IReadOnlyList<NoteModel> Notes { get; set; } = Array.Empty<NoteModel>();

    public IReadOnlyList<int> MidiPitches { get; set; } = Array.Empty<int>();
}

public sealed class NoteModel : ExtensibleModel
{
    public string Xml { get; set; } = string.Empty;

    public int Id { get; set; }

    public int? Velocity { get; set; }

    public int? MidiPitch { get; set; }

    public int? SourceMidiPitch { get; set; }

    public int? SourceTransposedMidiPitch { get; set; }

    public PitchValueModel? ConcertPitch { get; set; }

    public PitchValueModel? TransposedPitch { get; set; }

    public int? SourceFret { get; set; }

    public int? SourceStringNumber { get; set; }

    public bool ShowStringNumber { get; set; }

    public int? StringNumber { get; set; }

    public IReadOnlyDictionary<string, int> XProperties { get; set; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; set; } = string.Empty;

    public decimal Duration { get; set; }

    public bool TieExtendedFromPrevious { get; set; }

    public NoteArticulationModel Articulation { get; set; } = new();
}

public sealed class PitchValueModel
{
    public string Step { get; set; } = string.Empty;

    public string Accidental { get; set; } = string.Empty;

    public int? Octave { get; set; }
}

public sealed class NoteArticulationModel
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

    public int? InstrumentArticulation { get; set; }

    public bool PalmMuted { get; set; }

    public bool Muted { get; set; }

    public bool Tapped { get; set; }

    public bool LeftHandTapped { get; set; }

    public bool HopoOrigin { get; set; }

    public bool HopoDestination { get; set; }

    public HopoTypeKind HopoType { get; set; } = HopoTypeKind.None;

    public int? HopoOriginNoteId { get; set; }

    public int? HopoDestinationNoteId { get; set; }

    public int? SlideFlags { get; set; }

    public IReadOnlyList<SlideType> Slides { get; set; } = Array.Empty<SlideType>();

    public HarmonicModel? Harmonic { get; set; }

    public BendModel? Bend { get; set; }
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
    public int? Type { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public HarmonicTypeKind Kind { get; set; } = HarmonicTypeKind.Unknown;

    public decimal? Fret { get; set; }

    public bool Enabled { get; set; }
}

public sealed class BendModel
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

public sealed class WhammyBarModel
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
