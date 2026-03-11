namespace Motif;

using Motif.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Score))]
[JsonSerializable(typeof(TimelineBarModel))]
[JsonSerializable(typeof(PitchValueModel))]
[JsonSerializable(typeof(TupletRatioModel))]
internal partial class MotifJsonContext : JsonSerializerContext
{
}
