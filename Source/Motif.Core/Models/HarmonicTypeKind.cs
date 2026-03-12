namespace Motif.Models;

/// <summary>
/// Normalized harmonic technique categories.
/// </summary>
public enum HarmonicTypeKind
{
    /// <summary>
    /// Harmonic kind could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Explicitly indicates the absence of a harmonic.
    /// </summary>
    NoHarmonic = 1,

    /// <summary>
    /// Natural harmonic.
    /// </summary>
    Natural = 2,

    /// <summary>
    /// Artificial harmonic.
    /// </summary>
    Artificial = 3,

    /// <summary>
    /// Pinch harmonic.
    /// </summary>
    Pinch = 4,

    /// <summary>
    /// Tapped harmonic.
    /// </summary>
    Tap = 5,

    /// <summary>
    /// Semi-harmonic.
    /// </summary>
    Semi = 6,

    /// <summary>
    /// Feedback harmonic.
    /// </summary>
    Feedback = 7
}
