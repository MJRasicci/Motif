namespace Motif.Models;

/// <summary>
/// Ratio describing how a tuplet subdivides time.
/// </summary>
public sealed class TupletRatio
{
    /// <summary>
    /// Gets or sets the performed note count.
    /// </summary>
    public int Numerator { get; set; }

    /// <summary>
    /// Gets or sets the normal note count occupied by the tuplet.
    /// </summary>
    public int Denominator { get; set; }
}
