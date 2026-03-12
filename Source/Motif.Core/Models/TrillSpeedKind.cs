namespace Motif.Models;

/// <summary>
/// Normalized trill-speed buckets.
/// </summary>
public enum TrillSpeedKind
{
    /// <summary>
    /// No trill speed is specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Sixteenth-note trill subdivision.
    /// </summary>
    Sixteenth = 1,

    /// <summary>
    /// Thirty-second-note trill subdivision.
    /// </summary>
    ThirtySecond = 2,

    /// <summary>
    /// Sixty-fourth-note trill subdivision.
    /// </summary>
    SixtyFourth = 3,

    /// <summary>
    /// One-hundred-twenty-eighth-note trill subdivision.
    /// </summary>
    OneHundredTwentyEighth = 4
}
