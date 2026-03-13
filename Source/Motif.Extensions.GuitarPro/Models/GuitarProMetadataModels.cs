namespace Motif.Extensions.GuitarPro.Models;

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

    public string TuningInstrument { get; set; } = string.Empty;

    public string TuningLabel { get; set; } = string.Empty;

    public bool? TuningLabelVisible { get; set; }

    public bool EmitTuningFlatElement { get; set; }

    public bool EmitTuningFlatProperty { get; set; }

    public int? CapoFret { get; set; }

    public int? FretCount { get; set; }

    public int? PartialCapoFret { get; set; }

    public string PartialCapoStringFlags { get; set; } = string.Empty;

    public bool EmitChordCollection { get; set; }

    public bool EmitChordWorkingSet { get; set; }

    public bool EmitDiagramCollection { get; set; }

    public bool EmitDiagramWorkingSet { get; set; }

    public string Name { get; set; } = string.Empty;

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
