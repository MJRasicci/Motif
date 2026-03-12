namespace Motif.Models;

/// <summary>
/// Whammy-bar automation curve applied to a beat.
/// </summary>
public sealed class WhammyBar
{
    /// <summary>
    /// Gets or sets a value indicating whether whammy-bar data is active.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the extended whammy form is used.
    /// </summary>
    public bool Extended { get; set; }

    /// <summary>
    /// Gets or sets the origin point pitch value.
    /// </summary>
    public decimal? OriginValue { get; set; }

    /// <summary>
    /// Gets or sets the middle control-point pitch value.
    /// </summary>
    public decimal? MiddleValue { get; set; }

    /// <summary>
    /// Gets or sets the destination point pitch value.
    /// </summary>
    public decimal? DestinationValue { get; set; }

    /// <summary>
    /// Gets or sets the origin point offset along the whammy curve.
    /// </summary>
    public decimal? OriginOffset { get; set; }

    /// <summary>
    /// Gets or sets the first middle control-point offset.
    /// </summary>
    public decimal? MiddleOffset1 { get; set; }

    /// <summary>
    /// Gets or sets the second middle control-point offset.
    /// </summary>
    public decimal? MiddleOffset2 { get; set; }

    /// <summary>
    /// Gets or sets the destination point offset along the whammy curve.
    /// </summary>
    public decimal? DestinationOffset { get; set; }
}
