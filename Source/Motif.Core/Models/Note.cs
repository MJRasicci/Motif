namespace Motif.Models;

/// <summary>
/// A single pitched or unpitched note event within a <see cref="Beat"/>.
/// </summary>
public sealed class Note : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the note identifier used by format extensions.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the playback velocity, when available.
    /// </summary>
    public int? Velocity { get; set; }

    /// <summary>
    /// Gets or sets the sounding pitch of the note when the note is pitched.
    /// </summary>
    public Pitch? Pitch { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the written string number should be shown.
    /// </summary>
    public bool ShowStringNumber { get; set; }

    /// <summary>
    /// Gets or sets the string number or string slot used for fretted instruments.
    /// </summary>
    public int? StringNumber { get; set; }

    /// <summary>
    /// Gets or sets the written duration of the note as notated on the page.
    /// </summary>
    public ScoreTime Duration { get; set; } = ScoreTime.Zero;

    /// <summary>
    /// Gets or sets the effective sounding duration of the note after ties or sustain semantics are applied.
    /// </summary>
    public ScoreTime SoundingDuration { get; set; } = ScoreTime.Zero;

    /// <summary>
    /// Gets or sets note-level articulations and techniques.
    /// </summary>
    public NoteArticulation Articulation { get; set; } = new();
}
