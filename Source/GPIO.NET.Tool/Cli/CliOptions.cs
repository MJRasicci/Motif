namespace GPIO.NET.Tool.Cli;

public sealed class CliOptions
{
    public string InputPath { get; init; } = string.Empty;

    public string? OutputPath { get; init; }

    public OutputFormat Format { get; init; } = OutputFormat.Json;

    public bool JsonIndented { get; init; } = true;

    public bool JsonIgnoreNull { get; init; }

    public bool JsonIgnoreDefaults { get; init; }

    public bool FromJson { get; init; }

    public bool PatchFromJson { get; init; }

    public string? SourceGpPath { get; init; }

    public string? DiagnosticsOutPath { get; init; }

    public bool DiagnosticsAsJson { get; init; }

    public bool PlanOnly { get; init; }

    public bool Strict { get; init; }

    public string? BatchInputDir { get; init; }

    public string? BatchOutputDir { get; init; }

    public bool ContinueOnError { get; init; } = true;

    public string? FailureLogPath { get; init; }
}
