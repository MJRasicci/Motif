namespace GPIO.NET.Models.Raw;

/// <summary>
/// Raw GPIF data model intentionally mirrors source indirection patterns.
/// </summary>
public sealed class GpifDocument
{
    public string GpVersion { get; init; } = string.Empty;

    public GpifRevisionInfo GpRevision { get; init; } = new();

    public string EncodingDescription { get; init; } = string.Empty;

    public ScoreInfo Score { get; init; } = new();

    public GpifMasterTrack MasterTrack { get; init; } = new();

    public IReadOnlyList<GpifTrack> Tracks { get; init; } = Array.Empty<GpifTrack>();

    public IReadOnlyList<GpifMasterBar> MasterBars { get; init; } = Array.Empty<GpifMasterBar>();

    public IReadOnlyDictionary<int, GpifBar> BarsById { get; init; } = new Dictionary<int, GpifBar>();

    public IReadOnlyDictionary<int, GpifVoice> VoicesById { get; init; } = new Dictionary<int, GpifVoice>();

    public IReadOnlyDictionary<int, GpifBeat> BeatsById { get; init; } = new Dictionary<int, GpifBeat>();

    public IReadOnlyDictionary<int, GpifNote> NotesById { get; init; } = new Dictionary<int, GpifNote>();

    public IReadOnlyDictionary<int, GpifRhythm> RhythmsById { get; init; } = new Dictionary<int, GpifRhythm>();

    public string BackingTrackXml { get; init; } = string.Empty;

    public string AudioTracksXml { get; init; } = string.Empty;

    public string AssetsXml { get; init; } = string.Empty;

    public string ScoreViewsXml { get; init; } = string.Empty;
}

public sealed class GpifRevisionInfo
{
    public string Xml { get; init; } = string.Empty;

    public string Required { get; init; } = string.Empty;

    public string Recommended { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

public sealed class ScoreInfo
{
    public string Xml { get; init; } = string.Empty;

    public string[] ExplicitEmptyOptionalElements { get; init; } = Array.Empty<string>();

    public string Title { get; init; } = string.Empty;

    public string SubTitle { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public string Album { get; init; } = string.Empty;

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
}

public sealed class GpifTrack
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

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

    public GpifInstrumentSet InstrumentSet { get; init; } = new();

    public IReadOnlyList<GpifSound> Sounds { get; init; } = Array.Empty<GpifSound>();

    public GpifRse ChannelRse { get; init; } = new();

    public string PlaybackStateXml { get; init; } = string.Empty;

    public string AudioEngineStateXml { get; init; } = string.Empty;

    public GpifPlaybackState PlaybackState { get; init; } = new();

    public GpifAudioEngineState AudioEngineState { get; init; } = new();

    public IReadOnlyList<GpifAutomation> Automations { get; init; } = Array.Empty<GpifAutomation>();

    public string MidiConnectionXml { get; init; } = string.Empty;

    public string LyricsXml { get; init; } = string.Empty;

    public string AutomationsXml { get; init; } = string.Empty;

    public string TransposeXml { get; init; } = string.Empty;

    public GpifMidiConnection MidiConnection { get; init; } = new();

    public GpifLyrics Lyrics { get; init; } = new();

    public GpifTranspose Transpose { get; init; } = new();

    public IReadOnlyList<GpifStaff> Staffs { get; init; } = Array.Empty<GpifStaff>();
}

public sealed class GpifStaff
{
    public int? Id { get; init; }

    public string Cref { get; init; } = string.Empty;

    public int[] TuningPitches { get; init; } = Array.Empty<int>();

    public int? CapoFret { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public string Xml { get; init; } = string.Empty;
}

public sealed class GpifMasterBar
{
    public string Xml { get; init; } = string.Empty;

    public int Index { get; init; }

    public string Time { get; init; } = string.Empty;

    public bool DoubleBar { get; init; }

    public bool FreeTime { get; init; }

    public string TripletFeel { get; init; } = string.Empty;

    public string BarsReferenceList { get; init; } = string.Empty;

    public string AlternateEndings { get; init; } = string.Empty;

    public bool RepeatStart { get; init; }

    public bool RepeatStartAttributePresent { get; init; }

    public bool RepeatEnd { get; init; }

    public bool RepeatEndAttributePresent { get; init; }

    public int RepeatCount { get; init; }

    public bool RepeatCountAttributePresent { get; init; }

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

    public IReadOnlyList<GpifFermata> Fermatas { get; init; } = Array.Empty<GpifFermata>();

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; init; } = string.Empty;
}

public sealed class GpifFermata
{
    public string Type { get; init; } = string.Empty;

    public string Offset { get; init; } = string.Empty;

    public decimal? Length { get; init; }
}

public sealed class GpifBar
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public string VoicesReferenceList { get; init; } = string.Empty;

    public string Clef { get; init; } = string.Empty;

    public string SimileMark { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}

public sealed class GpifVoice
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public string BeatsReferenceList { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public string[] DirectionTags { get; init; } = Array.Empty<string>();
}

public sealed class GpifBeat
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public int RhythmRef { get; init; }

    public string NotesReferenceList { get; init; } = string.Empty;

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

    public bool WhammyBar { get; init; }

    public bool WhammyBarExtended { get; init; }

    public bool WhammyExtendUsesElement { get; init; }

    public decimal? WhammyBarOriginValue { get; init; }

    public decimal? WhammyBarMiddleValue { get; init; }

    public decimal? WhammyBarDestinationValue { get; init; }

    public decimal? WhammyBarOriginOffset { get; init; }

    public decimal? WhammyBarMiddleOffset1 { get; init; }

    public decimal? WhammyBarMiddleOffset2 { get; init; }

    public decimal? WhammyBarDestinationOffset { get; init; }

    public bool WhammyUsesElement { get; init; }

    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; init; } = string.Empty;
}

