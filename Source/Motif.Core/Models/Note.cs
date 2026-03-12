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
    /// Gets or sets the resolved MIDI pitch, when available.
    /// </summary>
    public int? MidiPitch { get; set; }

    /// <summary>
    /// Gets or sets the concert-pitch spelling of the note.
    /// </summary>
    public PitchValue? ConcertPitch { get; set; }

    /// <summary>
    /// Gets or sets the transposed-pitch spelling of the note.
    /// </summary>
    public PitchValue? TransposedPitch { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the written string number should be shown.
    /// </summary>
    public bool ShowStringNumber { get; set; }

    /// <summary>
    /// Gets or sets the string number or string slot used for fretted instruments.
    /// </summary>
    public int? StringNumber { get; set; }

    /// <summary>
    /// Gets or sets the effective sounding duration of the note.
    /// Tied destinations may extend an earlier note without starting a new sound.
    /// </summary>
    public decimal Duration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this note only extends a tie from a previous note.
    /// </summary>
    public bool TieExtendedFromPrevious { get; set; }

    /// <summary>
    /// Gets or sets note-level articulations and techniques.
    /// </summary>
    public NoteArticulation Articulation { get; set; } = new();
}
