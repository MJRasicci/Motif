namespace Motif.Models;

/// <summary>
/// Note-scoped articulations, fingering, and guitar-specific techniques.
/// </summary>
public sealed class NoteArticulation
{
    /// <summary>
    /// Gets or sets the left-hand fingering annotation.
    /// </summary>
    public string LeftFingering { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the right-hand fingering annotation.
    /// </summary>
    public string RightFingering { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ornament name or identifier.
    /// </summary>
    public string Ornament { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the note should ring through subsequent beats.
    /// </summary>
    public bool LetRing { get; set; }

    /// <summary>
    /// Gets or sets the vibrato style or identifier.
    /// </summary>
    public string Vibrato { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the note starts a tie.
    /// </summary>
    public bool TieOrigin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the note ends a tie.
    /// </summary>
    public bool TieDestination { get; set; }

    /// <summary>
    /// Gets or sets the trill interval or reference value.
    /// </summary>
    public int? Trill { get; set; }

    /// <summary>
    /// Gets or sets the trill speed classification.
    /// </summary>
    public TrillSpeedKind TrillSpeed { get; set; } = TrillSpeedKind.None;

    /// <summary>
    /// Gets or sets the accent level or source value.
    /// </summary>
    public int? Accent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether anti-accent applies.
    /// </summary>
    public bool AntiAccent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the note is palm-muted.
    /// </summary>
    public bool PalmMuted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the note is muted.
    /// </summary>
    public bool Muted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the note uses tapping.
    /// </summary>
    public bool Tapped { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the note uses left-hand tapping.
    /// </summary>
    public bool LeftHandTapped { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the note starts a hammer-on or pull-off relationship.
    /// </summary>
    public bool HopoOrigin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the note ends a hammer-on or pull-off relationship.
    /// </summary>
    public bool HopoDestination { get; set; }

    /// <summary>
    /// Gets or sets the resolved hammer-on or pull-off classification.
    /// </summary>
    public HopoTypeKind HopoType { get; set; } = HopoTypeKind.None;

    /// <summary>
    /// Gets or sets the linked origin note identifier for hammer-on or pull-off relationships.
    /// </summary>
    public int? HopoOriginNoteId { get; set; }

    /// <summary>
    /// Gets or sets the linked destination note identifier for hammer-on or pull-off relationships.
    /// </summary>
    public int? HopoDestinationNoteId { get; set; }

    /// <summary>
    /// Gets or sets the applied slide techniques.
    /// </summary>
    public IReadOnlyList<SlideType> Slides { get; set; } = Array.Empty<SlideType>();

    /// <summary>
    /// Gets or sets the harmonic technique details.
    /// </summary>
    public Harmonic? Harmonic { get; set; }

    /// <summary>
    /// Gets or sets the bend curve details.
    /// </summary>
    public Bend? Bend { get; set; }
}
