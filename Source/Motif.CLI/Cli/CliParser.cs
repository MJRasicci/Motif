namespace Motif.CLI;

internal static class CliParser
{
    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            throw new OperationCanceledException("help");
        }

        // If the first arg looks like a flag, it is not the input path (e.g. batch mode with no input file).
        string? inputPath = args[0].StartsWith("--", StringComparison.Ordinal) ? null : args[0];
        var startIndex = inputPath is null ? 0 : 1;

        string? outputPath = null;
        var format = OutputFormat.Json;
        var jsonIndented = true;
        var jsonIgnoreNull = false;
        var jsonIgnoreDefaults = false;
        var fromJson = false;
        string? sourceGpPath = null;
        string? diagnosticsOutPath = null;
        var diagnosticsAsJson = false;
        string? batchInputDir = null;
        string? batchOutputDir = null;
        var batchRoundTripDiagnostics = false;
        var continueOnError = true;
        string? failureLogPath = null;

        for (var i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                if (inputPath is null)
                    inputPath = arg;
                else
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
                        "musicxml" => OutputFormat.MusicXml,
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

                case "--source-gp":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    sourceGpPath = value;
                    break;

                case "--diagnostics-out":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    diagnosticsOutPath = value;
                    break;

                case "--diagnostics-json":
                    diagnosticsAsJson = ParseBoolOption(value, true);
                    break;

                case "--batch-input-dir":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    batchInputDir = value;
                    break;

                case "--batch-output-dir":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    batchOutputDir = value;
                    break;

                case "--batch-roundtrip-diagnostics":
                    batchRoundTripDiagnostics = ParseBoolOption(value, true);
                    break;

                case "--continue-on-error":
                    continueOnError = ParseBoolOption(value, true);
                    break;

                case "--failure-log":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    failureLogPath = value;
                    break;

                case "--help":
                case "-h":
                    throw new OperationCanceledException("help");

                default:
                    throw new ArgumentException($"Unknown option '{name}'.");
            }
        }

        if (inputPath is null && batchInputDir is null)
        {
            throw new ArgumentException("Missing input path. Pass a .gp or mapped JSON file, or use --batch-input-dir for batch mode.");
        }

        return new CliOptions
        {
            InputPath = inputPath is null ? string.Empty : Path.GetFullPath(inputPath),
            OutputPath = string.IsNullOrWhiteSpace(outputPath) ? null : Path.GetFullPath(outputPath),
            Format = format,
            JsonIndented = jsonIndented,
            JsonIgnoreNull = jsonIgnoreNull,
            JsonIgnoreDefaults = jsonIgnoreDefaults,
            FromJson = fromJson,
            SourceGpPath = string.IsNullOrWhiteSpace(sourceGpPath) ? null : Path.GetFullPath(sourceGpPath),
            DiagnosticsOutPath = string.IsNullOrWhiteSpace(diagnosticsOutPath) ? null : Path.GetFullPath(diagnosticsOutPath),
            DiagnosticsAsJson = diagnosticsAsJson,
            BatchInputDir = string.IsNullOrWhiteSpace(batchInputDir) ? null : Path.GetFullPath(batchInputDir),
            BatchOutputDir = string.IsNullOrWhiteSpace(batchOutputDir) ? null : Path.GetFullPath(batchOutputDir),
            BatchRoundTripDiagnostics = batchRoundTripDiagnostics,
            ContinueOnError = continueOnError,
            FailureLogPath = string.IsNullOrWhiteSpace(failureLogPath) ? null : Path.GetFullPath(failureLogPath)
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
