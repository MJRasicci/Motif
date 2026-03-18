namespace Motif.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Scope targeted by a control event.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ControlScopeKind>))]
public enum ControlScopeKind
{
    Score = 0,
    Track = 1,
    Staff = 2,
    Voice = 3
}
