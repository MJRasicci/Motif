namespace Motif;

using System.IO.Compression;
using Motif.Models;
using System.Text.Json;

internal sealed class MotifArchiveWriter : IScoreWriter
{
    public async ValueTask WriteAsync(Score score, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(destination);

        var contributors = ArchiveContributorRegistry.GetRegisteredContributors();
        var supplementalEntries = ArchiveContributorRegistry.PreserveArchiveEntries(
            score,
            contributors,
            out var manifestExtensionKeys);
        var manifestSources = MotifArchiveProvenance.GetManifestSources(score);

        using (var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteManifestAsync(archive, manifestSources, manifestExtensionKeys, cancellationToken).ConfigureAwait(false);
            await WriteScoreAsync(archive, score, cancellationToken).ConfigureAwait(false);
            await WriteSupplementalEntriesAsync(archive, supplementalEntries, cancellationToken).ConfigureAwait(false);
        }

        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteAsync(Score score, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var destination = File.Create(filePath);
        await WriteAsync(score, destination, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask WriteManifestAsync(
        ZipArchive archive,
        IReadOnlyList<MotifArchiveSource> manifestSources,
        IReadOnlyList<string> manifestExtensionKeys,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(MotifArchiveFormat.ManifestEntryName);
        var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        await JsonSerializer.SerializeAsync(
                stream,
                MotifArchiveFormat.CreateManifest(manifestSources, manifestExtensionKeys),
                MotifArchiveJsonContext.Default.MotifArchiveManifest,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async ValueTask WriteScoreAsync(
        ZipArchive archive,
        Score score,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(MotifArchiveFormat.ScoreEntryName);
        var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var _ = stream;
        await JsonSerializer.SerializeAsync(stream, score, MotifJsonContext.Default.Score, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async ValueTask WriteSupplementalEntriesAsync(
        ZipArchive archive,
        IReadOnlyList<ArchiveEntry> supplementalEntries,
        CancellationToken cancellationToken)
    {
        foreach (var supplementalEntry in supplementalEntries)
        {
            var entry = archive.CreateEntry(MotifArchivePaths.NormalizeEntryPath(supplementalEntry.EntryPath));
            var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var _ = stream;
            await stream.WriteAsync(supplementalEntry.Data, cancellationToken).ConfigureAwait(false);
        }
    }
}
