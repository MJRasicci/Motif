namespace Motif.Models;

/// <summary>
/// Fermata attachment metadata stored on a <see cref="TimelineBar"/>.
/// </summary>
public sealed class FermataMetadata
{
    /// <summary>
    /// Gets or sets the fermata type identifier.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the placement or anchor offset identifier.
    /// </summary>
    public string Offset { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fermata duration hint, when available.
    /// </summary>
    public decimal? Length { get; set; }
}