public sealed class GpifNote
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public int? Velocity { get; init; }

    public int? MidiPitch { get; init; }

    public int? TransposedMidiPitch { get; init; }

    public GpifPitchValue? ConcertPitch { get; init; }

    public GpifPitchValue? TransposedPitch { get; init; }

    public int? SourceFret { get; init; }

    public int? SourceStringNumber { get; init; }

    public bool ShowStringNumber { get; init; }

    public IReadOnlyList<GpifNoteProperty> Properties { get; init; } = Array.Empty<GpifNoteProperty>();

    public GpifNoteArticulation Articulation { get; init; } = new();

    public IReadOnlyDictionary<string, int> XProperties { get; init; } = new Dictionary<string, int>();

    public string XPropertiesXml { get; init; } = string.Empty;
}

public sealed class GpifPitchValue
{
    public string Step { get; init; } = string.Empty;

    public string Accidental { get; init; } = string.Empty;

    public int? Octave { get; init; }
}

public sealed class GpifNoteProperty
{
    public string Name { get; init; } = string.Empty;

    public bool Enabled { get; init; }

    public int? Flags { get; init; }

    public int? Number { get; init; }

    public int? Fret { get; init; }

    public int? StringNumber { get; init; }

    public string HType { get; init; } = string.Empty;

    public decimal? HFret { get; init; }

    public decimal? Float { get; init; }
}

public sealed class GpifNoteArticulation
{
    public string LeftFingering { get; init; } = string.Empty;

    public string RightFingering { get; init; } = string.Empty;

    public string Ornament { get; init; } = string.Empty;

    public bool LetRing { get; init; }

    public string Vibrato { get; init; } = string.Empty;

    public bool TieOrigin { get; init; }

    public bool TieDestination { get; init; }

    public int? Trill { get; init; }

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

    public int? SlideFlags { get; init; }

    public bool BendEnabled { get; init; }

    public decimal? BendOriginOffset { get; init; }

    public decimal? BendOriginValue { get; init; }

    public decimal? BendMiddleOffset1 { get; init; }

    public decimal? BendMiddleOffset2 { get; init; }

    public decimal? BendMiddleValue { get; init; }

    public decimal? BendDestinationOffset { get; init; }

    public decimal? BendDestinationValue { get; init; }

    public bool HarmonicEnabled { get; init; }

    public int? HarmonicType { get; init; }

    public string HarmonicTypeText { get; init; } = string.Empty;

    public decimal? HarmonicFret { get; init; }
}

public sealed class GpifRhythm
{
    public string Xml { get; init; } = string.Empty;

    public int Id { get; init; }

    public string NoteValue { get; init; } = string.Empty;

    public int AugmentationDots { get; init; }

    public bool AugmentationDotUsesCountAttribute { get; init; }

    public int[] AugmentationDotCounts { get; init; } = Array.Empty<int>();

    public TupletRatio? PrimaryTuplet { get; init; }

    public TupletRatio? SecondaryTuplet { get; init; }
}

public sealed class TupletRatio
{
    public int Numerator { get; init; }

    public int Denominator { get; init; }
}
