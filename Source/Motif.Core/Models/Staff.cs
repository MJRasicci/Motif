namespace Motif.Models;

/// <summary>
/// A single staff within a <see cref="Track"/>.
/// </summary>
public sealed class Staff : ExtensibleModel
{
    /// <summary>
    /// Gets or sets the zero-based staff position within the owning track.
    /// </summary>
    public int StaffIndex { get; set; }

    /// <summary>
    /// Gets or sets the ordered measures assigned to the staff.
    /// </summary>
    public IReadOnlyList<StaffMeasure> Measures { get; set; } = Array.Empty<StaffMeasure>();
}
