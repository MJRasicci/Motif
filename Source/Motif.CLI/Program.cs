using Motif;
using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models.Write;
using Motif.Models;
using Motif.CLI;
using System.IO.Compression;
using System.Text.Json;

try
{
    var options = CliParser.Parse(args);

    if (!string.IsNullOrWhiteSpace(options.BatchInputDir))
    {
        if (string.IsNullOrWhiteSpace(options.BatchOutputDir))
        {
            throw new InvalidOperationException("Batch mode requires --batch-output-dir.");
        }

        if (options.BatchRoundTripDiagnostics)
        {
            var runner = new BatchRoundTripDiagnosticsRunner();
            return await runner.RunAsync(options).ConfigureAwait(false);
        }

        var inputRoot = options.BatchInputDir;
        var outputRoot = options.BatchOutputDir;
        Directory.CreateDirectory(outputRoot);

        if (options.InputFormat != CliFormat.GuitarPro)
        {
            throw new InvalidOperationException("Batch mode currently supports Guitar Pro input only.");
        }

        var files = Directory.GetFiles(inputRoot, "*.gp", SearchOption.AllDirectories);
        var failures = new List<BatchFailure>();
        var ok = 0;
        var reader = options.OutputFormat == CliFormat.Json ? new GuitarProReader() : null;

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(inputRoot, file);
            var outPath = Path.Combine(outputRoot, BuildBatchOutputRelativePath(rel, options.OutputFormat));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            try
            {
                switch (options.OutputFormat)
                {
                    case CliFormat.Json:
                    {
                        var mappedScore = await reader!.ReadAsync(file).ConfigureAwait(false);
                        var mappedJson = mappedScore.ToJson(
                            indented: options.JsonIndented,
                            ignoreNullValues: options.JsonIgnoreNull,
                            ignoreDefaultValues: options.JsonIgnoreDefaults);
                        await File.WriteAllTextAsync(outPath, mappedJson).ConfigureAwait(false);
                        break;
                    }

                    case CliFormat.Gpif:
                        await ExtractGpifAsync(file, outPath).ConfigureAwait(false);
                        break;

                    case CliFormat.GuitarPro:
                        if (!AreSamePath(file, outPath))
                        {
                            File.Copy(file, outPath, overwrite: true);
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported batch conversion gp -> {FormatToken(options.OutputFormat)}.");
                }

                ok++;
            }
            catch (Exception ex)
            {
                failures.Add(new BatchFailure { File = file, Output = outPath, Error = ex.ToString() });
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }

        var failureLog = options.FailureLogPath ?? Path.Combine(outputRoot, "batch-failures.jsonl");
        if (failures.Count > 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(failureLog)!);
            await File.WriteAllLinesAsync(
                    failureLog,
                    failures.Select(f => JsonSerializer.Serialize(f, CliJsonContext.Default.BatchFailure)))
                .ConfigureAwait(false);
        }

        Console.WriteLine($"Batch export complete. ok={ok} fail={failures.Count} total={files.Length}");
        if (failures.Count > 0)
        {
            Console.WriteLine($"Failure log: {failureLog}");
        }

        return failures.Count > 0 ? 10 : 0;
    }

    if (!File.Exists(options.InputPath))
    {
        await Console.Error.WriteLineAsync($"Input file not found: {options.InputPath}").ConfigureAwait(false);
        return 2;
    }

    var outputPath = options.OutputPath ?? BuildDefaultOutputPath(options.InputPath, options.OutputFormat);

    if (!string.IsNullOrWhiteSpace(options.SourceGpPath)
        && options.OutputFormat != CliFormat.GuitarPro)
    {
        throw new InvalidOperationException("--source-gp is only supported for -> .gp writes.");
    }

    switch ((options.InputFormat, options.OutputFormat))
    {
        case (CliFormat.GuitarPro, CliFormat.Gpif):
        {
            await ExtractGpifAsync(options.InputPath, outputPath).ConfigureAwait(false);
            Console.WriteLine($"Extracted GPIF written: {outputPath}");
            return 0;
        }
    }

    var score = await ReadScoreAsync(options.InputPath, options.InputFormat).ConfigureAwait(false);
    switch (options.OutputFormat)
    {
        case CliFormat.Json:
            await WriteMappedJsonAsync(score, options, outputPath).ConfigureAwait(false);
            return 0;

        case CliFormat.GuitarPro:
        {
            var sourceScore = options.InputFormat == CliFormat.GuitarPro ? score : null;
            var archiveTemplatePath = !string.IsNullOrWhiteSpace(options.SourceGpPath)
                ? options.SourceGpPath
                : options.InputFormat == CliFormat.GuitarPro
                    ? options.InputPath
                    : null;

            await WriteGuitarProArchiveAsync(options, outputPath, score, sourceScore, archiveTemplatePath).ConfigureAwait(false);
            return 0;
        }

        case CliFormat.Gpif:
            await WriteGpifAsync(score, options, outputPath).ConfigureAwait(false);
            return 0;

        default:
            throw new InvalidOperationException(
                $"Unsupported conversion {FormatToken(options.InputFormat)} -> {FormatToken(options.OutputFormat)}.");
    }
}
catch (OperationCanceledException ex) when (ex.Message == "help")
{
    PrintHelp();
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
    return 1;
}

