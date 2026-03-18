namespace Motif.Models;

/// <summary>
/// Directed relation from one note to another note in the score.
/// </summary>
public sealed class NoteRelation
{
    /// <summary>
    /// Gets or sets the relation family.
    /// </summary>
    public NoteRelationKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the target note identifier.
    /// </summary>
    public int TargetNoteId { get; set; }
}
