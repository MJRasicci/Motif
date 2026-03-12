namespace Motif.Models;

/// <summary>
/// Measure content for a single staff at a specific timeline position.
/// </summary>
public sealed class StaffMeasure : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the score-level timeline-bar index represented by this measure.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the zero-based staff position within the owning track.
    /// </summary>
    public int StaffIndex { get; set; }

    /// <summary>
    /// Gets or sets the clef applied to this staff measure.
    /// </summary>
    public string Clef { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the simile marking carried by the measure.
    /// </summary>
    public string SimileMark { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explicit voices contained in the measure.
    /// When populated, each voice owns its own beat list.
    /// </summary>
    public IReadOnlyList<Voice> Voices { get; set; } = Array.Empty<Voice>();

    /// <summary>
    /// Gets or sets the primary beat list for the measure.
    /// For multi-voice content this typically mirrors voice 0 for convenience and legacy workflows.
    /// </summary>
    public IReadOnlyList<Beat> Beats { get; set; } = Array.Empty<Beat>();
}
