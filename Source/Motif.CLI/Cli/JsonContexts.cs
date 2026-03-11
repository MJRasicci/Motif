namespace Motif.CLI;

using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Write;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Score))]
[JsonSerializable(typeof(PitchValue))]
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
