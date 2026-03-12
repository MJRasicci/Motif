namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Models;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

public class MotifScoreTests
{
    [Fact]
    public async Task MotifScore_can_round_trip_json_without_any_extension_package()
    {
        var score = CreateScore("Json Native");
        var tempDirectory = CreateTempDirectory();
        var filePath = Path.Combine(tempDirectory, "score.json");

        try
        {
            await MotifScore.SaveAsync(score, filePath, TestContext.Current.CancellationToken);
            var readBack = await MotifScore.OpenAsync(filePath, TestContext.Current.CancellationToken);

            readBack.Title.Should().Be("Json Native");
            readBack.Tracks.Should().ContainSingle();
            ScoreNavigation.HasCurrentPlaybackSequence(readBack).Should().BeTrue();
            MotifScore.CanOpen(filePath).Should().BeTrue();
            MotifScore.GetRegisteredFormats().Should().Contain(handler => handler.SupportedExtensions.Contains(".json"));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MotifScore_can_round_trip_motif_archive_without_any_extension_package()
    {
        var score = CreateScore("Motif Archive");
        var tempDirectory = CreateTempDirectory();
        var filePath = Path.Combine(tempDirectory, "score.motif");

        try
        {
            await MotifScore.SaveAsync(score, filePath, TestContext.Current.CancellationToken);
            var readBack = await MotifScore.OpenAsync(filePath, TestContext.Current.CancellationToken);

            readBack.Title.Should().Be("Motif Archive");
            readBack.Tracks.Should().ContainSingle();
            ScoreNavigation.HasCurrentPlaybackSequence(readBack).Should().BeTrue();
            MotifScore.CanOpen(filePath).Should().BeTrue();
            MotifScore.GetRegisteredFormats().Should().Contain(handler => handler.SupportedExtensions.Contains(".motif"));

            await using var stream = new MemoryStream();
            await MotifScore.SaveAsync(score, stream, "motif", TestContext.Current.CancellationToken);
            stream.Position = 0;

            var streamRead = await MotifScore.OpenAsync(stream, ".motif", TestContext.Current.CancellationToken);
            streamRead.Title.Should().Be("Motif Archive");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Explicit_archive_contributor_registration_round_trips_contributed_entries()
    {
        using var registration = MotifScore.RegisterArchiveContributor(new TestArchiveContributor());

        var score = CreateScore("Contributed Archive");
        score.SetExtension(new TestArchivePayloadExtension
        {
            Payload = "contributed-value"
        });

        var tempDirectory = CreateTempDirectory();
        var filePath = Path.Combine(tempDirectory, "score.motif");

        try
        {
            await MotifScore.SaveAsync(score, filePath, TestContext.Current.CancellationToken);

            using (var archive = ZipFile.OpenRead(filePath))
            {
                archive.GetEntry("extensions/test.json").Should().NotBeNull();
                archive.GetEntry("resources/test/payload.txt").Should().NotBeNull();

                using var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(archive, "manifest.json"));
                manifest.RootElement.GetProperty("extensions").EnumerateArray()
                    .Select(element => element.GetString())
                    .Should().Contain("test");
            }

            var readBack = await MotifScore.OpenAsync(filePath, TestContext.Current.CancellationToken);
            readBack.GetExtension<TestArchivePayloadExtension>().Should().NotBeNull();
            readBack.GetExtension<TestArchivePayloadExtension>()!.Payload.Should().Be("contributed-value");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Motif_archive_rewrite_preserves_unknown_supplemental_entries_without_registered_contributors()
    {
        var tempDirectory = CreateTempDirectory();
        var inputPath = Path.Combine(tempDirectory, "input.motif");
        var outputPath = Path.Combine(tempDirectory, "output.motif");

        try
        {
            await CreateMotifArchiveWithSupplementalEntriesAsync(
                inputPath,
                CreateScore("Unknown Entries"),
                extensions: ["unknown"],
                supplementalEntries:
                [
                    new ArchiveEntry("resources/unknown/data.bin", new byte[] { 1, 2, 3, 4 }),
                    new ArchiveEntry("extensions/unknown.json", Encoding.UTF8.GetBytes("""{"state":"kept"}"""))
                ]);

            var readBack = await MotifScore.OpenAsync(inputPath, TestContext.Current.CancellationToken);
            await MotifScore.SaveAsync(readBack, outputPath, TestContext.Current.CancellationToken);

            using var archive = ZipFile.OpenRead(outputPath);
            archive.GetEntry("resources/unknown/data.bin").Should().NotBeNull();
            archive.GetEntry("extensions/unknown.json").Should().NotBeNull();

            using var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(archive, "manifest.json"));
            manifest.RootElement.GetProperty("extensions").EnumerateArray()
                .Select(element => element.GetString())
                .Should().Contain("unknown");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Motif_archive_rewrite_preserves_manifest_sources_without_registered_contributors()
    {
        var tempDirectory = CreateTempDirectory();
        var inputPath = Path.Combine(tempDirectory, "input.motif");
        var outputPath = Path.Combine(tempDirectory, "output.motif");
        const string importedAt = "2026-03-12T00:00:00.0000000+00:00";

        try
        {
            await CreateMotifArchiveWithSupplementalEntriesAsync(
                inputPath,
                CreateScore("Preserved Sources"),
                extensions: [],
                supplementalEntries: [],
                sources:
                [
                    (".gp", "source-input", importedAt)
                ]);

            var readBack = await MotifScore.OpenAsync(inputPath, TestContext.Current.CancellationToken);
            await MotifScore.SaveAsync(readBack, outputPath, TestContext.Current.CancellationToken);

            using var archive = ZipFile.OpenRead(outputPath);
            using var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(archive, "manifest.json"));
            var sources = manifest.RootElement.GetProperty("sources").EnumerateArray().ToArray();

            sources.Should().ContainSingle();
            sources[0].GetProperty("format").GetString().Should().Be(".gp");
            sources[0].GetProperty("fileName").GetString().Should().Be("source-input");
            sources[0].GetProperty("importedAt").GetString().Should().Be(importedAt);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Archive_contributors_must_write_inside_their_own_namespace()
    {
        using var registration = MotifScore.RegisterArchiveContributor(new InvalidArchiveContributor());
        var score = CreateScore("Invalid Contributor");

        var action = async () =>
        {
            await using var stream = new MemoryStream();
            await MotifScore.SaveAsync(score, stream, ".motif", TestContext.Current.CancellationToken);
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        exception.Message.Should().Contain("invalid");
        exception.Message.Should().Contain("resources/other");
    }

    [Fact]
    public async Task Explicit_handler_registration_routes_path_and_stream_operations()
    {
        var handler = new RecordingFormatHandler();
        using var registration = MotifScore.RegisterHandler(handler);

        var score = CreateScore("Custom Format");
        var tempDirectory = CreateTempDirectory();
        var filePath = Path.Combine(tempDirectory, "score.fake");

        try
        {
            MotifScore.CreateReader(".fake").Should().BeSameAs(handler.Reader);
            MotifScore.CreateWriter("fake").Should().BeSameAs(handler.Writer);

            await MotifScore.SaveAsync(score, filePath, TestContext.Current.CancellationToken);
            var pathRead = await MotifScore.OpenAsync(filePath, TestContext.Current.CancellationToken);

            pathRead.Title.Should().Be("Custom Format");
            handler.Writer.PathWrites.Should().Be(1);
            handler.Reader.PathReads.Should().Be(1);
            MotifScore.CanOpen(filePath).Should().BeTrue();

            await using var stream = new MemoryStream();
            await MotifScore.SaveAsync(score, stream, ".fake", TestContext.Current.CancellationToken);
            stream.Position = 0;

            var streamRead = await MotifScore.OpenAsync(stream, "fake", TestContext.Current.CancellationToken);
            streamRead.Title.Should().Be("Custom Format");
            handler.Writer.StreamWrites.Should().Be(1);
            handler.Reader.StreamReads.Should().Be(1);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Explicit_format_path_open_populates_manifest_sources_for_extensionless_input()
    {
        var handler = new RecordingFormatHandler();
        using var registration = MotifScore.RegisterHandler(handler);

        var tempDirectory = CreateTempDirectory();
        var inputPath = Path.Combine(tempDirectory, "score");
        var motifPath = Path.Combine(tempDirectory, "score.motif");

        try
        {
            await File.WriteAllTextAsync(inputPath, "Extensionless Import", TestContext.Current.CancellationToken);

            var score = await MotifScore.OpenAsync(inputPath, "fake", TestContext.Current.CancellationToken);
            score.Title.Should().Be("Extensionless Import");
            handler.Reader.PathReads.Should().Be(1);
            handler.Reader.StreamReads.Should().Be(0);

            await MotifScore.SaveAsync(score, motifPath, TestContext.Current.CancellationToken);

            using var archive = ZipFile.OpenRead(motifPath);
            using var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(archive, "manifest.json"));
            var sources = manifest.RootElement.GetProperty("sources").EnumerateArray().ToArray();

            sources.Should().ContainSingle();
            sources[0].GetProperty("format").GetString().Should().Be(".fake");
            sources[0].GetProperty("fileName").GetString().Should().Be("score");
            DateTimeOffset.TryParse(sources[0].GetProperty("importedAt").GetString(), out _).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Opening_a_motif_archive_with_a_future_manifest_version_is_rejected()
    {
        await using var archiveStream = new MemoryStream();
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifestEntry = archive.CreateEntry("manifest.json");
            await using (var manifestStream = manifestEntry.Open())
            await using (var writer = new StreamWriter(manifestStream, Encoding.UTF8, leaveOpen: true))
            {
                await writer.WriteAsync("""
                    {
                      "formatVersion": "99.0",
                      "createdBy": "test",
                      "sources": [],
                      "extensions": []
                    }
                    """);
            }

            var scoreEntry = archive.CreateEntry("score.json");
            await using (var scoreStream = scoreEntry.Open())
            {
                var bytes = Encoding.UTF8.GetBytes(CreateScore("Future Manifest").ToJson());
                await scoreStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
            }
        }

        archiveStream.Position = 0;

        var action = async () => await MotifScore.OpenAsync(archiveStream, ".motif", TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<NotSupportedException>(action);
        exception.Message.Should().Contain("99.0");
        exception.Message.Should().Contain("1.0");
    }

    [Fact]
    public async Task Opening_unknown_format_throws_a_helpful_error()
    {
        var action = async () => await MotifScore.OpenAsync("song.xyz", TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        exception.Message.Should().Contain(".xyz");
        exception.Message.Should().Contain("RegisterHandler");
    }

    [Fact]
    public void Creating_unknown_writer_throws_a_helpful_error()
    {
        var action = () => MotifScore.CreateWriter("song.xyz");

        var exception = Assert.Throws<InvalidOperationException>(action);
        exception.Message.Should().Contain(".xyz");
        exception.Message.Should().Contain("RegisterHandler");
    }

    private static Score CreateScore(string title)
    {
        var score = new Score
        {
            Title = title,
            TimelineBars =
            [
                new TimelineBar
                {
                    Index = 0,
                    TimeSignature = "4/4"
                }
            ],
            Tracks =
            [
                new Track
                {
                    Id = 1,
                    Name = "Lead",
                    Staves =
                    [
                        new Staff
                        {
                            StaffIndex = 0,
                            Measures =
                            [
                                new StaffMeasure
                                {
                                    Index = 0,
                                    StaffIndex = 0,
                                    Voices =
                                    [
                                        new Voice
                                        {
                                            VoiceIndex = 0,
                                            Beats =
                                            [
                                                new Beat
                                                {
                                                    Id = 1,
                                                    Offset = 0m,
                                                    Duration = 0.25m,
                                                    Notes =
                                                    [
                                                        new Note
                                                        {
                                                            Id = 1,
                                                            MidiPitch = 64,
                                                            Duration = 0.25m
                                                        }
                                                    ],
                                                    MidiPitches = [64]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        ScoreNavigation.RebuildPlaybackSequence(score);
        return score;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "motif-core-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<string> ReadArchiveEntryTextAsync(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private static async Task CreateMotifArchiveWithSupplementalEntriesAsync(
        string filePath,
        Score score,
        IReadOnlyList<string> extensions,
        IReadOnlyList<ArchiveEntry> supplementalEntries,
        IReadOnlyList<(string Format, string FileName, string ImportedAt)>? sources = null)
    {
        await using var destination = File.Create(filePath);
        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

        var manifestEntry = archive.CreateEntry("manifest.json");
        await using (var manifestStream = manifestEntry.Open())
        {
            await JsonSerializer.SerializeAsync(
                manifestStream,
                new
                {
                    formatVersion = "1.0",
                    createdBy = "Motif.Core",
                    sources = (sources ?? [])
                        .Select(source => new
                        {
                            format = source.Format,
                            fileName = source.FileName,
                            importedAt = source.ImportedAt
                        }),
                    extensions
                },
                options: (JsonSerializerOptions?)null,
                TestContext.Current.CancellationToken);
        }

        var scoreEntry = archive.CreateEntry("score.json");
        await using (var scoreStream = scoreEntry.Open())
        {
            var bytes = Encoding.UTF8.GetBytes(score.ToJson());
            await scoreStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
        }

        foreach (var supplementalEntry in supplementalEntries)
        {
            var entry = archive.CreateEntry(supplementalEntry.EntryPath);
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(supplementalEntry.Data, TestContext.Current.CancellationToken);
        }
    }

    private sealed class RecordingFormatHandler : IFormatHandler
    {
        public RecordingFormatHandler()
        {
            Reader = new RecordingReader();
            Writer = new RecordingWriter();
        }

        public RecordingReader Reader { get; }

        public RecordingWriter Writer { get; }

        public IReadOnlyList<string> SupportedExtensions { get; } = [".fake"];

        public string FormatName => "Fake";

        public IScoreReader CreateReader() => Reader;

        public IScoreWriter CreateWriter() => Writer;
    }

    private sealed class RecordingReader : IScoreReader
    {
        public int PathReads { get; private set; }

        public int StreamReads { get; private set; }

        public async ValueTask<Score> ReadAsync(Stream source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            StreamReads++;
            using var reader = new StreamReader(source, Encoding.UTF8, leaveOpen: true);
            var title = await reader.ReadToEndAsync(cancellationToken);
            return CreateScore(title);
        }

        public async ValueTask<Score> ReadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            PathReads++;
            var title = await File.ReadAllTextAsync(filePath, cancellationToken);
            return CreateScore(title);
        }
    }

    private sealed class RecordingWriter : IScoreWriter
    {
        public int PathWrites { get; private set; }

        public int StreamWrites { get; private set; }

        public async ValueTask WriteAsync(Score score, Stream destination, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(score);
            ArgumentNullException.ThrowIfNull(destination);

            StreamWrites++;
            var bytes = Encoding.UTF8.GetBytes(score.Title ?? string.Empty);
            await destination.WriteAsync(bytes, cancellationToken);
            await destination.FlushAsync(cancellationToken);
        }

        public async ValueTask WriteAsync(Score score, string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(score);

            PathWrites++;
            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, score.Title ?? string.Empty, cancellationToken);
        }
    }

    private sealed class TestArchivePayloadExtension : IModelExtension
    {
        public string Payload { get; init; } = string.Empty;
    }

    private sealed class TestArchiveContributor : IArchiveContributor
    {
        public string ContributorKey => "test";

        public IReadOnlyList<ArchiveEntry> GetArchiveEntries(Score score)
        {
            ArgumentNullException.ThrowIfNull(score);

            var payload = score.GetExtension<TestArchivePayloadExtension>()?.Payload;
            if (string.IsNullOrWhiteSpace(payload))
            {
                return [];
            }

            return
            [
                new ArchiveEntry("extensions/test.json", Encoding.UTF8.GetBytes($$"""{"payload":"{{payload}}"}""")),
                new ArchiveEntry("resources/test/payload.txt", Encoding.UTF8.GetBytes(payload))
            ];
        }

        public void RestoreFromArchive(Score score, IReadOnlyList<ArchiveEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(score);
            ArgumentNullException.ThrowIfNull(entries);

            var payloadEntry = entries.FirstOrDefault(entry =>
                string.Equals(entry.EntryPath, "resources/test/payload.txt", StringComparison.OrdinalIgnoreCase));
            if (payloadEntry is null)
            {
                return;
            }

            score.SetExtension(new TestArchivePayloadExtension
            {
                Payload = Encoding.UTF8.GetString(payloadEntry.Data.Span)
            });
        }
    }

    private sealed class InvalidArchiveContributor : IArchiveContributor
    {
        public string ContributorKey => "invalid";

        public IReadOnlyList<ArchiveEntry> GetArchiveEntries(Score score)
        {
            ArgumentNullException.ThrowIfNull(score);
            return [new ArchiveEntry("resources/other/file.bin", new byte[] { 1, 2, 3 })];
        }

        public void RestoreFromArchive(Score score, IReadOnlyList<ArchiveEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(score);
            ArgumentNullException.ThrowIfNull(entries);
        }
    }
}
