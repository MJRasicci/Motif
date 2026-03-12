namespace Motif.Models;

/// <summary>
/// Root domain object representing a score and its global timeline state.
/// </summary>
public sealed class Score : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the display title of the score.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary artist or composer attribution.
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the album or collection name associated with the score.
    /// </summary>
    public string Album { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the track list contained in the score.
    /// </summary>
    public IReadOnlyList<Track> Tracks { get; set; } = Array.Empty<Track>();

    /// <summary>
    /// Score-owned master-bar timeline used for playback/navigation and other timeline-global state.
    /// </summary>
    public IReadOnlyList<TimelineBar> TimelineBars { get; set; } = Array.Empty<TimelineBar>();

    /// <summary>
    /// True when playback should treat the score as beginning with a pickup bar.
    /// </summary>
    public bool Anacrusis { get; set; }

    /// <summary>
    /// Ordered master-bar indices representing derived navigation-aware playback traversal.
    /// Call <see cref="Motif.ScoreNavigation.RebuildPlaybackSequence(Motif.Models.Score)"/> after
    /// traversal-affecting edits, or <see cref="Motif.ScoreNavigation.EnsurePlaybackSequence(Motif.Models.Score)"/>
    /// when reading the cached value.
    /// </summary>
    public IReadOnlyList<int> PlaybackMasterBarSequence { get; set; } = Array.Empty<int>();
}
