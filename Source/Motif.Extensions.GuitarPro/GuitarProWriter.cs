namespace Motif.Extensions.GuitarPro;

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

    public async ValueTask WriteAsync(GuitarProScore score, string filePath, CancellationToken cancellationToken = default)
    {
        var result = await unmapper.UnmapAsync(score, cancellationToken).ConfigureAwait(false);
        await using var buffer = new MemoryStream();
        await serializer.SerializeAsync(result.RawDocument, buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        await archiveWriter.WriteArchiveAsync(buffer, filePath, cancellationToken).ConfigureAwait(false);
    }
}

