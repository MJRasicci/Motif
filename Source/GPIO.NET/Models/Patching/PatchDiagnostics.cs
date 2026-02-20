namespace GPIO.NET.Models.Patching;

public sealed class PatchDiagnostics
{
    private readonly List<PatchDiagnosticEntry> entries = [];

    public IReadOnlyList<PatchDiagnosticEntry> Entries => entries;

    public void Add(string operation, string message)
    {
        entries.Add(new PatchDiagnosticEntry
        {
            Operation = operation,
            Message = message
        });
    }
}

public sealed class PatchDiagnosticEntry
{
    public required string Operation { get; init; }

    public required string Message { get; init; }
}

public sealed class PatchResult
{
    public required PatchDiagnostics Diagnostics { get; init; }
}
