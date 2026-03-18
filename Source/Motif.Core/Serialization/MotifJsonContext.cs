namespace Motif;

using Motif.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Score))]
[JsonSerializable(typeof(TimelineBar))]
[JsonSerializable(typeof(TempoChange))]
[JsonSerializable(typeof(TrackInstrument))]
[JsonSerializable(typeof(TrackTransposition))]
[JsonSerializable(typeof(StaffTuning))]
[JsonSerializable(typeof(PitchValue))]
[JsonSerializable(typeof(TupletRatio))]
internal sealed partial class MotifJsonContext : JsonSerializerContext
{
}
