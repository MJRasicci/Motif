namespace GPIO.NET.Models.Write;

public sealed class WriteDiagnostics
{
    private readonly List<WriteDiagnosticEntry> entries = [];

    public IReadOnlyList<WriteDiagnosticEntry> Entries => entries;

    public IReadOnlyList<WriteDiagnosticEntry> Warnings
        => entries.Where(e => e.Severity == WriteDiagnosticSeverity.Warning).ToArray();

    public void Warn(string code, string category, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        entries.Add(new WriteDiagnosticEntry
        {
            Code = code,
            Category = category,
            Message = message,
            Severity = WriteDiagnosticSeverity.Warning
        });
    }
}
