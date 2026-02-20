namespace GPIO.NET.Models.Write;

public enum WriteDiagnosticSeverity
{
    Info,
    Warning
}

public sealed class WriteDiagnosticEntry
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public required string Category { get; init; }

    public WriteDiagnosticSeverity Severity { get; init; } = WriteDiagnosticSeverity.Warning;
}
