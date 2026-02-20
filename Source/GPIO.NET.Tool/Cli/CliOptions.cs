namespace GPIO.NET.Tool.Cli;

public sealed class CliOptions
{
    public string InputPath { get; init; } = string.Empty;

    public string? OutputPath { get; init; }

    public OutputFormat Format { get; init; } = OutputFormat.Json;

    public bool JsonIndented { get; init; } = true;

    public bool JsonIgnoreNull { get; init; }

    public bool JsonIgnoreDefaults { get; init; }
}
