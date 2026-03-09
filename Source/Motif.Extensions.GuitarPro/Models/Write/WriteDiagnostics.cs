namespace Motif.Extensions.GuitarPro.Models.Write;

public sealed class WriteDiagnostics
{
    private readonly List<WriteDiagnosticEntry> entries = [];

    public IReadOnlyList<WriteDiagnosticEntry> Entries => entries;

    public IReadOnlyList<WriteDiagnosticEntry> Infos
        => entries.Where(e => e.Severity == WriteDiagnosticSeverity.Info).ToArray();

    public IReadOnlyList<WriteDiagnosticEntry> Warnings
        => entries.Where(e => e.Severity == WriteDiagnosticSeverity.Warning).ToArray();

    public void Info(
        string code,
        string category,
        string message,
        string? path = null,
        string? sourceValue = null,
        string? outputValue = null)
        => Add(WriteDiagnosticSeverity.Info, code, category, message, path, sourceValue, outputValue);

    public void Warn(
        string code,
        string category,
        string message,
        string? path = null,
        string? sourceValue = null,
        string? outputValue = null)
        => Add(WriteDiagnosticSeverity.Warning, code, category, message, path, sourceValue, outputValue);

    private void Add(
        WriteDiagnosticSeverity severity,
        string code,
        string category,
        string message,
        string? path,
        string? sourceValue,
        string? outputValue)
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
            Severity = severity,
            Path = path,
            SourceValue = sourceValue,
            OutputValue = outputValue
        });
    }
}
