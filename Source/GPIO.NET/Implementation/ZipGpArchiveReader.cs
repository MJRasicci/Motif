namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using System.IO.Compression;

public sealed class ZipGpArchiveReader : IGpArchiveReader
{
    public async ValueTask<Stream> OpenScoreStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Guitar Pro file not found: {filePath}", filePath);
        }

        await using var archive = await ZipFile.OpenReadAsync(filePath, cancellationToken).ConfigureAwait(false);
        var entry = archive.GetEntry("Content/score.gpif")
            ?? throw new InvalidDataException("Archive does not contain Content/score.gpif");

        await using var entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new MemoryStream();
        await entryStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        return buffer;
    }
}
