namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Models;
using System.IO.Compression;

internal sealed class ZipGpArchiveWriter : IGpArchiveWriter
{
    private const string ScoreEntryPath = "Content/score.gpif";
    private const string DefaultTemplateResourceName = "Motif.Extensions.GuitarPro.Resources.DefaultTemplate.gp";
    private static readonly byte[]? DefaultTemplateArchiveBytes = LoadDefaultTemplateArchiveBytes();

    public async ValueTask WriteArchiveAsync(
        Stream gpifContent,
        Stream destination,
        CancellationToken cancellationToken = default,
        IReadOnlyList<GpArchiveResourceEntry>? resourceEntries = null)
    {
        ValidateStreams(gpifContent, destination);
        var normalizedResourceEntries = NormalizeResourceEntries(resourceEntries);

        if (destination.CanSeek)
        {
            destination.Position = 0;
            destination.SetLength(0);
        }

        if (normalizedResourceEntries.Count > 0)
        {
            await WriteArchiveFromResourceEntriesAsync(destination, gpifContent, normalizedResourceEntries, cancellationToken).ConfigureAwait(false);
        }
        else if (DefaultTemplateArchiveBytes is { Length: > 0 })
        {
            await using var templateArchive = new MemoryStream(DefaultTemplateArchiveBytes, writable: false);
            await RewriteArchiveFromTemplateAsync(templateArchive, destination, gpifContent, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
            await CreateScoreEntryAsync(archive, gpifContent, cancellationToken).ConfigureAwait(false);
        }

        if (destination.CanSeek)
        {
            destination.Position = 0;
        }
    }

    public async ValueTask WriteArchiveAsync(
        Stream gpifContent,
        string filePath,
        CancellationToken cancellationToken = default,
        IReadOnlyList<GpArchiveResourceEntry>? resourceEntries = null)
    {
        ArgumentNullException.ThrowIfNull(gpifContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var normalizedResourceEntries = NormalizeResourceEntries(resourceEntries);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (normalizedResourceEntries.Count > 0)
        {
            await WriteArchiveFromResourceEntriesAsync(filePath, gpifContent, normalizedResourceEntries, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (File.Exists(filePath))
        {
            var tempPath = filePath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                await RewriteArchiveFromTemplateAsync(filePath, tempPath, gpifContent, cancellationToken).ConfigureAwait(false);

                // Replace in a single move so we never leave a partially written archive at the destination path.
                File.Move(tempPath, filePath, overwrite: true);
                return;
            }
            catch (InvalidDataException)
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                // Existing output is not a valid zip archive; replace it with a fresh archive.
                File.Delete(filePath);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }

        if (DefaultTemplateArchiveBytes is { Length: > 0 })
        {
            await RewriteArchiveFromTemplateBytesAsync(DefaultTemplateArchiveBytes, filePath, gpifContent, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
        await CreateScoreEntryAsync(archive, gpifContent, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask WriteArchiveFromResourceEntriesAsync(
        string filePath,
        Stream gpifContent,
        IReadOnlyList<GpArchiveResourceEntry> resourceEntries,
        CancellationToken cancellationToken)
    {
        var tempPath = filePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var targetArchive = File.Create(tempPath))
            {
                await WriteArchiveFromResourceEntriesAsync(targetArchive, gpifContent, resourceEntries, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static async ValueTask WriteArchiveFromResourceEntriesAsync(
        Stream targetArchive,
        Stream gpifContent,
        IReadOnlyList<GpArchiveResourceEntry> resourceEntries,
        CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(targetArchive, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var resourceEntry in resourceEntries)
        {
            var targetEntry = archive.CreateEntry(resourceEntry.EntryPath, CompressionLevel.Optimal);
            await using var stream = await targetEntry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(resourceEntry.Data, cancellationToken).ConfigureAwait(false);
        }

        await CreateScoreEntryAsync(archive, gpifContent, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask RewriteArchiveFromTemplateBytesAsync(byte[] templateArchiveBytes, string targetArchivePath, Stream gpifContent, CancellationToken cancellationToken)
    {
        await using var templateArchive = new MemoryStream(templateArchiveBytes, writable: false);
        await RewriteArchiveFromTemplateAsync(templateArchive, targetArchivePath, gpifContent, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask RewriteArchiveFromTemplateAsync(string templateArchivePath, string targetArchivePath, Stream gpifContent, CancellationToken cancellationToken)
    {
        await using var templateArchive = File.OpenRead(templateArchivePath);
        await RewriteArchiveFromTemplateAsync(templateArchive, targetArchivePath, gpifContent, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask RewriteArchiveFromTemplateAsync(Stream templateArchive, Stream targetArchive, Stream gpifContent, CancellationToken cancellationToken)
    {
        using var source = new ZipArchive(templateArchive, ZipArchiveMode.Read, leaveOpen: false);
        using var target = new ZipArchive(targetArchive, ZipArchiveMode.Create, leaveOpen: true);

        var wroteScoreEntry = false;
        foreach (var sourceEntry in source.Entries)
        {
            var targetEntry = target.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
            await using var outStream = await targetEntry.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (string.Equals(sourceEntry.FullName, ScoreEntryPath, StringComparison.OrdinalIgnoreCase))
            {
                if (gpifContent.CanSeek)
                {
                    gpifContent.Position = 0;
                }

                await gpifContent.CopyToAsync(outStream, cancellationToken).ConfigureAwait(false);
                wroteScoreEntry = true;
                continue;
            }

            await using var inStream = await sourceEntry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await inStream.CopyToAsync(outStream, cancellationToken).ConfigureAwait(false);
        }

        if (!wroteScoreEntry)
        {
            await CreateScoreEntryAsync(target, gpifContent, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask RewriteArchiveFromTemplateAsync(Stream templateArchive, string targetArchivePath, Stream gpifContent, CancellationToken cancellationToken)
    {
        using var source = new ZipArchive(templateArchive, ZipArchiveMode.Read, leaveOpen: false);
        using var target = ZipFile.Open(targetArchivePath, ZipArchiveMode.Create);

        var wroteScoreEntry = false;
        foreach (var sourceEntry in source.Entries)
        {
            var targetEntry = target.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
            await using var outStream = await targetEntry.OpenAsync(cancellationToken).ConfigureAwait(false);
            if (string.Equals(sourceEntry.FullName, ScoreEntryPath, StringComparison.OrdinalIgnoreCase))
            {
                if (gpifContent.CanSeek)
                {
                    gpifContent.Position = 0;
                }

                await gpifContent.CopyToAsync(outStream, cancellationToken).ConfigureAwait(false);
                wroteScoreEntry = true;
                continue;
            }

            await using var inStream = await sourceEntry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await inStream.CopyToAsync(outStream, cancellationToken).ConfigureAwait(false);
        }

        if (!wroteScoreEntry)
        {
            await CreateScoreEntryAsync(target, gpifContent, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateStreams(Stream gpifContent, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(gpifContent);
        ArgumentNullException.ThrowIfNull(destination);

        if (!gpifContent.CanRead)
        {
            throw new ArgumentException("GPIF content stream must be readable.", nameof(gpifContent));
        }

        if (!destination.CanWrite)
        {
            throw new ArgumentException("Destination stream must be writable.", nameof(destination));
        }
    }

    private static IReadOnlyList<GpArchiveResourceEntry> NormalizeResourceEntries(IReadOnlyList<GpArchiveResourceEntry>? resourceEntries)
    {
        if (resourceEntries is null || resourceEntries.Count == 0)
        {
            return [];
        }

        var unique = new Dictionary<string, GpArchiveResourceEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var resourceEntry in resourceEntries)
        {
            ArgumentNullException.ThrowIfNull(resourceEntry);

            var normalizedPath = resourceEntry.EntryPath.Trim().Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                throw new InvalidDataException("Guitar Pro archive resource entries must have a non-empty path.");
            }

            if (string.Equals(normalizedPath, ScoreEntryPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Guitar Pro archive resource entries must not include Content/score.gpif.");
            }

            unique[normalizedPath] = new GpArchiveResourceEntry(normalizedPath, resourceEntry.Data);
        }

        return unique.Values
            .OrderBy(entry => entry.EntryPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static byte[]? LoadDefaultTemplateArchiveBytes()
    {
        using var stream = typeof(ZipGpArchiveWriter).Assembly.GetManifestResourceStream(DefaultTemplateResourceName);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static async ValueTask CreateScoreEntryAsync(ZipArchive archive, Stream gpifContent, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(ScoreEntryPath, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        if (gpifContent.CanSeek)
        {
            gpifContent.Position = 0;
        }

        await gpifContent.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
    }
}
