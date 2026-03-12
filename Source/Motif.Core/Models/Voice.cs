namespace Motif.Models;

/// <summary>
/// An independent rhythmic layer within a <see cref="StaffMeasure"/>.
/// </summary>
public sealed class Voice : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the zero-based voice slot within the measure.
    /// </summary>
    public int VoiceIndex { get; set; }

    /// <summary>
    /// Gets or sets the ordered beats belonging to the voice.
    /// </summary>
    public IReadOnlyList<Beat> Beats { get; set; } = Array.Empty<Beat>();
}
