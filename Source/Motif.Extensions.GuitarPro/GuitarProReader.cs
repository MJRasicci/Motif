namespace Motif.Extensions.GuitarPro;

using Motif;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;

public sealed class GuitarProReader : IGuitarProReader
{
    private readonly IGpArchiveReader archiveReader;
    private readonly IGpifDeserializer deserializer;
    private readonly IScoreMapper mapper;

    public GuitarProReader()
        : this(new ZipGpArchiveReader(), new XmlGpifDeserializer(), new DefaultScoreMapper())
    {
    }

    internal GuitarProReader(IGpArchiveReader archiveReader, IGpifDeserializer deserializer, IScoreMapper mapper)
    {
        this.archiveReader = archiveReader;
        this.deserializer = deserializer;
        this.mapper = mapper;
    }

    public async ValueTask<Score> ReadAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        await using var scoreStream = await archiveReader.OpenScoreStreamAsync(source, cancellationToken).ConfigureAwait(false);
        return await ReadScoreAsync(scoreStream, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Score> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var scoreStream = await archiveReader.OpenScoreStreamAsync(filePath, cancellationToken).ConfigureAwait(false);
        return await ReadScoreAsync(scoreStream, cancellationToken).ConfigureAwait(false);
    }

    ValueTask<Score> IScoreReader.ReadAsync(Stream source, CancellationToken cancellationToken)
        => ReadAsync(source, cancellationToken);

    private async ValueTask<Score> ReadScoreAsync(Stream scoreStream, CancellationToken cancellationToken)
    {
        var raw = await deserializer.DeserializeAsync(scoreStream, cancellationToken).ConfigureAwait(false);
        return await mapper.MapAsync(raw, cancellationToken).ConfigureAwait(false);
    }
}
