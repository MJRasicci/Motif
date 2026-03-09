namespace Motif.Extensions.GuitarPro.Implementation;

using Motif.Extensions.GuitarPro.Abstractions;
using System.IO.Compression;

public sealed class ZipGpArchiveWriter : IGpArchiveWriter
{
    private const string ScoreEntryPath = "Content/score.gpif";
    private const string DefaultTemplateResourceName = "Motif.Extensions.GuitarPro.Resources.DefaultTemplate.gp";
    private static readonly byte[]? DefaultTemplateArchiveBytes = LoadDefaultTemplateArchiveBytes();

    public async ValueTask WriteArchiveAsync(Stream gpifContent, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gpifContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
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