static async Task<Score> ReadScoreAsync(string inputPath, CliFormat inputFormat)
{
    switch (inputFormat)
    {
        case CliFormat.Json:
        {
            var json = await File.ReadAllTextAsync(inputPath).ConfigureAwait(false);
            return JsonSerializer.Deserialize(json, CliJsonContext.Default.Score)
                   ?? throw new InvalidDataException("Unable to deserialize mapped score JSON.");
        }

        case CliFormat.GuitarPro:
        {
            var reader = new GuitarProReader();
            return await reader.ReadAsync(inputPath).ConfigureAwait(false);
        }

        case CliFormat.Gpif:
        {
            await using var stream = File.OpenRead(inputPath);
            var deserializer = new XmlGpifDeserializer();
            var raw = await deserializer.DeserializeAsync(stream).ConfigureAwait(false);
            var mapper = new DefaultScoreMapper();
            return await mapper.MapAsync(raw).ConfigureAwait(false);
        }

        default:
            throw new InvalidOperationException($"Unsupported input format {FormatToken(inputFormat)}.");
    }
}

static async Task WriteMappedJsonAsync(Score score, CliOptions options, string outputPath)
{
    var mappedJson = score.ToJson(
        indented: options.JsonIndented,
        ignoreNullValues: options.JsonIgnoreNull,
        ignoreDefaultValues: options.JsonIgnoreDefaults);

    EnsureOutputDirectory(outputPath);
    await File.WriteAllTextAsync(outputPath, mappedJson).ConfigureAwait(false);

    Console.WriteLine($"Mapped JSON written: {outputPath}");
    Console.WriteLine($"Title: {score.Title}");
    Console.WriteLine($"Tracks: {score.Tracks.Count}");
    Console.WriteLine($"Playback bars: {score.PlaybackMasterBarSequence.Count}");
}

static async Task WriteGpifAsync(Score score, CliOptions options, string outputPath)
{
    var unmapResult = await BuildWriteResultAsync(score).ConfigureAwait(false);
    EnsureOutputDirectory(outputPath);

    await using var output = File.Create(outputPath);
    var serializer = new XmlGpifSerializer();
    await serializer.SerializeAsync(unmapResult.RawDocument, output).ConfigureAwait(false);

    Console.WriteLine($"GPIF written: {outputPath}");
    await ReportWriteDiagnosticsAsync(options, unmapResult.Diagnostics).ConfigureAwait(false);
}

static async Task WriteGuitarProArchiveAsync(
    CliOptions options,
    string outputPath,
    Score editedScore,
    Score? sourceScore,
    string? archiveTemplatePath)
{
    if (!string.IsNullOrWhiteSpace(archiveTemplatePath) && !File.Exists(archiveTemplatePath))
    {
        throw new InvalidOperationException("--source-gp requires <path-to-existing.gp>.");
    }

    if (sourceScore is null && !string.IsNullOrWhiteSpace(options.SourceGpPath))
    {
        var reader = new GuitarProReader();
        sourceScore = await reader.ReadAsync(options.SourceGpPath).ConfigureAwait(false);
    }

    var isNoOpWrite = sourceScore is not null && IsNoOpWrite(sourceScore, editedScore);
    if (isNoOpWrite && !ReferenceEquals(sourceScore, editedScore))
    {
        editedScore.ReattachGuitarProExtensionsFrom(sourceScore!);
    }

    var unmapResult = await BuildWriteResultAsync(editedScore).ConfigureAwait(false);

    using var gpifBuffer = new MemoryStream();
    var serializer = new XmlGpifSerializer();
    await serializer.SerializeAsync(unmapResult.RawDocument, gpifBuffer).ConfigureAwait(false);

    if (isNoOpWrite)
    {
        var sourceGpifBytes = await ReadScoreGpifBytesAsync(archiveTemplatePath!).ConfigureAwait(false);
        var sourceRaw = await DeserializeRawGpifAsync(sourceGpifBytes).ConfigureAwait(false);
        GpifWriteFidelityDiagnostics.AppendNoOpSourceFidelityWarnings(
            sourceRaw,
            unmapResult.RawDocument,
            sourceGpifBytes,
            gpifBuffer.ToArray(),
            unmapResult.Diagnostics);
    }

    gpifBuffer.Position = 0;

    if (!string.IsNullOrWhiteSpace(archiveTemplatePath) && !AreSamePath(archiveTemplatePath, outputPath))
    {
        EnsureOutputDirectory(outputPath);
        File.Copy(archiveTemplatePath, outputPath, overwrite: true);
    }

    EnsureOutputDirectory(outputPath);
    var archiveWriter = new ZipGpArchiveWriter();
    await archiveWriter.WriteArchiveAsync(gpifBuffer, outputPath).ConfigureAwait(false);

    Console.WriteLine($"GP archive written: {outputPath}");
    if (!string.IsNullOrWhiteSpace(archiveTemplatePath))
    {
        Console.WriteLine($"Archive template preserved from: {archiveTemplatePath}");
    }

    await ReportWriteDiagnosticsAsync(options, unmapResult.Diagnostics).ConfigureAwait(false);
}

