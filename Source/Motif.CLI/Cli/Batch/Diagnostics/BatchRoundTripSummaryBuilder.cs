namespace Motif.CLI;

using Motif.Extensions.GuitarPro.Models.Write;
using System.Text.RegularExpressions;

internal sealed class BatchRoundTripSummaryBuilder
{
    private const int SampleLimit = 5;
    private const int TopBucketLimit = 20;
    private const int TopPathLimit = 50;
    private const int TopFileLimit = 25;

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
    private int filesWithWarnings;
    private int filesWithInfos;
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

        if (fileResult.WarningCount > 0)
        {
            filesWithWarnings++;
        }

        if (fileResult.InfoCount > 0)
        {
            filesWithInfos++;
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
            FilesWithWarnings = filesWithWarnings,
            FilesWithInfos = filesWithInfos,
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

    private static string GetTopLevelSection(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "(none)";
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : segments[0];
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
