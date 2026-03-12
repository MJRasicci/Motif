namespace Motif.Models;

/// <summary>
/// Bend curve metadata for a note articulation.
/// </summary>
public sealed class Bend
{
    /// <summary>
    /// Gets or sets a value indicating whether bend data is active.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the resolved bend type classification.
    /// </summary>
    public BendTypeKind Type { get; set; } = BendTypeKind.Unknown;

    /// <summary>
    /// Gets or sets the origin point offset along the bend curve.
    /// </summary>
    public decimal? OriginOffset { get; set; }

    /// <summary>
    /// Gets or sets the origin point pitch value.
    /// </summary>
    public decimal? OriginValue { get; set; }

    /// <summary>
    /// Gets or sets the first middle control-point offset.
    /// </summary>
    public decimal? MiddleOffset1 { get; set; }

    /// <summary>
    /// Gets or sets the second middle control-point offset.
    /// </summary>
    public decimal? MiddleOffset2 { get; set; }

    /// <summary>
    /// Gets or sets the middle control-point pitch value.
    /// </summary>
    public decimal? MiddleValue { get; set; }

    /// <summary>
    /// Gets or sets the destination point offset along the bend curve.
    /// </summary>
    public decimal? DestinationOffset { get; set; }

    /// <summary>
    /// Gets or sets the destination point pitch value.
    /// </summary>
    public decimal? DestinationValue { get; set; }
}
