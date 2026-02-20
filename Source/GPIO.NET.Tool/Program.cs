using GPIO.NET;
using GPIO.NET.Tool.Cli;
using System.IO.Compression;

try
{
    var options = CliParser.Parse(args);

    if (!File.Exists(options.InputPath))
    {
        Console.Error.WriteLine($"Input file not found: {options.InputPath}");
        return 2;
    }

    var outputPath = options.OutputPath ?? BuildDefaultOutputPath(options.InputPath, options.Format);

    switch (options.Format)
    {
        case OutputFormat.Json:
        {
            var reader = new GuitarProReader();
            var score = await reader.ReadAsync(options.InputPath).ConfigureAwait(false);
            var json = score.ToJson(
                indented: options.JsonIndented,
                ignoreNullValues: options.JsonIgnoreNull,
                ignoreDefaultValues: options.JsonIgnoreDefaults);

            EnsureOutputDirectory(outputPath);
            await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);

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

static string BuildDefaultOutputPath(string inputPath, OutputFormat format)
{
    var extension = format switch
    {
        OutputFormat.Json => ".mapped.json",
        OutputFormat.Gpif => ".score.gpif",
        OutputFormat.Midi => ".mid",
        _ => ".out"
    };

    var directory = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
    var baseName = Path.GetFileNameWithoutExtension(inputPath);
    return Path.Combine(directory, baseName + extension);
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
    Console.WriteLine("  dotnet run --project Source/GPIO.NET.Tool -- <input.gp> [output-path] [options]");
    Console.WriteLine();
    Console.WriteLine("Formats:");
    Console.WriteLine("  --format json   (default) mapped domain model JSON");
    Console.WriteLine("  --format gpif              extracted Content/score.gpif XML");
    Console.WriteLine("  --format midi              planned; currently not implemented");
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
