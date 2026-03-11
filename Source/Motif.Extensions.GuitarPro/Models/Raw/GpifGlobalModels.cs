namespace Motif.Extensions.GuitarPro.Models.Raw;

internal sealed class GpifMasterTrack
{
    public string Xml { get; init; } = string.Empty;

    public int[] TrackIds { get; init; } = Array.Empty<int>();

    public string AutomationsXml { get; init; } = string.Empty;

    public IReadOnlyList<GpifAutomation> Automations { get; init; } = Array.Empty<GpifAutomation>();

    public bool Anacrusis { get; init; }

    public string RseXml { get; init; } = string.Empty;

    public GpifMasterRse Rse { get; init; } = new();
}

internal sealed class GpifMasterRse
{
    public IReadOnlyList<GpifRseEffect> MasterEffects { get; init; } = Array.Empty<GpifRseEffect>();
}
