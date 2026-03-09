namespace Motif.CLI;

using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;
using Motif.Extensions.GuitarPro.Models.Write;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

internal sealed class BatchRoundTripDiagnosticsRunner
{
    private static readonly CliJsonContext CompactJsonContext = new(new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    });

    private const int SampleLimit = 5;
    private const int TopBucketLimit = 20;
    private const int TopPathLimit = 50;
    private const int TopFileLimit = 25;

    public async ValueTask<int> RunAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BatchInputDir))
        {
            throw new InvalidOperationException("Batch roundtrip diagnostics requires --batch-input-dir.");
        }

        if (string.IsNullOrWhiteSpace(options.BatchOutputDir))
        {
            throw new InvalidOperationException("Batch roundtrip diagnostics requires --batch-output-dir.");
        }

        if (options.FromJson)
        {
            throw new InvalidOperationException("Batch roundtrip diagnostics cannot be combined with --from-json.");
        }

        if (options.Format != OutputFormat.Json)
        {
            throw new InvalidOperationException("Batch roundtrip diagnostics currently supports --format json only.");
        }

        var inputRoot = options.BatchInputDir;
        var outputRoot = options.BatchOutputDir;
        Directory.CreateDirectory(outputRoot);

        var summaryJsonPath = Path.Combine(outputRoot, "batch-roundtrip-summary.json");
        var summaryTextPath = Path.Combine(outputRoot, "batch-roundtrip-summary.txt");
        var fileResultsPath = Path.Combine(outputRoot, "batch-file-results.jsonl");
        var diagnosticsPath = Path.Combine(outputRoot, "batch-diagnostics.jsonl");
        var failureLogPath = options.FailureLogPath ?? Path.Combine(outputRoot, "batch-failures.jsonl");
        var requestedSummaryPath = options.DiagnosticsOutPath;

        var files = Directory.GetFiles(inputRoot, "*.gp", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var failures = new List<BatchFailure>();
        var aggregate = new BatchAggregateBuilder();
        var reader = new GuitarProReader();
        var unmapper = new DefaultScoreUnmapper();
        var serializer = new XmlGpifSerializer();
        var deserializer = new XmlGpifDeserializer();

        using var fileResultsWriter = new StreamWriter(fileResultsPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var diagnosticsWriter = new StreamWriter(diagnosticsPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        for (var index = 0; index < files.Length; index++)
        {
            var file = files[index];
            var relativePath = Path.GetRelativePath(inputRoot, file);
            Console.WriteLine($"[{index + 1}/{files.Length}] {relativePath}");

            try
            {
                var analyzed = await AnalyzeFileAsync(file, relativePath, reader, unmapper, serializer, deserializer, cancellationToken).ConfigureAwait(false);

                await fileResultsWriter.WriteLineAsync(
                        JsonSerializer.Serialize(analyzed.FileResult, CompactJsonContext.BatchRoundTripFileResult))
                    .ConfigureAwait(false);

                foreach (var entry in analyzed.Diagnostics)
                {
                    var logEntry = new BatchRoundTripDiagnosticLogEntry
                    {
                        File = file,
                        RelativePath = relativePath,
                        Code = entry.Code,
                        Category = entry.Category,
                        Severity = entry.Severity.ToString(),
                        Message = entry.Message,
                        Path = entry.Path,
                        SourceValue = entry.SourceValue,
                        OutputValue = entry.OutputValue
                    };

                    await diagnosticsWriter.WriteLineAsync(
                            JsonSerializer.Serialize(logEntry, CompactJsonContext.BatchRoundTripDiagnosticLogEntry))
                        .ConfigureAwait(false);
                }

                aggregate.Add(analyzed.FileResult, analyzed.Diagnostics);
            }
            catch (Exception ex)
            {
                failures.Add(new BatchFailure
                {
                    File = file,
                    Output = outputRoot,
                    Error = ex.ToString()
                });

                Console.WriteLine($"  failed: {ex.Message}");
                if (!options.ContinueOnError)
                {
                    throw;
                }
            }
        }

        await fileResultsWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        await diagnosticsWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

        if (failures.Count > 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(failureLogPath)!);
            await File.WriteAllLinesAsync(
                    failureLogPath,
                    failures.Select(f => JsonSerializer.Serialize(f, CompactJsonContext.BatchFailure)),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else if (File.Exists(failureLogPath))
        {
            File.Delete(failureLogPath);
        }

        var summary = aggregate.Build(
            inputRoot,
            outputRoot,
            summaryJsonPath,
            fileResultsPath,
            diagnosticsPath,
            failureLogPath,
            totalFiles: files.Length,
            failedFiles: failures.Count);

        var summaryJson = JsonSerializer.Serialize(summary, CliJsonContext.Default.BatchRoundTripSummary);
        var summaryText = BuildSummaryText(summary);

        await File.WriteAllTextAsync(summaryJsonPath, summaryJson, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(summaryTextPath, summaryText, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(requestedSummaryPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(requestedSummaryPath)!);
            if (options.DiagnosticsAsJson)
            {
                await File.WriteAllTextAsync(requestedSummaryPath, summaryJson, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await File.WriteAllTextAsync(requestedSummaryPath, summaryText, cancellationToken).ConfigureAwait(false);
            }
        }

        Console.WriteLine($"Batch roundtrip diagnostics complete. ok={summary.SucceededFiles} fail={summary.FailedFiles} total={summary.TotalFiles}");
        Console.WriteLine($"Summary JSON: {summaryJsonPath}");
        Console.WriteLine($"Summary text: {summaryTextPath}");
        Console.WriteLine($"File results: {fileResultsPath}");
        Console.WriteLine($"Diagnostics log: {diagnosticsPath}");
        if (failures.Count > 0)
        {
            Console.WriteLine($"Failure log: {failureLogPath}");
        }

        return failures.Count > 0 ? 10 : 0;
    }

    private static async ValueTask<AnalyzedFile> AnalyzeFileAsync(
        string file,
        string relativePath,
        GuitarProReader reader,
        DefaultScoreUnmapper unmapper,
        XmlGpifSerializer serializer,
        XmlGpifDeserializer deserializer,
        CancellationToken cancellationToken)
    {
        var sourceGpifBytes = await ReadScoreGpifBytesAsync(file, cancellationToken).ConfigureAwait(false);
        var sourceRaw = await DeserializeRawGpifAsync(sourceGpifBytes, deserializer, cancellationToken).ConfigureAwait(false);
        var sourceScore = await reader.ReadAsync(file, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Use the CLI source-generated context here so batch diagnostics stay trim/AOT-safe while still
        // exercising the JSON roundtrip. ignore-default/null trims would artificially create drift.
        var mappedJson = JsonSerializer.Serialize(sourceScore, CompactJsonContext.GuitarProScore);
        var jsonScore = JsonSerializer.Deserialize(mappedJson, CliJsonContext.Default.GuitarProScore)
            ?? throw new InvalidDataException("Unable to deserialize mapped score JSON during batch roundtrip diagnostics.");

        var unmapResult = await unmapper.UnmapAsync(jsonScore, cancellationToken).ConfigureAwait(false);

        await using var gpifBuffer = new MemoryStream();
        await serializer.SerializeAsync(unmapResult.RawDocument, gpifBuffer, cancellationToken).ConfigureAwait(false);
        var outputGpifBytes = gpifBuffer.ToArray();

        GpifWriteFidelityDiagnostics.AppendNoOpSourceFidelityWarnings(
            sourceRaw,
            unmapResult.RawDocument,
            sourceGpifBytes,
            outputGpifBytes,
            unmapResult.Diagnostics);

        var diagnostics = unmapResult.Diagnostics.Entries.ToArray();
        var diagnosticCodeCounts = BuildNamedCounts(diagnostics.GroupBy(entry => entry.Code), relativePath);
        var diagnosticSectionCounts = BuildNamedCounts(diagnostics.GroupBy(entry => GetTopLevelSection(entry.Path)), relativePath);

        var fileResult = new BatchRoundTripFileResult
        {
            File = file,
            RelativePath = relativePath,
            GpifBytesIdentical = sourceGpifBytes.AsSpan().SequenceEqual(outputGpifBytes),
            DiagnosticCount = diagnostics.Length,
            WarningCount = diagnostics.Count(entry => entry.Severity == WriteDiagnosticSeverity.Warning),
            InfoCount = diagnostics.Count(entry => entry.Severity == WriteDiagnosticSeverity.Info),
            DiagnosticCodeCounts = diagnosticCodeCounts,
            DiagnosticSectionCounts = diagnosticSectionCounts
        };

        return new AnalyzedFile(fileResult, diagnostics);
    }

    private static BatchNamedCount[] BuildNamedCounts<T>(
        IEnumerable<IGrouping<string, T>> groups,
        string relativePath)
    {
        return groups
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new BatchNamedCount
            {
                Name = group.Key,
                Count = group.Count(),
                FileCount = 1,
                SampleFiles = [relativePath]
            })
            .ToArray();
    }

    private static string GetTopLevelSection(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "(none)";
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : segments[0];
    }

    private static async Task<byte[]> ReadScoreGpifBytesAsync(string gpPath, CancellationToken cancellationToken)
    {
        await using var archive = await ZipFile.OpenReadAsync(gpPath, cancellationToken).ConfigureAwait(false);
        var entry = archive.GetEntry("Content/score.gpif")
            ?? throw new InvalidDataException("Archive does not contain Content/score.gpif");

        await using var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static async Task<GpifDocument> DeserializeRawGpifAsync(
        byte[] gpifBytes,
        XmlGpifDeserializer deserializer,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(gpifBytes, writable: false);
        return await deserializer.DeserializeAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildSummaryText(BatchRoundTripSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Batch roundtrip diagnostics");
        builder.AppendLine($"GeneratedAtUtc: {summary.GeneratedAtUtc}");
        builder.AppendLine($"InputRoot: {summary.InputRoot}");
        builder.AppendLine($"OutputRoot: {summary.OutputRoot}");
        builder.AppendLine($"TotalFiles: {summary.TotalFiles}");
        builder.AppendLine($"SucceededFiles: {summary.SucceededFiles}");
        builder.AppendLine($"FailedFiles: {summary.FailedFiles}");
        builder.AppendLine($"CleanFiles: {summary.CleanFiles}");
        builder.AppendLine($"FilesWithDiagnostics: {summary.FilesWithDiagnostics}");
        builder.AppendLine($"FilesWithByteDrift: {summary.FilesWithByteDrift}");
        builder.AppendLine($"TotalDiagnostics: {summary.TotalDiagnostics}");
        builder.AppendLine($"TotalWarnings: {summary.TotalWarnings}");
        builder.AppendLine($"TotalInfos: {summary.TotalInfos}");
        AppendNamedCounts(builder, "DiagnosticCodes", summary.DiagnosticCodes);
        AppendNamedCounts(builder, "DiagnosticSections", summary.DiagnosticSections);
        AppendNamedCounts(builder, "MissingElements", summary.MissingElements);
        AppendNamedCounts(builder, "AddedElements", summary.AddedElements);
        AppendNamedCounts(builder, "ValueDrifts", summary.ValueDrifts);
        AppendNamedCounts(builder, "AttributeDrifts", summary.AttributeDrifts);
        AppendPathCounts(builder, "TopNormalizedPaths", summary.TopNormalizedPaths);
        AppendTopFiles(builder, summary.MostChangedFiles);
        return builder.ToString();
    }

    private static void AppendNamedCounts(StringBuilder builder, string title, IReadOnlyList<BatchNamedCount> counts)
    {
        builder.AppendLine();
        builder.AppendLine($"{title}:");
        if (counts.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        foreach (var count in counts)
        {
            var samples = count.SampleFiles.Length == 0 ? string.Empty : $" | files: {string.Join(", ", count.SampleFiles)}";
            builder.AppendLine($"  {count.Name}: count={count.Count} files={count.FileCount}{samples}");
        }
    }

    private static void AppendPathCounts(StringBuilder builder, string title, IReadOnlyList<BatchPathSummary> counts)
    {
        builder.AppendLine();
        builder.AppendLine($"{title}:");
        if (counts.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        foreach (var count in counts)
        {
            var samples = count.SampleFiles.Length == 0 ? string.Empty : $" | files: {string.Join(", ", count.SampleFiles)}";
            builder.AppendLine($"  [{count.Code}] {count.Path}: count={count.Count} files={count.FileCount}{samples}");
        }
    }

    private static void AppendTopFiles(StringBuilder builder, IReadOnlyList<BatchFileHeadline> files)
    {
        builder.AppendLine();
        builder.AppendLine("MostChangedFiles:");
        if (files.Count == 0)
        {
            builder.AppendLine("  (none)");
            return;
        }

        foreach (var file in files)
        {
            builder.AppendLine($"  {file.RelativePath}: diagnostics={file.DiagnosticCount}");
        }
    }

    private readonly record struct AnalyzedFile(
        BatchRoundTripFileResult FileResult,
        IReadOnlyList<WriteDiagnosticEntry> Diagnostics);

    private sealed class BatchAggregateBuilder
    {
        private readonly Dictionary<string, AggregateBucket> diagnosticCodes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AggregateBucket> diagnosticSections = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AggregateBucket> missingElements = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AggregateBucket> addedElements = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AggregateBucket> valueDrifts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AggregateBucket> attributeDrifts = new(StringComparer.Ordinal);
        private readonly Dictionary<(string Code, string Path), AggregateBucket> normalizedPaths = new();
        private readonly List<BatchFileHeadline> mostChangedFiles = [];
        private int succeededFiles;
        private int cleanFiles;
        private int filesWithDiagnostics;
        private int filesWithByteDrift;
        private int totalDiagnostics;
        private int totalWarnings;
        private int totalInfos;

        public void Add(
            BatchRoundTripFileResult fileResult,
            IReadOnlyList<WriteDiagnosticEntry> diagnostics)
        {
            succeededFiles++;
            if (fileResult.DiagnosticCount == 0)
            {
                cleanFiles++;
            }
            else
            {
                filesWithDiagnostics++;
            }

            if (!fileResult.GpifBytesIdentical)
            {
                filesWithByteDrift++;
            }

            totalDiagnostics += fileResult.DiagnosticCount;
            totalWarnings += fileResult.WarningCount;
            totalInfos += fileResult.InfoCount;

            foreach (var diagnostic in diagnostics)
            {
                AddBucket(diagnosticCodes, diagnostic.Code, fileResult.RelativePath);
                AddBucket(diagnosticSections, GetTopLevelSection(diagnostic.Path), fileResult.RelativePath);
                AddBucket(normalizedPaths, (diagnostic.Code, NormalizeDiagnosticPath(diagnostic.Path)), fileResult.RelativePath);

                switch (diagnostic.Code)
                {
                    case "RAW_XML_ELEMENT_MISSING":
                        AddBucket(missingElements, GetLeafName(diagnostic.Path), fileResult.RelativePath);
                        break;

                    case "RAW_XML_ELEMENT_ADDED":
                        AddBucket(addedElements, GetLeafName(diagnostic.Path), fileResult.RelativePath);
                        break;

                    case "RAW_XML_VALUE_DRIFT":
                        AddBucket(valueDrifts, GetLeafName(diagnostic.Path), fileResult.RelativePath);
                        break;

                    case "RAW_XML_ATTRIBUTE_MISSING":
                    case "RAW_XML_ATTRIBUTE_ADDED":
                    case "RAW_XML_ATTRIBUTE_DRIFT":
                        AddBucket(attributeDrifts, GetLeafName(diagnostic.Path), fileResult.RelativePath);
                        break;
                }
            }

            mostChangedFiles.Add(new BatchFileHeadline
            {
                RelativePath = fileResult.RelativePath,
                DiagnosticCount = fileResult.DiagnosticCount
            });
        }

        public BatchRoundTripSummary Build(
            string inputRoot,
            string outputRoot,
            string summaryPath,
            string fileResultsPath,
            string diagnosticsPath,
            string failureLogPath,
            int totalFiles,
            int failedFiles)
        {
            return new BatchRoundTripSummary
            {
                InputRoot = inputRoot,
                OutputRoot = outputRoot,
                SummaryPath = summaryPath,
                FileResultsPath = fileResultsPath,
                DiagnosticsPath = diagnosticsPath,
                FailureLogPath = failureLogPath,
                GeneratedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                TotalFiles = totalFiles,
                SucceededFiles = succeededFiles,
                FailedFiles = failedFiles,
                CleanFiles = cleanFiles,
                FilesWithDiagnostics = filesWithDiagnostics,
                FilesWithByteDrift = filesWithByteDrift,
                TotalDiagnostics = totalDiagnostics,
                TotalWarnings = totalWarnings,
                TotalInfos = totalInfos,
                DiagnosticCodes = ToNamedCounts(diagnosticCodes),
                DiagnosticSections = ToNamedCounts(diagnosticSections),
                MissingElements = ToNamedCounts(missingElements),
                AddedElements = ToNamedCounts(addedElements),
                ValueDrifts = ToNamedCounts(valueDrifts),
                AttributeDrifts = ToNamedCounts(attributeDrifts),
                TopNormalizedPaths = normalizedPaths
                    .OrderByDescending(pair => pair.Value.Count)
                    .ThenBy(pair => pair.Key.Code, StringComparer.Ordinal)
                    .ThenBy(pair => pair.Key.Path, StringComparer.Ordinal)
                    .Take(TopPathLimit)
                    .Select(pair => new BatchPathSummary
                    {
                        Code = pair.Key.Code,
                        Path = pair.Key.Path,
                        Count = pair.Value.Count,
                        FileCount = pair.Value.FileCount,
                        SampleFiles = pair.Value.SampleFiles.ToArray()
                    })
                    .ToArray(),
                MostChangedFiles = mostChangedFiles
                    .OrderByDescending(file => file.DiagnosticCount)
                    .ThenBy(file => file.RelativePath, StringComparer.Ordinal)
                    .Take(TopFileLimit)
                    .ToArray()
            };
        }

        private static string NormalizeDiagnosticPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "(none)";
            }

            var normalized = Regex.Replace(path, "\\[@id='[^']*'\\]", "[@id='*']");
            normalized = Regex.Replace(normalized, "\\[(\\d+)\\]", "[*]");
            return normalized;
        }
        private static string GetLeafName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "(none)";
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return "(none)";
            }

            var leaf = segments[^1];
            if (leaf.StartsWith("@", StringComparison.Ordinal))
            {
                return leaf[1..];
            }

            var bracketIndex = leaf.IndexOf('[');
            return bracketIndex >= 0 ? leaf[..bracketIndex] : leaf;
        }

        private static BatchNamedCount[] ToNamedCounts(Dictionary<string, AggregateBucket> buckets)
            => buckets
                .OrderByDescending(pair => pair.Value.Count)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(TopBucketLimit)
                .Select(pair => new BatchNamedCount
                {
                    Name = pair.Key,
                    Count = pair.Value.Count,
                    FileCount = pair.Value.FileCount,
                    SampleFiles = pair.Value.SampleFiles.ToArray()
                })
                .ToArray();

        private static void AddBucket(
            Dictionary<string, AggregateBucket> buckets,
            string key,
            string relativePath,
            int increment = 1)
        {
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new AggregateBucket();
                buckets[key] = bucket;
            }

            bucket.Add(relativePath, increment);
        }

        private static void AddBucket(
            Dictionary<(string Code, string Path), AggregateBucket> buckets,
            (string Code, string Path) key,
            string relativePath,
            int increment = 1)
        {
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new AggregateBucket();
                buckets[key] = bucket;
            }

            bucket.Add(relativePath, increment);
        }

        private sealed class AggregateBucket
        {
            private readonly HashSet<string> seenFiles = new(StringComparer.Ordinal);
            private readonly List<string> sampleFiles = [];

            public int Count { get; private set; }

            public int FileCount => seenFiles.Count;

            public IReadOnlyList<string> SampleFiles => sampleFiles;

            public void Add(string relativePath, int increment)
            {
                Count += increment;
                if (seenFiles.Add(relativePath) && sampleFiles.Count < SampleLimit)
                {
                    sampleFiles.Add(relativePath);
                }
            }
        }
    }
}
