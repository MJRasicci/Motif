namespace GPIO.NET.Models.Raw;

public sealed class GpifMasterTrack
{
    public int[] TrackIds { get; init; } = Array.Empty<int>();

    public IReadOnlyList<GpifAutomation> Automations { get; init; } = Array.Empty<GpifAutomation>();

    public bool Anacrusis { get; init; }

    public string RseXml { get; init; } = string.Empty;

    public GpifMasterRse Rse { get; init; } = new();
}

public sealed class GpifMasterRse
{
    public IReadOnlyList<GpifRseEffect> MasterEffects { get; init; } = Array.Empty<GpifRseEffect>();
}
