namespace Motif.CLI;

internal sealed class BatchRoundTripSummary
{
    public required string InputRoot { get; init; }

    public required string OutputRoot { get; init; }

    public required string SummaryPath { get; init; }

    public required string FileResultsPath { get; init; }

    public required string DiagnosticsPath { get; init; }

    public required string FailureLogPath { get; init; }

    public required string GeneratedAtUtc { get; init; }

    public int TotalFiles { get; init; }

    public int SucceededFiles { get; init; }

    public int FailedFiles { get; init; }

    public int CleanFiles { get; init; }

    public int FilesWithDiagnostics { get; init; }

    public int FilesWithWarnings { get; init; }

    public int FilesWithInfos { get; init; }

    public int FilesWithByteDrift { get; init; }

    public int TotalDiagnostics { get; init; }

    public int TotalWarnings { get; init; }

    public int TotalInfos { get; init; }

    public BatchNamedCount[] DiagnosticCodes { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] DiagnosticSections { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] MissingElements { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] AddedElements { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] ValueDrifts { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] AttributeDrifts { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchPathSummary[] TopNormalizedPaths { get; init; } = Array.Empty<BatchPathSummary>();

    public BatchFileHeadline[] MostChangedFiles { get; init; } = Array.Empty<BatchFileHeadline>();
}
