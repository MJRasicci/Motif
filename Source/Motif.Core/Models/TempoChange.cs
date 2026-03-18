namespace Motif.Models;

/// <summary>
/// Score-level tempo change event.
/// </summary>
public sealed class TempoChange
{
    /// <summary>
    /// Gets or sets the zero-based master-bar index where the change occurs.
    /// </summary>
    public int BarIndex { get; set; }

    /// <summary>
    /// Gets or sets the source-relative position within the bar when one is available.
    /// </summary>
    public ScoreTime Offset { get; set; } = ScoreTime.Zero;

    /// <summary>
    /// Gets or sets the target tempo in beats per minute.
    /// </summary>
    public decimal BeatsPerMinute { get; set; }
}
