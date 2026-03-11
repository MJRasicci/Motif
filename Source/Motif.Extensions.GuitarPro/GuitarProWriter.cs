namespace Motif.Extensions.GuitarPro;

using Motif;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models.Write;
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

    internal GuitarProWriter(IScoreUnmapper unmapper, IGpifSerializer serializer, IGpArchiveWriter archiveWriter)
    {
        this.unmapper = unmapper;
        this.serializer = serializer;
        this.archiveWriter = archiveWriter;
    }

    public async ValueTask WriteAsync(Score score, string filePath, CancellationToken cancellationToken = default)
        => _ = await WriteWithDiagnosticsAsync(score, filePath, cancellationToken).ConfigureAwait(false);

    public async ValueTask<WriteDiagnostics> WriteWithDiagnosticsAsync(Score score, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var result = await unmapper.UnmapAsync(score, cancellationToken).ConfigureAwait(false);
        await using var buffer = await SerializeToGpifBufferAsync(result, cancellationToken).ConfigureAwait(false);
        await archiveWriter.WriteArchiveAsync(buffer, filePath, cancellationToken).ConfigureAwait(false);
        return result.Diagnostics;
    }

    public async ValueTask WriteAsync(Score score, Stream destination, CancellationToken cancellationToken = default)
        => _ = await WriteWithDiagnosticsAsync(score, destination, cancellationToken).ConfigureAwait(false);

    public async ValueTask<WriteDiagnostics> WriteWithDiagnosticsAsync(Score score, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var result = await unmapper.UnmapAsync(score, cancellationToken).ConfigureAwait(false);
        await using var buffer = await SerializeToGpifBufferAsync(result, cancellationToken).ConfigureAwait(false);
        await archiveWriter.WriteArchiveAsync(buffer, destination, cancellationToken).ConfigureAwait(false);
        return result.Diagnostics;
    }

    private async ValueTask<MemoryStream> SerializeToGpifBufferAsync(WriteResult result, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await serializer.SerializeAsync(result.RawDocument, buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }
}
