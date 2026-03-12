namespace Motif.Models;

/// <summary>
/// Normalized hammer-on and pull-off relationship types.
/// </summary>
public enum HopoTypeKind
{
    /// <summary>
    /// No hammer-on or pull-off applies.
    /// </summary>
    None = 0,

    /// <summary>
    /// Upward legato transition into the note.
    /// </summary>
    HammerOn = 1,

    /// <summary>
    /// Downward legato transition into the note.
    /// </summary>
    PullOff = 2,

    /// <summary>
    /// Legato relation is present but not classified as hammer-on or pull-off.
    /// </summary>
    Legato = 3
}
