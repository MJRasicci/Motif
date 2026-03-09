namespace Motif.Extensions.GuitarPro;

using Motif.Extensions.GuitarPro.Models;
using Motif.Models;

public static class GuitarProModelExtensions
{
    public static GpScoreExtension? GetGuitarPro(this GuitarProScore score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return score.GetExtension<GpScoreExtension>();
    }

    public static GpScoreExtension GetRequiredGuitarPro(this GuitarProScore score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return score.GetRequiredExtension<GpScoreExtension>();
    }

    public static GpTrackExtension? GetGuitarPro(this TrackModel track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return track.GetExtension<GpTrackExtension>();
    }

    public static GpTrackExtension GetRequiredGuitarPro(this TrackModel track)
    {
        ArgumentNullException.ThrowIfNull(track);

        return track.GetRequiredExtension<GpTrackExtension>();
    }
}
