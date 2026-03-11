namespace Motif.Extensions.GuitarPro.Abstractions;

internal interface IGpArchiveWriter
{
    ValueTask WriteArchiveAsync(Stream gpifContent, Stream destination, CancellationToken cancellationToken = default);

    ValueTask WriteArchiveAsync(Stream gpifContent, string filePath, CancellationToken cancellationToken = default);
}
