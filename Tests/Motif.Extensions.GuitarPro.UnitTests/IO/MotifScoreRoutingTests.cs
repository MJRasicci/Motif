namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

public class MotifScoreRoutingTests
{
    [Fact]
    public void MotifScore_discovers_guitar_pro_handlers_when_the_extension_assembly_is_present()
    {
        var formats = MotifScore.GetRegisteredFormats();

        formats.Should().ContainSingle(format => format is GpFormatHandler);
        formats.Should().ContainSingle(format => format is GpifFormatHandler);
        MotifScore.CanOpen("song.gp").Should().BeTrue();
        MotifScore.CanOpen("song.gpif").Should().BeTrue();
        MotifScore.CreateReader("gp").Should().BeAssignableTo<IGuitarProReader>();
        MotifScore.CreateWriter("gp").Should().BeAssignableTo<IGuitarProWriter>();
        MotifScore.CreateWriter("gpif").Should().BeAssignableTo<IGpifWriter>();
    }

    [Fact]
    public async Task MotifScore_can_open_gp_and_save_gpif_via_discovered_handlers()
    {
        var fixturePath = GuitarProFixture.PathFor("test.gp");
        var tempDirectory = CreateTempDirectory();
        var gpifPath = Path.Combine(tempDirectory, "roundtrip.gpif");

        try
        {
            var score = await MotifScore.OpenAsync(fixturePath, TestContext.Current.CancellationToken);
            await MotifScore.SaveAsync(score, gpifPath, TestContext.Current.CancellationToken);

            var roundTripped = await MotifScore.OpenAsync(gpifPath, TestContext.Current.CancellationToken);
            roundTripped.Title.Should().Be(score.Title);
            roundTripped.Tracks.Count.Should().Be(score.Tracks.Count);
            roundTripped.TimelineBars.Count.Should().Be(score.TimelineBars.Count);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MotifScore_can_save_gp_via_the_discovered_guitar_pro_handler()
    {
        var fixturePath = GuitarProFixture.PathFor("test.gp");
        var tempDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(tempDirectory, "roundtrip.gp");

        try
        {
            var score = await MotifScore.OpenAsync(fixturePath, TestContext.Current.CancellationToken);
            await MotifScore.SaveAsync(score, outputPath, TestContext.Current.CancellationToken);

            var readBack = await new GuitarProReader().ReadAsync(outputPath, TestContext.Current.CancellationToken);
            readBack.Title.Should().Be(score.Title);
            readBack.Tracks.Count.Should().Be(score.Tracks.Count);
            readBack.TimelineBars.Count.Should().Be(score.TimelineBars.Count);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MotifScore_can_create_a_gpif_writer_with_diagnostics_via_discovered_handlers()
    {
        var fixturePath = GuitarProFixture.PathFor("test.gp");
        var tempDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(tempDirectory, "roundtrip.gpif");

        try
        {
            var score = await MotifScore.OpenAsync(fixturePath, TestContext.Current.CancellationToken);
            var writer = MotifScore.CreateWriter("gpif").Should().BeAssignableTo<IGpifWriter>().Subject;
            var diagnostics = await writer.WriteWithDiagnosticsAsync(score, outputPath, TestContext.Current.CancellationToken);

            File.Exists(outputPath).Should().BeTrue();
            diagnostics.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MotifScore_can_round_trip_gp_through_motif_without_losing_guitar_pro_metadata_or_archive_resources()
    {
        var fixturePath = GuitarProFixture.PathFor("test.gp");
        var tempDirectory = CreateTempDirectory();
        var motifPath = Path.Combine(tempDirectory, "roundtrip.motif");
        var outputPath = Path.Combine(tempDirectory, "roundtrip-from-motif.gp");

        try
        {
            var score = await MotifScore.OpenAsync(fixturePath, TestContext.Current.CancellationToken);
            score.GetGuitarPro().Should().NotBeNull();
            score.GetExtension<GpArchiveResourcesExtension>().Should().NotBeNull();

            await MotifScore.SaveAsync(score, motifPath, TestContext.Current.CancellationToken);

            using (var motifArchive = ZipFile.OpenRead(motifPath))
            {
                motifArchive.GetEntry("extensions/guitarpro.json").Should().NotBeNull();
                motifArchive.GetEntry("resources/guitarpro/Content/Preferences.json").Should().NotBeNull();

                using var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(motifArchive, "manifest.json"));
                manifest.RootElement.GetProperty("extensions").EnumerateArray()
                    .Select(element => element.GetString())
                    .Should().Contain("guitarpro");
            }

            var restored = await MotifScore.OpenAsync(motifPath, TestContext.Current.CancellationToken);
            restored.GetGuitarPro().Should().NotBeNull();
            restored.GetExtension<GpArchiveResourcesExtension>().Should().NotBeNull();

            await MotifScore.SaveAsync(restored, outputPath, TestContext.Current.CancellationToken);

            using var sourceArchive = ZipFile.OpenRead(fixturePath);
            using var outputArchive = ZipFile.OpenRead(outputPath);
            outputArchive.Entries.Select(entry => entry.FullName)
                .Should().BeEquivalentTo(sourceArchive.Entries.Select(entry => entry.FullName));

            foreach (var entryName in new[] { "VERSION", "meta.json", "Content/Preferences.json", "Content/LayoutConfiguration", "Content/PartConfiguration" })
            {
                var sourceBytes = await ReadArchiveEntryBytesAsync(sourceArchive, entryName);
                var outputBytes = await ReadArchiveEntryBytesAsync(outputArchive, entryName);
                outputBytes.Should().Equal(sourceBytes, $"entry '{entryName}' should be preserved through .motif");
            }
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "motif-gp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<byte[]> ReadArchiveEntryBytesAsync(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, TestContext.Current.CancellationToken);
        return buffer.ToArray();
    }

    private static async Task<string> ReadArchiveEntryTextAsync(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
