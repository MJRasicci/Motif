namespace Motif.Extensions.GuitarPro.Abstractions;

/// <summary>
/// Responsible for opening a .gp archive and exposing the GPIF score stream.
/// </summary>
internal interface IGpArchiveReader
{
    ValueTask<Stream> OpenScoreStreamAsync(Stream archiveStream, CancellationToken cancellationToken = default);

    ValueTask<Stream> OpenScoreStreamAsync(string filePath, CancellationToken cancellationToken = default);
}
