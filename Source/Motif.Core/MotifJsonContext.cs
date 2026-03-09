namespace Motif;

using Motif.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GuitarProScore))]
[JsonSerializable(typeof(PitchValueModel))]
[JsonSerializable(typeof(RhythmShapeModel))]
[JsonSerializable(typeof(TupletRatioModel))]
internal partial class MotifJsonContext : JsonSerializerContext
{
}
