namespace Motif.Models;

/// <summary>
/// Rhythmic event container holding notes and beat-scoped performance markings.
/// </summary>
public sealed class Beat : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the beat identifier used by format extensions.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the grace-note classification for the beat.
    /// </summary>
    public string GraceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dynamic marking applied at the beat.
    /// </summary>
    public string Dynamic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the golpe marking carried by the beat.
    /// </summary>
    public string Golpe { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the beat is slashed.
    /// </summary>
    public bool Slashed { get; set; }

    /// <summary>
    /// Gets or sets the hairpin marking that begins or applies at the beat.
    /// </summary>
    public string Hairpin { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ottava indication applied at the beat.
    /// </summary>
    public string Ottavia { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the beat starts a legato span.
    /// </summary>
    public bool? LegatoOrigin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beat ends a legato span.
    /// </summary>
    public bool? LegatoDestination { get; set; }

    /// <summary>
    /// Gets or sets the pick-stroke direction.
    /// </summary>
    public string PickStrokeDirection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the beat uses slapped technique.
    /// </summary>
    public bool Slapped { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beat uses popped technique.
    /// </summary>
    public bool Popped { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beat is palm-muted at the aggregate beat level.
    /// </summary>
    public bool PalmMuted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beat uses a brush stroke.
    /// </summary>
    public bool Brush { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a brush stroke travels upward.
    /// </summary>
    public bool BrushIsUp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beat should be arpeggiated.
    /// </summary>
    public bool Arpeggio { get; set; }

    /// <summary>
    /// Gets or sets the brush duration in ticks when the source format provides one.
    /// </summary>
    public int? BrushDurationTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the beat uses rasgueado.
    /// </summary>
    public bool Rasgueado { get; set; }

    /// <summary>
    /// Gets or sets the rasgueado pattern identifier.
    /// </summary>
    public string RasgueadoPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the beat uses dead slap technique.
    /// </summary>
    public bool DeadSlapped { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tremolo picking applies to the beat.
    /// </summary>
    public bool Tremolo { get; set; }

    /// <summary>
    /// Gets or sets the tremolo subdivision or variant identifier.
    /// </summary>
    public string TremoloValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets free-form text attached to the beat.
    /// </summary>
    public string FreeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the whammy-bar automation for the beat.
    /// </summary>
    public WhammyBar? WhammyBar { get; set; }

    /// <summary>
    /// Gets or sets the beat offset within its containing measure.
    /// </summary>
    public decimal Offset { get; set; }

    /// <summary>
    /// Gets or sets the notated beat duration expressed as a score fraction.
    /// </summary>
    public decimal Duration { get; set; }

    /// <summary>
    /// Gets or sets the notes sounded by the beat.
    /// </summary>
    public IReadOnlyList<Note> Notes { get; set; } = Array.Empty<Note>();

    /// <summary>
    /// Gets or sets the aggregate MIDI pitches for the beat.
    /// </summary>
    public IReadOnlyList<int> MidiPitches { get; set; } = Array.Empty<int>();
}