static async Task<WriteResult> BuildWriteResultAsync(Score score)
{
    var unmapper = new DefaultScoreUnmapper();
    return await unmapper.UnmapAsync(score).ConfigureAwait(false);
}

static async Task ReportWriteDiagnosticsAsync(CliOptions options, WriteDiagnostics diagnostics)
{
    Console.WriteLine($"Warnings: {diagnostics.Warnings.Count}");

    if (diagnostics.Warnings.Count > 0)
    {
        foreach (var warning in diagnostics.Warnings)
        {
            Console.WriteLine($" - [{warning.Code}] {warning.Category}: {warning.Message}");
        }

        if (!string.IsNullOrWhiteSpace(options.DiagnosticsOutPath))
        {
            EnsureOutputDirectory(options.DiagnosticsOutPath);
            if (options.DiagnosticsAsJson)
            {
                var jsonDiagnostics = JsonSerializer.Serialize(diagnostics.Entries.ToArray(), CliJsonContext.Default.WriteDiagnosticEntryArray);
                await File.WriteAllTextAsync(options.DiagnosticsOutPath, jsonDiagnostics).ConfigureAwait(false);
            }
            else
            {
                var lines = diagnostics.Entries.Select(d => $"[{d.Severity}] [{d.Code}] {d.Category}: {d.Message}").ToArray();
                await File.WriteAllLinesAsync(options.DiagnosticsOutPath, lines).ConfigureAwait(false);
            }

            Console.WriteLine($"Diagnostics written: {options.DiagnosticsOutPath}");
        }
    }
}

static async Task ExtractGpifAsync(string inputPath, string outputPath)
{
    await using var archive = await ZipFile.OpenReadAsync(inputPath, CancellationToken.None).ConfigureAwait(false);
    var entry = archive.GetEntry("Content/score.gpif")
                ?? throw new InvalidDataException("Archive does not contain Content/score.gpif");

    EnsureOutputDirectory(outputPath);
    await using var inStream = await entry.OpenAsync(CancellationToken.None).ConfigureAwait(false);
    await using var outStream = File.Create(outputPath);
    await inStream.CopyToAsync(outStream).ConfigureAwait(false);
}

static string BuildDefaultOutputPath(string inputPath, CliFormat outputFormat)
{
    var extension = outputFormat switch
    {
        CliFormat.Json => ".mapped.json",
        CliFormat.GuitarPro => ".gp",
        CliFormat.Gpif => ".score.gpif",
        _ => ".out"
    };

    var outDirectory = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
    var outBaseName = Path.GetFileNameWithoutExtension(inputPath);
    return Path.Combine(outDirectory, outBaseName + extension);
}

static string BuildBatchOutputRelativePath(string relativeInputPath, CliFormat outputFormat)
    => outputFormat switch
    {
        CliFormat.Json => Path.ChangeExtension(relativeInputPath, ".json"),
        CliFormat.GuitarPro => Path.ChangeExtension(relativeInputPath, ".gp"),
        CliFormat.Gpif => Path.ChangeExtension(relativeInputPath, ".score.gpif"),
        _ => throw new InvalidOperationException($"Unsupported batch output format {FormatToken(outputFormat)}.")
    };

static void EnsureOutputDirectory(string outputPath)
{
    var outputDirectory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }
}

static bool AreSamePath(string left, string right)
{
    var normalizedLeft = Path.GetFullPath(left)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var normalizedRight = Path.GetFullPath(right)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    return string.Equals(normalizedLeft, normalizedRight, comparison);
}

static bool IsNoOpWrite(Score sourceScore, Score editedScore)
{
    var sourceJson = JsonSerializer.Serialize(sourceScore, CliJsonContext.Default.Score);
    var editedJson = JsonSerializer.Serialize(editedScore, CliJsonContext.Default.Score);
    return string.Equals(sourceJson, editedJson, StringComparison.Ordinal);
}

