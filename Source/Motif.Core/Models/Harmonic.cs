namespace Motif.Models;

/// <summary>
/// Harmonic technique metadata for a note articulation.
/// </summary>
public sealed class Harmonic
{
    /// <summary>
    /// Gets or sets the raw harmonic type value from the source format.
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// Gets or sets the source harmonic type name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized harmonic kind.
    /// </summary>
    public HarmonicTypeKind Kind { get; set; } = HarmonicTypeKind.Unknown;

    /// <summary>
    /// Gets or sets the harmonic fret position, when applicable.
    /// </summary>
    public decimal? Fret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether harmonic data is active.
    /// </summary>
    public bool Enabled { get; set; }
}
