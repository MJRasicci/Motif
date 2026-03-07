using GPIO.NET;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using GPIO.NET.Models.Patching;
using GPIO.NET.Tool.Cli;
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

        var files = Directory.GetFiles(inputRoot, "*.gp", SearchOption.AllDirectories);
        var failures = new List<BatchFailure>();
        var ok = 0;

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(inputRoot, file);
            var outPath = Path.Combine(outputRoot, Path.ChangeExtension(rel, ".json"));
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            try
            {
                var reader = new GuitarProReader();
                var score = await reader.ReadAsync(file).ConfigureAwait(false);
                var mappedJson = score.ToJson(
                    indented: options.JsonIndented,
                    ignoreNullValues: options.JsonIgnoreNull,
                    ignoreDefaultValues: options.JsonIgnoreDefaults);
                await File.WriteAllTextAsync(outPath, mappedJson).ConfigureAwait(false);
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
        Console.Error.WriteLine($"Input file not found: {options.InputPath}");
        return 2;
    }

    var outputPath = options.OutputPath ?? BuildDefaultOutputPath(options.InputPath, options.Format, options.FromJson, options.PlanOnly);

    if (options.FromJson)
    {
        if (options.Format != OutputFormat.Json)
        {
            throw new InvalidOperationException("--from-json currently supports --format json only.");
        }

        if (!string.IsNullOrWhiteSpace(options.SourceGpPath) && !File.Exists(options.SourceGpPath))
        {
            throw new InvalidOperationException("--source-gp requires <path-to-existing.gp>.");
        }

        var json = await File.ReadAllTextAsync(options.InputPath).ConfigureAwait(false);
        var editedScore = JsonSerializer.Deserialize(json, CliJsonContext.Default.GuitarProScore)
                          ?? throw new InvalidDataException("Unable to deserialize mapped score JSON.");

        if (options.PatchFromJson)
        {
            if (string.IsNullOrWhiteSpace(options.SourceGpPath) || !File.Exists(options.SourceGpPath))
            {
                throw new InvalidOperationException("--patch-from-json requires --source-gp <path-to-existing.gp>");
            }

            var reader = new GuitarProReader();
            var patchSourceScore = await reader.ReadAsync(options.SourceGpPath).ConfigureAwait(false);
            var plan = JsonPatchPlanner.BuildPatch(patchSourceScore, editedScore);

            if (options.Strict && plan.UnsupportedChanges.Count > 0)
            {
                throw new InvalidOperationException($"Strict mode failed: {plan.UnsupportedChanges.Count} unsupported changes detected.");
            }

            if (options.PlanOnly)
            {
                var planJson = JsonSerializer.Serialize(plan, CliJsonContext.Default.JsonPatchPlanResult);
                EnsureOutputDirectory(outputPath);
                await File.WriteAllTextAsync(outputPath, planJson).ConfigureAwait(false);
                Console.WriteLine($"Patch plan written: {outputPath}");
                return 0;
            }

            var patcher = new GuitarProPatcher();
            var patchResult = await patcher.PatchAsync(options.SourceGpPath, outputPath, plan.Patch).ConfigureAwait(false);

            Console.WriteLine($"Patched GP archive written: {outputPath}");
            Console.WriteLine($"Patch operations logged: {patchResult.Diagnostics.Entries.Count}");

            if (!string.IsNullOrWhiteSpace(options.DiagnosticsOutPath))
            {
                EnsureOutputDirectory(options.DiagnosticsOutPath);
                if (options.DiagnosticsAsJson)
                {
                    var jsonDiagnostics = JsonSerializer.Serialize(
                        new PatchDiagnosticsOutput
                        {
                            UnsupportedChanges = plan.UnsupportedChanges,
                            PatchDiagnostics = patchResult.Diagnostics.Entries
                        },
                        CliJsonContext.Default.PatchDiagnosticsOutput);
                    await File.WriteAllTextAsync(options.DiagnosticsOutPath, jsonDiagnostics).ConfigureAwait(false);
                }
                else
                {
                    var lines = plan.UnsupportedChanges.Select(u => $"[planner] {u}")
                        .Concat(patchResult.Diagnostics.Entries.Select(d => $"[{d.Operation}] {d.Message}"))
                        .ToArray();
                    await File.WriteAllLinesAsync(options.DiagnosticsOutPath, lines).ConfigureAwait(false);
                }

                Console.WriteLine($"Diagnostics written: {options.DiagnosticsOutPath}");
            }

            return 0;
        }

        var sourceScore = default(GuitarProScore);
        if (!string.IsNullOrWhiteSpace(options.SourceGpPath))
        {
            var reader = new GuitarProReader();
            sourceScore = await reader.ReadAsync(options.SourceGpPath).ConfigureAwait(false);
        }

        var unmapper = new DefaultScoreUnmapper();
        var unmapResult = await unmapper.UnmapAsync(editedScore).ConfigureAwait(false);

        await using var gpifBuffer = new MemoryStream();
        var serializer = new XmlGpifSerializer();
        await serializer.SerializeAsync(unmapResult.RawDocument, gpifBuffer).ConfigureAwait(false);

        if (sourceScore is not null && IsNoOpWrite(sourceScore, editedScore))
        {
            var sourceGpifBytes = await ReadScoreGpifBytesAsync(options.SourceGpPath!).ConfigureAwait(false);
            var sourceRaw = await DeserializeRawGpifAsync(sourceGpifBytes).ConfigureAwait(false);
            GpifWriteFidelityDiagnostics.AppendNoOpSourceFidelityWarnings(
                sourceRaw,
                unmapResult.RawDocument,
                sourceGpifBytes,
                gpifBuffer.ToArray(),
                unmapResult.Diagnostics);
        }

        gpifBuffer.Position = 0;

        if (!string.IsNullOrWhiteSpace(options.SourceGpPath) && !AreSamePath(options.SourceGpPath, outputPath))
        {
            EnsureOutputDirectory(outputPath);
            File.Copy(options.SourceGpPath, outputPath, overwrite: true);
        }

        var archiveWriter = new ZipGpArchiveWriter();
        await archiveWriter.WriteArchiveAsync(gpifBuffer, outputPath).ConfigureAwait(false);

        Console.WriteLine($"GP archive written: {outputPath}");
        if (!string.IsNullOrWhiteSpace(options.SourceGpPath))
        {
            Console.WriteLine($"Archive template preserved from: {options.SourceGpPath}");
        }

        Console.WriteLine($"Warnings: {unmapResult.Diagnostics.Warnings.Count}");

        if (unmapResult.Diagnostics.Warnings.Count > 0)
        {
            foreach (var warning in unmapResult.Diagnostics.Warnings)
            {
                Console.WriteLine($" - [{warning.Code}] {warning.Category}: {warning.Message}");
            }

            if (!string.IsNullOrWhiteSpace(options.DiagnosticsOutPath))
            {
                EnsureOutputDirectory(options.DiagnosticsOutPath);
                if (options.DiagnosticsAsJson)
                {
                    var jsonDiagnostics = JsonSerializer.Serialize(unmapResult.Diagnostics.Entries.ToArray(), CliJsonContext.Default.WriteDiagnosticEntryArray);
                    await File.WriteAllTextAsync(options.DiagnosticsOutPath, jsonDiagnostics).ConfigureAwait(false);
                }
                else
                {
                    var lines = unmapResult.Diagnostics.Entries.Select(d => $"[{d.Severity}] [{d.Code}] {d.Category}: {d.Message}").ToArray();
                    await File.WriteAllLinesAsync(options.DiagnosticsOutPath, lines).ConfigureAwait(false);
                }

                Console.WriteLine($"Diagnostics written: {options.DiagnosticsOutPath}");
            }
        }

        return 0;
    }

    switch (options.Format)
    {
        case OutputFormat.Json:
        {
            var reader = new GuitarProReader();
            var score = await reader.ReadAsync(options.InputPath).ConfigureAwait(false);
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
            break;
        }

        case OutputFormat.Gpif:
        {
            await using var archive = await ZipFile.OpenReadAsync(options.InputPath, CancellationToken.None).ConfigureAwait(false);
            var entry = archive.GetEntry("Content/score.gpif")
                        ?? throw new InvalidDataException("Archive does not contain Content/score.gpif");

            EnsureOutputDirectory(outputPath);
            await using var inStream = await entry.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await using var outStream = File.Create(outputPath);
            await inStream.CopyToAsync(outStream).ConfigureAwait(false);

            Console.WriteLine($"Extracted GPIF written: {outputPath}");
            break;
        }

        case OutputFormat.MusicXml:
            throw new NotImplementedException("MusicXML output is planned but not implemented yet.");

        case OutputFormat.Midi:
            throw new NotImplementedException("MIDI output is planned but not implemented yet.");

        default:
            throw new ArgumentOutOfRangeException();
    }

    return 0;
}
catch (OperationCanceledException ex) when (ex.Message == "help")
{
    PrintHelp();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static string BuildDefaultOutputPath(string inputPath, OutputFormat format, bool fromJson, bool planOnly)
{
    if (fromJson)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, baseName + (planOnly ? ".patch-plan.json" : ".gp"));
    }

    var extension = format switch
    {
        OutputFormat.Json => ".mapped.json",
        OutputFormat.Gpif => ".score.gpif",
        OutputFormat.MusicXml => ".mxl",
        OutputFormat.Midi => ".mid",
        _ => ".out"
    };

    var outDirectory = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
    var outBaseName = Path.GetFileNameWithoutExtension(inputPath);
    return Path.Combine(outDirectory, outBaseName + extension);
}

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

