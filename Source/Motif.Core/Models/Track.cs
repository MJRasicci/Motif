namespace Motif.Models;

/// <summary>
/// A playable part within a <see cref="Score"/>, such as guitar, bass, or vocals.
/// </summary>
public sealed class Track : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the stable track identifier used by format extensions.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the track.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the staves belonging to the track.
    /// Structural edits should flow through staff and measure collections rather than a flattened track-measure view.
    /// </summary>
    public IReadOnlyList<Staff> Staves { get; set; } = Array.Empty<Staff>();
}
