namespace Motif.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Base written note values used to describe rhythmic notation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NoteValueKind>))]
public enum NoteValueKind
{
    Unknown = 0,
    Whole = 1,
    Half = 2,
    Quarter = 3,
    Eighth = 4,
    Sixteenth = 5,
    ThirtySecond = 6,
    SixtyFourth = 7,
    OneHundredTwentyEighth = 8,
    TwoHundredFiftySixth = 9
}
