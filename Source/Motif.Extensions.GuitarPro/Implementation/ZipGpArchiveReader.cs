namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Models;
using System.IO.Compression;

internal sealed class ZipGpArchiveReader : IGpArchiveReader
{
    private const string ScoreEntryPath = "Content/score.gpif";

    public async ValueTask<GpArchiveReadResult> ReadArchiveAsync(Stream archiveStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);
        cancellationToken.ThrowIfCancellationRequested();

        if (!archiveStream.CanRead)
        {
            throw new ArgumentException("Archive stream must be readable.", nameof(archiveStream));
        }

        if (archiveStream.CanSeek)
        {
            archiveStream.Position = 0;
        }

        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry(ScoreEntryPath)
            ?? throw new InvalidDataException("Archive does not contain Content/score.gpif");

        await using var entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new MemoryStream();
        await entryStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        return new GpArchiveReadResult
        {
            ScoreStream = buffer,
            ResourceEntries = await ReadResourceEntriesAsync(archive, cancellationToken).ConfigureAwait(false)
        };
    }

    public async ValueTask<GpArchiveReadResult> ReadArchiveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Guitar Pro file not found: {filePath}", filePath);
        }

        await using var archiveStream = File.OpenRead(filePath);
        return await ReadArchiveAsync(archiveStream, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<IReadOnlyList<GpArchiveResourceEntry>> ReadResourceEntriesAsync(
        ZipArchive archive,
        CancellationToken cancellationToken)
    {
        var entries = new List<GpArchiveResourceEntry>();
        foreach (var entry in archive.Entries)
        {
            if (string.Equals(entry.FullName, ScoreEntryPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await using var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            entries.Add(new GpArchiveResourceEntry(entry.FullName, buffer.ToArray()));
        }

        return entries;
    }
}
