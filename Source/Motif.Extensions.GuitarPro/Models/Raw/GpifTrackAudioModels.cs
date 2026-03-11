namespace Motif.Extensions.GuitarPro.Models.Raw;

internal sealed class GpifInstrumentSet
{
    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public int? LineCount { get; init; }

    public IReadOnlyList<GpifInstrumentElement> Elements { get; init; } = Array.Empty<GpifInstrumentElement>();
}

internal sealed class GpifInstrumentElement
{
    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string SoundbankName { get; init; } = string.Empty;

    public IReadOnlyList<GpifInstrumentArticulation> Articulations { get; init; } = Array.Empty<GpifInstrumentArticulation>();
}

internal sealed class GpifInstrumentArticulation
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

internal sealed class GpifSound
{
    public string Name { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public int? MidiLsb { get; init; }

    public int? MidiMsb { get; init; }

    public int? MidiProgram { get; init; }

    public GpifSoundRse Rse { get; init; } = new();
}

internal sealed class GpifSoundRse
{
    public string SoundbankPatch { get; init; } = string.Empty;

    public string SoundbankSet { get; init; } = string.Empty;

    public string ElementsSettingsXml { get; init; } = string.Empty;

    public GpifSoundRsePickups Pickups { get; init; } = new();

    public IReadOnlyList<GpifRseEffect> EffectChain { get; init; } = Array.Empty<GpifRseEffect>();
}

internal sealed class GpifSoundRsePickups
{
    public string OverloudPosition { get; init; } = string.Empty;

    public string Volumes { get; init; } = string.Empty;

    public string Tones { get; init; } = string.Empty;
}

internal sealed class GpifRse
{
    public string Bank { get; init; } = string.Empty;

    public string ChannelStripVersion { get; init; } = string.Empty;

    public string ChannelStripParameters { get; init; } = string.Empty;

    public IReadOnlyList<GpifAutomation> Automations { get; init; } = Array.Empty<GpifAutomation>();
}

internal sealed class GpifRseEffect
{
    public string Id { get; init; } = string.Empty;

    public bool Bypass { get; init; }

    public string Parameters { get; init; } = string.Empty;
}
