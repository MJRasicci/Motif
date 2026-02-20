using GPIO.NET;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using GPIO.NET.Tool.Cli;
using System.IO.Compression;
using System.Text.Json;

try
{
    var options = CliParser.Parse(args);

    if (!File.Exists(options.InputPath))
    {
        Console.Error.WriteLine($"Input file not found: {options.InputPath}");
        return 2;
    }

    var outputPath = options.OutputPath ?? BuildDefaultOutputPath(options.InputPath, options.Format, options.FromJson);

    if (options.FromJson)
    {
        if (options.Format != OutputFormat.Json)
        {
            throw new InvalidOperationException("--from-json currently supports --format json only (mapped json -> .gp).");
        }

        var json = await File.ReadAllTextAsync(options.InputPath).ConfigureAwait(false);
        var score = JsonSerializer.Deserialize<GuitarProScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("Unable to deserialize mapped score JSON.");

        var unmapper = new DefaultScoreUnmapper();
        var unmapResult = await unmapper.UnmapAsync(score).ConfigureAwait(false);

        await using var gpifBuffer = new MemoryStream();
        var serializer = new XmlGpifSerializer();
        await serializer.SerializeAsync(unmapResult.RawDocument, gpifBuffer).ConfigureAwait(false);
        gpifBuffer.Position = 0;

        var archiveWriter = new ZipGpArchiveWriter();
        await archiveWriter.WriteArchiveAsync(gpifBuffer, outputPath).ConfigureAwait(false);

        Console.WriteLine($"GP archive written: {outputPath}");
        Console.WriteLine($"Warnings: {unmapResult.Diagnostics.Warnings.Count}");

        if (unmapResult.Diagnostics.Warnings.Count > 0)
        {
            foreach (var warning in unmapResult.Diagnostics.Warnings)
            {
                Console.WriteLine($" - {warning}");
            }

            if (!string.IsNullOrWhiteSpace(options.DiagnosticsOutPath))
            {
                EnsureOutputDirectory(options.DiagnosticsOutPath);
                await File.WriteAllLinesAsync(options.DiagnosticsOutPath, unmapResult.Diagnostics.Warnings).ConfigureAwait(false);
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

static string BuildDefaultOutputPath(string inputPath, OutputFormat format, bool fromJson)
{
    if (fromJson)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, baseName + ".gp");
    }

    var extension = format switch
    {
        OutputFormat.Json => ".mapped.json",
        OutputFormat.Gpif => ".score.gpif",
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

static void PrintHelp()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project Source/GPIO.NET.Tool -- <input> [output-path] [options]");
    Console.WriteLine();
    Console.WriteLine("Read/convert modes (default):");
    Console.WriteLine("  --format json   (default) mapped domain model JSON from .gp input");
    Console.WriteLine("  --format gpif              extracted Content/score.gpif XML from .gp input");
    Console.WriteLine("  --format midi              planned; currently not implemented");
    Console.WriteLine();
    Console.WriteLine("Write mode:");
    Console.WriteLine("  --from-json                input is mapped JSON and output is .gp archive");
    Console.WriteLine("  (use --format json with --from-json)");
    Console.WriteLine("  --diagnostics-out <path>   optional warnings output file for write mode");
    Console.WriteLine();
    Console.WriteLine("Output:");
    Console.WriteLine("  --out <path>               explicit output file path");
    Console.WriteLine();
    Console.WriteLine("JSON options:");
    Console.WriteLine("  --json-indent[=true|false]         default true");
    Console.WriteLine("  --json-ignore-null[=true|false]    default false");
    Console.WriteLine("  --json-ignore-default[=true|false] default false");
    Console.WriteLine();
    Console.WriteLine("Other:");
    Console.WriteLine("  --help");
}
