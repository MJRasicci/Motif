namespace Motif.Models;

/// <summary>
/// Exact written position within the score timeline.
/// </summary>
public sealed class WrittenPosition
{
    /// <summary>
    /// Gets or sets the zero-based master-bar index.
    /// </summary>
    public int BarIndex { get; set; }

    /// <summary>
    /// Gets or sets the offset within the bar.
    /// </summary>
    public ScoreTime Offset { get; set; } = ScoreTime.Zero;
}
