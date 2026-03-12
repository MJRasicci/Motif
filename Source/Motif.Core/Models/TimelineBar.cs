namespace Motif.Models;

/// <summary>
/// Score-level timeline state for a master bar, including navigation and repeat semantics.
/// </summary>
public sealed class TimelineBar : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the zero-based master-bar index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the written time signature, such as <c>4/4</c>.
    /// </summary>
    public string TimeSignature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the barline should be rendered as a double barline.
    /// </summary>
    public bool DoubleBar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bar is in free time.
    /// </summary>
    public bool FreeTime { get; set; }

    /// <summary>
    /// Gets or sets the triplet feel interpretation for the bar.
    /// </summary>
    public string TripletFeel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the bar begins a repeat section.
    /// </summary>
    public bool RepeatStart { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source format explicitly emitted the repeat-start attribute.
    /// </summary>
    public bool RepeatStartAttributePresent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bar ends a repeat section.
    /// </summary>
    public bool RepeatEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source format explicitly emitted the repeat-end attribute.
    /// </summary>
    public bool RepeatEndAttributePresent { get; set; }

    /// <summary>
    /// Gets or sets the repeat count applied when <see cref="RepeatEnd"/> is set.
    /// </summary>
    public int RepeatCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source format explicitly emitted the repeat-count attribute.
    /// </summary>
    public bool RepeatCountAttributePresent { get; set; }

    /// <summary>
    /// Gets or sets the alternate-ending mask or label for the bar.
    /// </summary>
    public string AlternateEndings { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section letter shown at the bar.
    /// </summary>
    public string SectionLetter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section text shown at the bar.
    /// </summary>
    public string SectionText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether an explicit empty section element should be preserved.
    /// </summary>
    public bool HasExplicitEmptySection { get; set; }

    /// <summary>
    /// Gets or sets the navigation jump declared at this bar, such as D.S. or D.C.
    /// </summary>
    public string Jump { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation target declared at this bar, such as Segno or Coda.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the written key accidental count.
    /// </summary>
    public int? KeyAccidentalCount { get; set; }

    /// <summary>
    /// Gets or sets the key mode, such as major or minor.
    /// </summary>
    public string KeyMode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transposed key signature spelling hint.
    /// </summary>
    public string KeyTransposeAs { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets format-specific direction properties that supplement <see cref="Jump"/> and <see cref="Target"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string> DirectionProperties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets fermatas anchored to the bar.
    /// </summary>
    public IReadOnlyList<FermataMetadata> Fermatas { get; set; } = Array.Empty<FermataMetadata>();
}