static bool IsNoOpWrite(GuitarProScore sourceScore, GuitarProScore editedScore)
{
    var plan = JsonPatchPlanner.BuildPatch(sourceScore, editedScore);
    return plan.UnsupportedChanges.Count == 0 && !HasPatchOperations(plan.Patch);
}

static bool HasPatchOperations(GpPatchDocument patch)
    => patch.AppendBars.Count > 0
       || patch.AppendVoices.Count > 0
       || patch.AppendNotes.Count > 0
       || patch.InsertBeats.Count > 0
       || patch.AddNotesToBeats.Count > 0
       || patch.ReorderBeatNotes.Count > 0
       || patch.UpdateNoteArticulations.Count > 0
       || patch.UpdateNotePitches.Count > 0
       || patch.DeleteNotes.Count > 0
       || patch.DeleteBeats.Count > 0;

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

static async Task<GPIO.NET.Models.Raw.GpifDocument> DeserializeRawGpifAsync(byte[] gpifBytes)
{
    await using var stream = new MemoryStream(gpifBytes, writable: false);
    var deserializer = new XmlGpifDeserializer();
    return await deserializer.DeserializeAsync(stream).ConfigureAwait(false);
}

static void PrintHelp()
{
    var helpText = """
GPIO.NET — Guitar Pro file parser and writer

USAGE
  gpio <input> [output] [options]
  gpio --batch-input-dir <dir> --batch-output-dir <dir> [options]
  gpio --help

READ MODES  (default: --format json)
  gpio song.gp
    Read a .gp file and export a mapped domain model JSON.
    Output: song.mapped.json

  gpio song.gp --format gpif
    Extract the raw GPIF XML embedded in the .gp archive.
    Output: song.score.gpif

  gpio song.gp --out out.json --format json

  --format musicxml  (planned, not yet implemented)
  --format midi      (planned, not yet implemented)

WRITE MODES  (--from-json)
  gpio song.mapped.json --from-json
    Round-trip: read mapped JSON and write a new .gp archive.
    Uses built-in default archive sidecar content and replaces Content/score.gpif.
    Output: song.gp

  gpio song.mapped.json --from-json --source-gp original.gp
    Round-trip with archive template preservation:
    copies original.gp and replaces only Content/score.gpif.
    Preserves stylesheets, score views, preferences, and other zip entries.

  gpio song.mapped.json --from-json --patch-from-json --source-gp original.gp
    Patch mode: diff the edited mapped JSON against an existing .gp file and
    apply only the supported changes.
    Output: song.gp

  gpio song.mapped.json --from-json --patch-from-json --source-gp original.gp --plan-only
    Generate a patch plan JSON without applying it.
    Output: song.patch-plan.json

  --strict
    Fail if the planner detects any unsupported changes (default: off).

  --diagnostics-out <path>
    Write write/patch diagnostics to a file.

  --diagnostics-json
    Write the diagnostics file as JSON instead of plain text.

BATCH EXPORT
  gpio --batch-input-dir ./songs --batch-output-dir ./json
    Export every .gp file found under ./songs to mapped JSON under ./json,
    mirroring the source directory structure.

  gpio --batch-input-dir ./songs --batch-output-dir ./analysis --batch-roundtrip-diagnostics
    Run a no-edit mapped JSON/full-write roundtrip for every .gp file under ./songs
    and write aggregate drift diagnostics plus JSONL file/diagnostic streams under ./analysis.

  gpio --batch-input-dir ./songs --batch-output-dir ./json --continue-on-error=false
  gpio --batch-input-dir ./songs --batch-output-dir ./json --failure-log ./failures.jsonl

  --continue-on-error[=true|false]
    Skip files that fail to parse and continue the batch (default: true).

  --failure-log <path>
    Path for the JSONL batch failure log
    (default: <batch-output-dir>/batch-failures.jsonl).

OPTIONS
  --format <json|gpif|musicxml|midi>   Output format (default: json; musicxml and midi are planned)
  --out <path>                   Explicit output file path
  --from-json                    Input is mapped JSON; output is a .gp archive
  --patch-from-json              Enable patch mode (requires --from-json and --source-gp)
  --source-gp <path>             Source .gp for patch mode, or custom archive template for --from-json
  --plan-only                    Write patch plan JSON without patching
  --strict                       Fail on unsupported changes in patch mode
  --batch-input-dir <dir>        Source directory for batch export
  --batch-output-dir <dir>       Destination directory for batch export
  --batch-roundtrip-diagnostics  In batch mode, run roundtrip diagnostics instead of JSON export
  --continue-on-error[=bool]     Skip failed files in batch mode (default: true)
  --failure-log <path>           Batch failure log path
  --diagnostics-out <path>       Write diagnostics to a file (write/patch modes, or batch summary override)
  --diagnostics-json             Emit diagnostics as JSON
  --json-indent[=bool]           Indent JSON output (default: true)
  --json-ignore-null[=bool]      Omit null fields from JSON (default: false)
  --json-ignore-default[=bool]   Omit default-value fields from JSON (default: false)
  --help, -h                     Show this help

EXIT CODES
  0    Success
  1    Error
  2    Input file not found
  10   Batch completed with one or more failures
""";

    Console.WriteLine(helpText);
}
