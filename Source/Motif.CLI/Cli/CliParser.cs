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
        CliFormat? inputFormat = null;
        CliFormat? outputFormat = null;
        CliFormat? outputFormatAlias = null;
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

                    outputFormatAlias = ParseFormat(value, name);
                    break;

                case "--output-format":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    outputFormat = ParseFormat(value, name);
                    break;

                case "--input-format":
                    if (string.IsNullOrWhiteSpace(value) && i + 1 < args.Length)
                    {
                        value = args[++i];
                    }

                    inputFormat = ParseFormat(value, name);
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
                    if (fromJson)
                    {
                        inputFormat = CliFormat.Json;
                    }

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

        if (outputFormat is null)
        {
            outputFormat = ResolveLegacyOutputFormatAlias(outputFormatAlias, fromJson);
        }

        var resolvedInputFormat = ResolveInputFormat(inputPath, batchInputDir, inputFormat, fromJson);
        var resolvedOutputFormat = ResolveOutputFormat(outputPath, outputFormat, resolvedInputFormat, batchInputDir is not null);

        return new CliOptions
        {
            InputPath = inputPath is null ? string.Empty : Path.GetFullPath(inputPath),
            OutputPath = string.IsNullOrWhiteSpace(outputPath) ? null : Path.GetFullPath(outputPath),
            InputFormat = resolvedInputFormat,
            OutputFormat = resolvedOutputFormat,
            JsonIndented = jsonIndented,
            JsonIgnoreNull = jsonIgnoreNull,
            JsonIgnoreDefaults = jsonIgnoreDefaults,
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
        var idx = arg.AsSpan().IndexOf('=');
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

        return value.ToUpperInvariant() switch
        {
            "1" or "TRUE" or "YES" or "ON" => true,
            "0" or "FALSE" or "NO" or "OFF" => false,
            _ => throw new ArgumentException($"Invalid boolean option value '{value}'.")
        };
    }

    private static CliFormat ParseFormat(string? value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{optionName} requires a format value.");
        }

        return value.ToUpperInvariant() switch
        {
            "JSON" => CliFormat.Json,
            "GP" or "GUITARPRO" => CliFormat.GuitarPro,
            "GPIF" => CliFormat.Gpif,
            _ => throw new ArgumentException($"Unknown format '{value}'.")
        };
    }

    private static CliFormat ResolveInputFormat(
        string? inputPath,
        string? batchInputDir,
        CliFormat? explicitFormat,
        bool fromJson)
    {
        if (!string.IsNullOrWhiteSpace(batchInputDir))
        {
            if (explicitFormat is not null && explicitFormat != CliFormat.GuitarPro)
            {
                throw new ArgumentException("Batch mode currently supports --input-format gp only.");
            }

            if (fromJson)
            {
                throw new ArgumentException("Batch mode does not support --from-json.");
            }

            return CliFormat.GuitarPro;
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Missing input path.");
        }

        var inferred = InferFormatFromPath(inputPath);
        if (explicitFormat is not null)
        {
            if (inferred is not null && inferred != explicitFormat)
            {
                throw new ArgumentException(
                    $"Input path '{inputPath}' conflicts with --input-format {FormatToken(explicitFormat.Value)}.");
            }

            return explicitFormat.Value;
        }

        if (inferred is null)
        {
            throw new ArgumentException(
                $"Unable to infer input format from '{inputPath}'. Pass --input-format <json|gp|gpif>.");
        }

        return inferred.Value;
    }

    private static CliFormat ResolveOutputFormat(
        string? outputPath,
        CliFormat? explicitFormat,
        CliFormat inputFormat,
        bool batchMode)
    {
        var inferred = string.IsNullOrWhiteSpace(outputPath) ? null : InferFormatFromPath(outputPath);

        if (explicitFormat is not null)
        {
            if (inferred is not null && inferred != explicitFormat)
            {
                throw new ArgumentException(
                    $"Output path '{outputPath}' conflicts with --output-format {FormatToken(explicitFormat.Value)}.");
            }

            return explicitFormat.Value;
        }

        if (inferred is not null)
        {
            return inferred.Value;
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                $"Unable to infer output format from '{outputPath}'. Pass --output-format <json|gp|gpif>.");
        }

        if (batchMode)
        {
            return CliFormat.Json;
        }

        return inputFormat switch
        {
            CliFormat.GuitarPro => CliFormat.Json,
            CliFormat.Gpif => CliFormat.Json,
            CliFormat.Json => CliFormat.GuitarPro,
            _ => throw new ArgumentException(
                $"Unable to infer an output format for {FormatToken(inputFormat)} input. Pass --output-format explicitly.")
        };
    }

    private static CliFormat? ResolveLegacyOutputFormatAlias(CliFormat? aliasFormat, bool fromJson)
    {
        if (aliasFormat is null)
        {
            return null;
        }

        // `--from-json --format json` was the legacy way to say "JSON input, write .gp".
        if (fromJson && aliasFormat == CliFormat.Json)
        {
            return null;
        }

        return aliasFormat;
    }

    private static CliFormat? InferFormatFromPath(string path)
    {
        var extension = Path.GetExtension(path).ToUpperInvariant();
        return extension switch
        {
            ".JSON" => CliFormat.Json,
            ".GP" => CliFormat.GuitarPro,
            ".GPIF" => CliFormat.Gpif,
            _ => null
        };
    }

    private static string FormatToken(CliFormat format)
        => format switch
        {
            CliFormat.Json => "json",
            CliFormat.GuitarPro => "gp",
            CliFormat.Gpif => "gpif",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
}
