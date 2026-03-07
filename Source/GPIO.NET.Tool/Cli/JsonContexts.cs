namespace GPIO.NET.Tool.Cli;

using GPIO.NET.Models;
using GPIO.NET.Models.Patching;
using GPIO.NET.Models.Write;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GuitarProScore))]
[JsonSerializable(typeof(RhythmShapeModel))]
[JsonSerializable(typeof(TupletRatioModel))]
[JsonSerializable(typeof(JsonPatchPlanResult))]
[JsonSerializable(typeof(GpPatchDocument))]
[JsonSerializable(typeof(PatchDiagnosticEntry[]))]
[JsonSerializable(typeof(WriteDiagnosticEntry[]))]
[JsonSerializable(typeof(PatchDiagnosticsOutput))]
[JsonSerializable(typeof(BatchFailure))]
[JsonSerializable(typeof(BatchFailure[]))]
internal partial class CliJsonContext : JsonSerializerContext
{
}