static async Task<byte[]> ReadScoreGpifBytesAsync(string gpPath)
{
    await using var archive = await ZipFile.OpenReadAsync(gpPath, CancellationToken.None).ConfigureAwait(false);
    var entry = archive.GetEntry("Content/score.gpif")
        ?? throw new InvalidDataException("Archive does not contain Content/score.gpif");

    await using var stream = await entry.OpenAsync(CancellationToken.None).ConfigureAwait(false);
    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer).ConfigureAwait(false);
    return buffer.ToArray();
}

static async Task<Motif.Extensions.GuitarPro.Models.Raw.GpifDocument> DeserializeRawGpifAsync(byte[] gpifBytes)
{
    using var stream = new MemoryStream(gpifBytes, writable: false);
    var deserializer = new XmlGpifDeserializer();
    return await deserializer.DeserializeAsync(stream).ConfigureAwait(false);
}

static string FormatToken(CliFormat format)
    => format switch
    {
        CliFormat.Json => "json",
        CliFormat.GuitarPro => "gp",
        CliFormat.Gpif => "gpif",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

static void PrintHelp()
{
    var helpText = """
Motif CLI — score inspection and conversion

USAGE
  motif-cli <input> [output] [options]
  motif-cli --batch-input-dir <dir> --batch-output-dir <dir> [options]
  motif-cli --help

FORMAT ROUTING
  Input/output formats are inferred from file extensions when possible.
  Use --input-format / --output-format when extensions are missing or ambiguous.
  --format remains an alias for --output-format.
  --from-json remains a compatibility alias for --input-format json.

SINGLE FILE
  motif-cli song.gp
    Read a .gp file and export mapped score JSON.
    Output: song.mapped.json

  motif-cli song.gp song.score.gpif
  motif-cli song.gp --output-format gpif
    Extract the raw GPIF XML embedded in the .gp archive.

  motif-cli song.gpif song.json
  motif-cli song.json song.score.gpif
    Route through the mapped score model without going through a .gp archive.

  motif-cli song.json output.gp
    Read mapped score JSON and write a .gp archive.
    Uses the built-in default archive payload and replaces Content/score.gpif.

  motif-cli song.json output.gp --source-gp original.gp
    Preserve non-score archive entries by copying original.gp and replacing only
    Content/score.gpif.

  motif-cli score.data out.data --input-format json --output-format gp
    Explicit format routing when extensions are not usable.

  --diagnostics-out <path>
    Write writer diagnostics to a file.

  --diagnostics-json
    Write the diagnostics file as JSON instead of plain text.

BATCH EXPORT
  motif-cli --batch-input-dir ./songs --batch-output-dir ./json
    Export every .gp file found under ./songs to mapped JSON under ./json,
    mirroring the source directory structure.

  motif-cli --batch-input-dir ./songs --batch-output-dir ./gpif --output-format gpif
    Extract raw GPIF for every .gp file under ./songs.

  motif-cli --batch-input-dir ./songs --batch-output-dir ./analysis --batch-roundtrip-diagnostics
    Run a no-edit mapped JSON/full-write roundtrip for every .gp file under ./songs
    and write aggregate drift diagnostics plus JSONL file/diagnostic streams under ./analysis.

  motif-cli --batch-input-dir ./songs --batch-output-dir ./json --continue-on-error=false
  motif-cli --batch-input-dir ./songs --batch-output-dir ./json --failure-log ./failures.jsonl

  --continue-on-error[=true|false]
    Skip files that fail to parse and continue the batch (default: true).

  --failure-log <path>
    Path for the JSONL batch failure log
    (default: <batch-output-dir>/batch-failures.jsonl).

OPTIONS
  --input-format <json|gp|gpif>
                                Explicit input format
  --output-format <json|gp|gpif>
                                Explicit output format
  --format <json|gp|gpif>
                                Alias for --output-format
  --out <path>                  Explicit output file path
  --from-json                   Compatibility alias for --input-format json
  --source-gp <path>            Custom archive template for JSON -> .gp writes
  --batch-input-dir <dir>       Source directory for batch export
  --batch-output-dir <dir>      Destination directory for batch export
  --batch-roundtrip-diagnostics  In batch mode, run roundtrip diagnostics instead of JSON export
  --continue-on-error[=bool]    Skip failed files in batch mode (default: true)
  --failure-log <path>          Batch failure log path
  --diagnostics-out <path>      Write diagnostics to a file (write mode, or batch summary override)
  --diagnostics-json            Emit diagnostics as JSON
  --json-indent[=bool]          Indent JSON output (default: true)
  --json-ignore-null[=bool]     Omit null fields from JSON (default: false)
  --json-ignore-default[=bool]  Omit default-value fields from JSON (default: false)
  --help, -h                    Show this help

EXIT CODES
  0    Success
  1    Error
  2    Input file not found
  10   Batch completed with one or more failures
""";

    Console.WriteLine(helpText);
}
