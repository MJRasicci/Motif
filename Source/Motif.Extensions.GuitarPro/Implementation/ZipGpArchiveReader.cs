namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Abstractions;
using System.IO.Compression;

public sealed class ZipGpArchiveReader : IGpArchiveReader
{
    public async ValueTask<Stream> OpenScoreStreamAsync(Stream archiveStream, CancellationToken cancellationToken = default)
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
        var entry = archive.GetEntry("Content/score.gpif")
            ?? throw new InvalidDataException("Archive does not contain Content/score.gpif");

        await using var entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new MemoryStream();
        await entryStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        return buffer;
    }

    public async ValueTask<Stream> OpenScoreStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Guitar Pro file not found: {filePath}", filePath);
        }

        await using var archiveStream = File.OpenRead(filePath);
        return await OpenScoreStreamAsync(archiveStream, cancellationToken).ConfigureAwait(false);
    }
}
