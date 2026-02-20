namespace GPIO.NET.Tool.Cli;

public static class CliParser
{
    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Missing input path.");
        }

        var inputPath = args[0];
        string? outputPath = null;
        var format = OutputFormat.Json;
        var jsonIndented = true;
        var jsonIgnoreNull = false;
        var jsonIgnoreDefaults = false;
        var fromJson = false;
        string? diagnosticsOutPath = null;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                outputPath ??= arg;
                continue;
            }

            var (name, value) = SplitOption(arg);
            switch (name)
            {
                case "--format":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    format = value?.ToLowerInvariant() switch
                    {
                        "json" => OutputFormat.Json,
                        "gpif" => OutputFormat.Gpif,
                        "midi" => OutputFormat.Midi,
                        _ => throw new ArgumentException($"Unknown format '{value}'.")
                    };
                    break;

                case "--out":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    outputPath = value;
                    break;

                case "--json-indent":
                    jsonIndented = ParseBoolOption(value, true);
                    break;

                case "--json-ignore-null":
                    jsonIgnoreNull = ParseBoolOption(value, true);
                    break;

                case "--json-ignore-default":
                    jsonIgnoreDefaults = ParseBoolOption(value, true);
                    break;

                case "--from-json":
                    fromJson = ParseBoolOption(value, true);
                    break;

                case "--diagnostics-out":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    diagnosticsOutPath = value;
                    break;

                case "--help":
                case "-h":
                    throw new OperationCanceledException("help");

                default:
                    throw new ArgumentException($"Unknown option '{name}'.");
            }
        }

        return new CliOptions
        {
            InputPath = Path.GetFullPath(inputPath),
            OutputPath = string.IsNullOrWhiteSpace(outputPath) ? null : Path.GetFullPath(outputPath),
            Format = format,
            JsonIndented = jsonIndented,
            JsonIgnoreNull = jsonIgnoreNull,
            JsonIgnoreDefaults = jsonIgnoreDefaults,
            FromJson = fromJson,
            DiagnosticsOutPath = string.IsNullOrWhiteSpace(diagnosticsOutPath) ? null : Path.GetFullPath(diagnosticsOutPath)
        };
    }

    private static (string name, string? value) SplitOption(string arg)
    {
        var idx = arg.IndexOf('=');
        if (idx < 0)
        {
            return (arg, null);
        }

        return (arg[..idx], arg[(idx + 1)..]);
    }

    private static bool ParseBoolOption(string? value, bool defaultIfMissing)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultIfMissing;
        }

        return value.ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => throw new ArgumentException($"Invalid boolean option value '{value}'.")
        };
    }
}
