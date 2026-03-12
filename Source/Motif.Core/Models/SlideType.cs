namespace Motif.Models;

/// <summary>
/// Slide technique flags that may be combined on a note.
/// </summary>
[Flags]
public enum SlideType
{
    /// <summary>
    /// No slide technique is present.
    /// </summary>
    None = 0,

    /// <summary>
    /// Shift slide between fretted notes.
    /// </summary>
    Shift = 1,

    /// <summary>
    /// Legato slide between notes.
    /// </summary>
    Legato = 2,

    /// <summary>
    /// Slide out downward from the note.
    /// </summary>
    OutDown = 4,

    /// <summary>
    /// Slide out upward from the note.
    /// </summary>
    OutUp = 8,

    /// <summary>
    /// Slide into the note from below.
    /// </summary>
    IntoFromBelow = 16,

    /// <summary>
    /// Slide into the note from above.
    /// </summary>
    IntoFromAbove = 32,

    /// <summary>
    /// Preserves an unclassified source flag value of 64.
    /// </summary>
    Unknown64 = 64,

    /// <summary>
    /// Preserves an unclassified source flag value of 128.
    /// </summary>
    Unknown128 = 128
}
