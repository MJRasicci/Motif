namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;
using System.IO.Compression;
using System.Text;

public class WriterRoundTripTests
{
    [Fact]
    public async Task Writer_creates_gp_archive_that_reader_can_open()
    {
        var score = CreateRoundTripScore();

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-roundtrip-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            File.Exists(outFile).Should().BeTrue();

            var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            readBack.Title.Should().Be("RoundTrip");
            readBack.Tracks.Should().HaveCount(1);
            readBack.Tracks[0].Staves[0].Measures.Should().NotBeEmpty();
            readBack.Tracks[0].PrimaryMeasure(0).Beats.Should().NotBeEmpty();
        }
        finally
        {
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }
        }
    }

    [Fact]
    public async Task Writer_and_reader_support_stream_based_core_contracts()
    {
        var score = CreateRoundTripScore();

        IScoreWriter writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
        await using var archiveBuffer = new MemoryStream();
        await writer.WriteAsync(score, archiveBuffer, TestContext.Current.CancellationToken);

        archiveBuffer.Length.Should().BeGreaterThan(0);

        using var archive = new ZipArchive(archiveBuffer, ZipArchiveMode.Read, leaveOpen: true);
        archive.Entries.Should().Contain(entry => entry.FullName == "Content/score.gpif");
        archive.Entries.Should().Contain(entry => entry.FullName == "Content/Preferences.json");

        IScoreReader reader = new Motif.Extensions.GuitarPro.GuitarProReader();
        var readBack = await reader.ReadAsync(archiveBuffer, TestContext.Current.CancellationToken);

        readBack.Title.Should().Be("RoundTrip");
        readBack.Tracks.Should().HaveCount(1);
        readBack.Tracks[0].Staves[0].Measures.Should().HaveCount(1);
        readBack.Tracks[0].PrimaryMeasure(0).Beats.Should().HaveCount(1);
    }

    [Fact]
    public async Task Writer_exposes_supported_public_diagnostics_api()
    {
        var score = CreateRoundTripScore();

        IGuitarProWriter writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
        await using var archiveBuffer = new MemoryStream();
        var diagnostics = await writer.WriteWithDiagnosticsAsync(score, archiveBuffer, TestContext.Current.CancellationToken);

        diagnostics.Should().NotBeNull();
        diagnostics.Entries.Should().NotBeNull();
        archiveBuffer.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Archive_writer_updates_existing_archive_without_losing_non_gpif_entries()
    {
        var source = GuitarProFixture.PathFor("test.gp");
        File.Exists(source).Should().BeTrue();

        var output = Path.Combine(Path.GetTempPath(), $"gpio-roundtrip-template-{Guid.NewGuid():N}.gp");
        try
        {
            File.Copy(source, output, overwrite: true);

            var replacementGpif = Encoding.UTF8.GetBytes("<GPIF><Score /></GPIF>");
            await using var gpifContent = new MemoryStream(replacementGpif);

            var archiveWriter = new ZipGpArchiveWriter();
            await archiveWriter.WriteArchiveAsync(gpifContent, output, TestContext.Current.CancellationToken);

            using var sourceZip = ZipFile.OpenRead(source);
            using var outputZip = ZipFile.OpenRead(output);

            var sourceEntryNames = sourceZip.Entries.Select(e => e.FullName).ToArray();
            var outputEntryNames = outputZip.Entries.Select(e => e.FullName).ToArray();
            outputEntryNames.Should().BeEquivalentTo(sourceEntryNames);

            foreach (var entryName in new[] { "Content/PartConfiguration", "Content/LayoutConfiguration", "Content/Preferences.json" })
            {
                var sourceEntry = sourceZip.GetEntry(entryName);
                var outputEntry = outputZip.GetEntry(entryName);
                sourceEntry.Should().NotBeNull();
                outputEntry.Should().NotBeNull();

                var sourceBytes = await ReadAllBytesAsync(sourceEntry!, TestContext.Current.CancellationToken);
                var outputBytes = await ReadAllBytesAsync(outputEntry!, TestContext.Current.CancellationToken);
                outputBytes.Should().Equal(sourceBytes, $"entry '{entryName}' payload should be preserved");
            }

            var scoreEntry = outputZip.GetEntry("Content/score.gpif");
            scoreEntry.Should().NotBeNull();
            var writtenGpif = await ReadAllBytesAsync(scoreEntry!, TestContext.Current.CancellationToken);
            writtenGpif.Should().Equal(replacementGpif);
        }
        finally
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
        }
    }

    [Fact]
    public async Task Archive_writer_seeds_new_archive_from_default_template_when_output_does_not_exist()
    {
        var output = Path.Combine(Path.GetTempPath(), $"gpio-roundtrip-default-template-{Guid.NewGuid():N}.gp");
        try
        {
            var replacementGpif = Encoding.UTF8.GetBytes("<GPIF><Score /></GPIF>");
            await using var gpifContent = new MemoryStream(replacementGpif);

            var archiveWriter = new ZipGpArchiveWriter();
            await archiveWriter.WriteArchiveAsync(gpifContent, output, TestContext.Current.CancellationToken);

            using var outputZip = ZipFile.OpenRead(output);
            outputZip.Entries.Count.Should().BeGreaterThan(1);
            outputZip.GetEntry("VERSION").Should().NotBeNull();
            outputZip.GetEntry("meta.json").Should().NotBeNull();
            outputZip.GetEntry("Content/Preferences.json").Should().NotBeNull();
            outputZip.GetEntry("Content/LayoutConfiguration").Should().NotBeNull();
            outputZip.GetEntry("Content/PartConfiguration").Should().NotBeNull();
            outputZip.GetEntry("Content/Stylesheets/score.gpss").Should().NotBeNull();
            outputZip.GetEntry("Content/ScoreViews/0.gpsv").Should().NotBeNull();

            var scoreEntry = outputZip.GetEntry("Content/score.gpif");
            scoreEntry.Should().NotBeNull();
            var writtenGpif = await ReadAllBytesAsync(scoreEntry!, TestContext.Current.CancellationToken);
            writtenGpif.Should().Equal(replacementGpif);
        }
        finally
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        await using var stream = entry.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static Score CreateRoundTripScore()
        => new()
        {
            Title = "RoundTrip",
            Artist = "GPIO",
            Album = "Tests",
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
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat
                            {
                                Id = 1,
                                Duration = 0.25m,
                                Notes =
                                [
                                    new Note
                                    {
                                        Id = 1,
                                        MidiPitch = 64,
                                        Articulation = new NoteArticulation { LetRing = true }
                                    }
                                ]
                            }
                        ]
                    })
            ]
        };
}
