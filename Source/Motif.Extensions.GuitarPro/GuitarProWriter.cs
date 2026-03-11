namespace Motif.Extensions.GuitarPro;

using Motif;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;

public sealed class GuitarProWriter : IGuitarProWriter
{
    private readonly IScoreUnmapper unmapper;
    private readonly IGpifSerializer serializer;
    private readonly IGpArchiveWriter archiveWriter;

    public GuitarProWriter()
        : this(new DefaultScoreUnmapper(), new XmlGpifSerializer(), new ZipGpArchiveWriter())
    {
    }

    public GuitarProWriter(IScoreUnmapper unmapper, IGpifSerializer serializer, IGpArchiveWriter archiveWriter)
    {
        this.unmapper = unmapper;
        this.serializer = serializer;
        this.archiveWriter = archiveWriter;
    }

    public async ValueTask WriteAsync(Score score, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await using var buffer = await SerializeToGpifBufferAsync(score, cancellationToken).ConfigureAwait(false);
        await archiveWriter.WriteArchiveAsync(buffer, filePath, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteAsync(Score score, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        await using var buffer = await SerializeToGpifBufferAsync(score, cancellationToken).ConfigureAwait(false);
        await archiveWriter.WriteArchiveAsync(buffer, destination, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<MemoryStream> SerializeToGpifBufferAsync(Score score, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(score);

        var result = await unmapper.UnmapAsync(score, cancellationToken).ConfigureAwait(false);
        var buffer = new MemoryStream();
        await serializer.SerializeAsync(result.RawDocument, buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }
}
