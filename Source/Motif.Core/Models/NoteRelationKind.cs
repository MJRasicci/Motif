namespace Motif.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Directed note-to-note relation families.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NoteRelationKind>))]
public enum NoteRelationKind
{
    Legato = 0,
    HammerOn = 1,
    PullOff = 2,
    Slide = 3,
    Tie = 4
}
