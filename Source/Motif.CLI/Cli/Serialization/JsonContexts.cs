namespace Motif.CLI;

using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Write;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Score))]
[JsonSerializable(typeof(Pitch))]
[JsonSerializable(typeof(ScoreTime))]
[JsonSerializable(typeof(RhythmValue))]
[JsonSerializable(typeof(NoteValueKind))]
[JsonSerializable(typeof(TupletRatio))]
[JsonSerializable(typeof(WriteDiagnosticEntry[]))]
[JsonSerializable(typeof(BatchFailure))]
[JsonSerializable(typeof(BatchFailure[]))]
[JsonSerializable(typeof(BatchRoundTripSummary))]
[JsonSerializable(typeof(BatchRoundTripFileResult))]
[JsonSerializable(typeof(BatchRoundTripDiagnosticLogEntry))]
[JsonSerializable(typeof(BatchNamedCount))]
[JsonSerializable(typeof(BatchNamedCount[]))]
[JsonSerializable(typeof(BatchPathSummary))]
[JsonSerializable(typeof(BatchPathSummary[]))]
[JsonSerializable(typeof(BatchFileHeadline))]
[JsonSerializable(typeof(BatchFileHeadline[]))]
internal sealed partial class CliJsonContext : JsonSerializerContext
{
}
