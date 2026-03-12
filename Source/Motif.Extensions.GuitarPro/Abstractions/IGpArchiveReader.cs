namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Extensions.GuitarPro.Models;

/// <summary>
/// Responsible for opening a .gp archive and exposing the GPIF score stream.
/// </summary>
internal interface IGpArchiveReader
{
    ValueTask<GpArchiveReadResult> ReadArchiveAsync(Stream archiveStream, CancellationToken cancellationToken = default);

    ValueTask<GpArchiveReadResult> ReadArchiveAsync(string filePath, CancellationToken cancellationToken = default);
}
