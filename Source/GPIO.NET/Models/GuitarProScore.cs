namespace GPIO.NET.Models;

public sealed class GuitarProScore
{
    public string Title { get; init; } = string.Empty;

    public string Artist { get; init; } = string.Empty;

    public string Album { get; init; } = string.Empty;

    public ScoreMetadata Metadata { get; init; } = new();

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

public sealed class MeasureModel
{
    public int Index { get; init; }

    public string TimeSignature { get; init; } = string.Empty;

    public int SourceBarId { get; init; }

    public bool RepeatStart { get; init; }

    public bool RepeatEnd { get; init; }

    public int RepeatCount { get; init; }

    public string AlternateEndings { get; init; } = string.Empty;

    public string SectionLetter { get; init; } = string.Empty;

    public string SectionText { get; init; } = string.Empty;

    public string Jump { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;

    public IReadOnlyList<BeatModel> Beats { get; init; } = Array.Empty<BeatModel>();
}

public sealed class BeatModel
{
    public int Id { get; init; }

    public decimal Offset { get; init; }

    public decimal Duration { get; init; }

    public IReadOnlyList<NoteModel> Notes { get; init; } = Array.Empty<NoteModel>();

    public IReadOnlyList<int> MidiPitches { get; init; } = Array.Empty<int>();
}

public sealed class NoteModel
{
    public int Id { get; init; }

    public int? MidiPitch { get; init; }

    public decimal Duration { get; set; }

    public bool TieExtendedFromPrevious { get; set; }

    public NoteArticulationModel Articulation { get; init; } = new();
}

public sealed class NoteArticulationModel
{
    public bool LetRing { get; init; }

    public string Vibrato { get; init; } = string.Empty;

    public bool TieOrigin { get; init; }

    public bool TieDestination { get; init; }

    public int? Trill { get; init; }

    public int? Accent { get; init; }

    public bool AntiAccent { get; init; }

    public int? InstrumentArticulation { get; init; }

    public bool PalmMuted { get; init; }

    public bool Muted { get; init; }

    public bool Tapped { get; init; }

    public bool LeftHandTapped { get; init; }

    public bool HopoOrigin { get; init; }

    public bool HopoDestination { get; init; }

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

public sealed class HarmonicModel
{
    public int? Type { get; init; }

    public decimal? Fret { get; init; }

    public bool Enabled { get; init; }
}

public sealed class BendModel
{
    public bool Enabled { get; init; }

    public decimal? OriginOffset { get; init; }

    public decimal? OriginValue { get; init; }

    public decimal? MiddleOffset1 { get; init; }

    public decimal? MiddleOffset2 { get; init; }

    public decimal? MiddleValue { get; init; }

    public decimal? DestinationOffset { get; init; }

    public decimal? DestinationValue { get; init; }
}
