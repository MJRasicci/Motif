namespace Motif.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Supported point control families.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PointControlKind>))]
public enum PointControlKind
{
    Tempo = 0,
    Dynamic = 1,
    Fermata = 2
}
