namespace GPIO.NET.Tool.Cli;

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

    public int FilesWithByteDrift { get; init; }

    public int FilesWithNonNoOpPatchPlan { get; init; }

    public int TotalDiagnostics { get; init; }

    public int TotalWarnings { get; init; }

    public int TotalInfos { get; init; }

    public BatchNamedCount[] DiagnosticCodes { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] DiagnosticSections { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] MissingElements { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] AddedElements { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] ValueDrifts { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] AttributeDrifts { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] PatchOperations { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] UnsupportedChanges { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchPathSummary[] TopNormalizedPaths { get; init; } = Array.Empty<BatchPathSummary>();

    public BatchFileHeadline[] MostChangedFiles { get; init; } = Array.Empty<BatchFileHeadline>();
}

internal sealed class BatchRoundTripFileResult
{
    public required string File { get; init; }

    public required string RelativePath { get; init; }

    public bool GpifBytesIdentical { get; init; }

    public bool PatchPlanIsNoOp { get; init; }

    public int UnsupportedChangeCount { get; init; }

    public string[] UnsupportedChangesSample { get; init; } = Array.Empty<string>();

    public BatchNamedCount[] PatchOperationCounts { get; init; } = Array.Empty<BatchNamedCount>();

    public int DiagnosticCount { get; init; }

    public int WarningCount { get; init; }

    public int InfoCount { get; init; }

    public BatchNamedCount[] DiagnosticCodeCounts { get; init; } = Array.Empty<BatchNamedCount>();

    public BatchNamedCount[] DiagnosticSectionCounts { get; init; } = Array.Empty<BatchNamedCount>();
}

internal sealed class BatchRoundTripDiagnosticLogEntry
{
    public required string File { get; init; }

    public required string RelativePath { get; init; }

    public required string Code { get; init; }

    public required string Category { get; init; }

    public required string Severity { get; init; }

    public required string Message { get; init; }

    public string? Path { get; init; }

    public string? SourceValue { get; init; }

    public string? OutputValue { get; init; }
}

internal sealed class BatchNamedCount
{
    public required string Name { get; init; }

    public int Count { get; init; }

    public int FileCount { get; init; }

    public string[] SampleFiles { get; init; } = Array.Empty<string>();
}

internal sealed class BatchPathSummary
{
    public required string Code { get; init; }

    public required string Path { get; init; }

    public int Count { get; init; }

    public int FileCount { get; init; }

    public string[] SampleFiles { get; init; } = Array.Empty<string>();
}

internal sealed class BatchFileHeadline
{
    public required string RelativePath { get; init; }

    public int DiagnosticCount { get; init; }

    public int UnsupportedChangeCount { get; init; }

    public bool PatchPlanIsNoOp { get; init; }
}
