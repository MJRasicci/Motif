namespace Motif.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Supported span control families.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SpanControlKind>))]
public enum SpanControlKind
{
    Hairpin = 0,
    Ottava = 1,
    Legato = 2
}
