namespace Motif.Extensions.GuitarPro.Models;

using Motif.Models;

internal sealed class GpArchiveReadResult
{
    public required Stream ScoreStream { get; init; }

    public IReadOnlyList<GpArchiveResourceEntry> ResourceEntries { get; init; } = [];
}

internal sealed class GpArchiveResourceEntry
{
    public GpArchiveResourceEntry(string entryPath, ReadOnlyMemory<byte> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);

        EntryPath = entryPath;
        Data = data.ToArray();
    }

    public string EntryPath { get; }

    public ReadOnlyMemory<byte> Data { get; }
}

internal sealed class GpArchiveResourcesExtension : IModelExtension
{
    public IReadOnlyList<GpArchiveResourceEntry> Entries { get; init; } = [];
}

internal sealed class GpMotifArchiveState
{
    public GpScoreExtension? Score { get; set; }

    public GpFidelityStateExtension? FidelityState { get; set; }

    public IReadOnlyList<GpTimelineBarArchiveState> TimelineBars { get; set; } = [];

    public IReadOnlyList<GpTrackArchiveState> Tracks { get; set; } = [];
}

internal sealed class GpTimelineBarArchiveState
{
    public int Index { get; set; }

    public GpTimelineBarMetadata? Metadata { get; set; }
}

internal sealed class GpTrackArchiveState
{
    public int TrackId { get; set; }

    public TrackMetadata? Metadata { get; set; }

    public IReadOnlyList<GpStaffArchiveState> Staves { get; set; } = [];
}

internal sealed class GpStaffArchiveState
{
    public int StaffIndex { get; set; }

    public StaffMetadata? Metadata { get; set; }

    public IReadOnlyList<GpMeasureArchiveState> Measures { get; set; } = [];
}

internal sealed class GpMeasureArchiveState
{
    public int Index { get; set; }

    public GpMeasureStaffMetadata? Metadata { get; set; }

    public IReadOnlyList<GpVoiceArchiveState> Voices { get; set; } = [];

    public IReadOnlyList<GpBeatArchiveState> Beats { get; set; } = [];
}

internal sealed class GpVoiceArchiveState
{
    public int VoiceIndex { get; set; }

    public GpVoiceMetadata? Metadata { get; set; }

    public IReadOnlyList<GpBeatArchiveState> Beats { get; set; } = [];
}

internal sealed class GpBeatArchiveState
{
    public int Id { get; set; }

    public GpBeatMetadata? Metadata { get; set; }

    public IReadOnlyList<GpNoteArchiveState> Notes { get; set; } = [];
}

internal sealed class GpNoteArchiveState
{
    public int Id { get; set; }

    public GpNoteMetadata? Metadata { get; set; }
}
