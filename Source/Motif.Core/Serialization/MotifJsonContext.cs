namespace Motif;

using Motif.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Score))]
[JsonSerializable(typeof(TimelineBar))]
[JsonSerializable(typeof(PitchValue))]
[JsonSerializable(typeof(TupletRatio))]
internal partial class MotifJsonContext : JsonSerializerContext
{
}
