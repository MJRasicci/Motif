namespace GPIO.NET.Tool.Cli;

using GPIO.NET.Models.Patching;

internal sealed class PatchDiagnosticsOutput
{
    public IReadOnlyList<string> UnsupportedChanges { get; init; } = Array.Empty<string>();

    public IReadOnlyList<PatchDiagnosticEntry> PatchDiagnostics { get; init; } = Array.Empty<PatchDiagnosticEntry>();
}
