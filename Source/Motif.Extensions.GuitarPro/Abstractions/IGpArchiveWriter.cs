namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Extensions.GuitarPro.Models;

internal interface IGpArchiveWriter
{
    ValueTask WriteArchiveAsync(
        Stream gpifContent,
        Stream destination,
        CancellationToken cancellationToken = default,
        IReadOnlyList<GpArchiveResourceEntry>? resourceEntries = null);

    ValueTask WriteArchiveAsync(
        Stream gpifContent,
        string filePath,
        CancellationToken cancellationToken = default,
        IReadOnlyList<GpArchiveResourceEntry>? resourceEntries = null);
}
