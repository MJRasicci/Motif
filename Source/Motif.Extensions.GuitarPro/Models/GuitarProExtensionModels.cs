namespace Motif.Extensions.GuitarPro.Models;

using Motif.Models;

public sealed class GpScoreExtension : IModelExtension
{
    public required ScoreMetadata Metadata { get; init; }

    public required MasterTrackMetadata MasterTrack { get; init; }
}

public sealed class GpTrackExtension : IModelExtension
{
    public required TrackMetadata Metadata { get; init; }
}
