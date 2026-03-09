namespace Motif.Extensions.GuitarPro.Models;

/// <summary>
/// Options controlling file read and mapping behavior.
/// </summary>
public sealed record GpReadOptions
{
    /// <summary>
    /// When true, validates the score payload against schema if a validator is configured.
    /// </summary>
    public bool ValidateSchema { get; init; }

    /// <summary>
    /// When true, mapper should preserve unresolved references as diagnostics instead of throwing immediately.
    /// </summary>
    public bool LenientReferenceResolution { get; init; }
}
