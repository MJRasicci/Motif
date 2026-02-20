namespace GPIO.NET.Implementation;

using GPIO.NET.Abstractions;
using System.IO.Compression;

public sealed class ZipGpArchiveWriter : IGpArchiveWriter
{
    public async ValueTask WriteArchiveAsync(Stream gpifContent, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gpifContent);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("Content/score.gpif", CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        gpifContent.Position = 0;
        await gpifContent.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
    }
}
