namespace Motif.Extensions.GuitarPro;

using Motif;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
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

        var archive = await archiveReader.ReadArchiveAsync(source, cancellationToken).ConfigureAwait(false);
        await using var scoreStream = archive.ScoreStream;
        var score = await ReadScoreAsync(scoreStream, cancellationToken).ConfigureAwait(false);
        AttachArchiveResources(score, archive.ResourceEntries);
        return score;
    }

    public async ValueTask<Score> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var archive = await archiveReader.ReadArchiveAsync(filePath, cancellationToken).ConfigureAwait(false);
        await using var scoreStream = archive.ScoreStream;
        var score = await ReadScoreAsync(scoreStream, cancellationToken).ConfigureAwait(false);
        AttachArchiveResources(score, archive.ResourceEntries);
        return score;
    }

    ValueTask<Score> IScoreReader.ReadAsync(Stream source, CancellationToken cancellationToken)
        => ReadAsync(source, cancellationToken);

    private async ValueTask<Score> ReadScoreAsync(Stream scoreStream, CancellationToken cancellationToken)
    {
        var raw = await deserializer.DeserializeAsync(scoreStream, cancellationToken).ConfigureAwait(false);
        return await mapper.MapAsync(raw, cancellationToken).ConfigureAwait(false);
    }

    private static void AttachArchiveResources(Score score, IReadOnlyList<GpArchiveResourceEntry> resourceEntries)
    {
        if (resourceEntries.Count == 0)
        {
            return;
        }

        score.SetExtension(new GpArchiveResourcesExtension
        {
            Entries = resourceEntries
                .Select(entry => new GpArchiveResourceEntry(entry.EntryPath, entry.Data))
                .ToArray()
        });
    }
}
