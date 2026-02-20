namespace GPIO.NET;

using GPIO.NET.Abstractions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;

public sealed class GuitarProReader : IGuitarProReader
{
    private readonly IGpArchiveReader archiveReader;
    private readonly IGpifDeserializer deserializer;
    private readonly IScoreMapper mapper;

    public GuitarProReader()
        : this(new ZipGpArchiveReader(), new XmlGpifDeserializer(), new DefaultScoreMapper())
    {
    }

    public GuitarProReader(IGpArchiveReader archiveReader, IGpifDeserializer deserializer, IScoreMapper mapper)
    {
        this.archiveReader = archiveReader;
        this.deserializer = deserializer;
        this.mapper = mapper;
    }

    public async ValueTask<GuitarProScore> ReadAsync(string filePath, GpReadOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new GpReadOptions();

        await using var scoreStream = await archiveReader.OpenScoreStreamAsync(filePath, cancellationToken).ConfigureAwait(false);
        var raw = await deserializer.DeserializeAsync(scoreStream, cancellationToken).ConfigureAwait(false);
        return await mapper.MapAsync(raw, cancellationToken).ConfigureAwait(false);
    }
}
