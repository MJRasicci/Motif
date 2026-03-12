namespace Motif.Models;

/// <summary>
/// Normalized bend-shape categories.
/// </summary>
public enum BendTypeKind
{
    /// <summary>
    /// Bend type could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Bend data is present but encodes no bend.
    /// </summary>
    None = 1,

    /// <summary>
    /// Sustained bend without additional movement.
    /// </summary>
    Hold = 2,

    /// <summary>
    /// Note begins prebent before attack.
    /// </summary>
    Prebend = 3,

    /// <summary>
    /// Rising bend after attack.
    /// </summary>
    Bend = 4,

    /// <summary>
    /// Release from a bent pitch.
    /// </summary>
    Release = 5,

    /// <summary>
    /// Rising bend followed by release.
    /// </summary>
    BendAndRelease = 6,

    /// <summary>
    /// Prebend followed by an additional rise.
    /// </summary>
    PrebendAndBend = 7,

    /// <summary>
    /// Prebend followed by release.
    /// </summary>
    PrebendAndRelease = 8
}
