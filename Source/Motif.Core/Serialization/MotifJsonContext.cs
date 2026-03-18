namespace Motif;

using Motif.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Score))]
[JsonSerializable(typeof(TimelineBar))]
[JsonSerializable(typeof(PointControlEvent))]
[JsonSerializable(typeof(SpanControlEvent))]
[JsonSerializable(typeof(WrittenPosition))]
[JsonSerializable(typeof(ControlScopeKind))]
[JsonSerializable(typeof(PointControlKind))]
[JsonSerializable(typeof(SpanControlKind))]
[JsonSerializable(typeof(NoteRelation))]
[JsonSerializable(typeof(NoteRelationKind))]
[JsonSerializable(typeof(TrackInstrument))]
[JsonSerializable(typeof(TrackTransposition))]
[JsonSerializable(typeof(StaffTuning))]
[JsonSerializable(typeof(Pitch))]
[JsonSerializable(typeof(ScoreTime))]
[JsonSerializable(typeof(RhythmValue))]
[JsonSerializable(typeof(NoteValueKind))]
[JsonSerializable(typeof(TupletRatio))]
internal sealed partial class MotifJsonContext : JsonSerializerContext
{
}
