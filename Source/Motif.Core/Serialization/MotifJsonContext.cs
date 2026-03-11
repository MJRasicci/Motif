namespace Motif;

using Motif.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Score))]
[JsonSerializable(typeof(TimelineBar))]
[JsonSerializable(typeof(PitchValue))]
[JsonSerializable(typeof(TupletRatio))]
internal sealed partial class MotifJsonContext : JsonSerializerContext
{
}
