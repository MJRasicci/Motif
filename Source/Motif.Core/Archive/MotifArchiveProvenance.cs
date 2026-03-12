namespace Motif;

using Motif.Models;
using System.Globalization;

internal static class MotifArchiveProvenance
{
    public static void AttachImportedSource(Score score, string formatHint, string? sourceFileName)
    {
        ArgumentNullException.ThrowIfNull(score);

        var normalizedFormat = FormatHandlerRegistry.NormalizeFormatHint(formatHint);
        if (string.Equals(normalizedFormat, FormatHandlerRegistry.NormalizeFormatHint(MotifArchiveFormat.Extension), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var currentState = score.GetExtension<MotifArchivePreservedStateExtension>();
        score.SetExtension(new MotifArchivePreservedStateExtension
        {
            PreservedEntries = CloneEntries(currentState?.PreservedEntries),
            ManifestExtensions = CloneExtensions(currentState?.ManifestExtensions),
            ManifestSources =
            [
                new MotifArchiveSource
                {
                    Format = normalizedFormat,
                    FileName = Path.GetFileName(sourceFileName ?? string.Empty),
                    ImportedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                }
            ]
        });
    }

    public static IReadOnlyList<MotifArchiveSource> GetManifestSources(Score score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return CloneSources(score.GetExtension<MotifArchivePreservedStateExtension>()?.ManifestSources);
    }

    public static void RestoreManifestSources(Score score, IReadOnlyList<MotifArchiveSource> sources)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(sources);

        if (sources.Count == 0)
        {
            return;
        }

        var currentState = score.GetExtension<MotifArchivePreservedStateExtension>();
        score.SetExtension(new MotifArchivePreservedStateExtension
        {
            PreservedEntries = CloneEntries(currentState?.PreservedEntries),
            ManifestExtensions = CloneExtensions(currentState?.ManifestExtensions),
            ManifestSources = CloneSources(sources)
        });
    }

    private static IReadOnlyList<ArchiveEntry> CloneEntries(IReadOnlyList<ArchiveEntry>? entries)
        => entries?
            .Select(entry => new ArchiveEntry(entry.EntryPath, entry.Data))
            .ToArray()
            ?? [];

    private static IReadOnlyList<string> CloneExtensions(IReadOnlyList<string>? extensions)
        => extensions?.ToArray() ?? [];

    private static IReadOnlyList<MotifArchiveSource> CloneSources(IReadOnlyList<MotifArchiveSource>? sources)
        => sources?
            .Select(source => new MotifArchiveSource
            {
                Format = source.Format,
                FileName = source.FileName,
                ImportedAt = source.ImportedAt
            })
            .ToArray()
            ?? [];
}
