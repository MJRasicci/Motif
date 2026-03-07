namespace GPIO.NET;

using GPIO.NET.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GuitarProScore))]
[JsonSerializable(typeof(RhythmShapeModel))]
[JsonSerializable(typeof(TupletRatioModel))]
internal partial class GpioJsonContext : JsonSerializerContext
{
}
