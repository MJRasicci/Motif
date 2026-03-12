namespace Motif.IntegrationTests;

using FluentAssertions;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

public class CliRoundTripRegressionTests
{
    [Fact]
    public async Task No_edit_full_write_from_json_preserves_triplet_rhythm_and_section_cdata_for_schema_reference_fixture()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"gpio-cli-roundtrip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var jsonPath = Path.Combine(tempDir, "schema-reference.json");
        var outputGp = Path.Combine(tempDir, "schema-reference.roundtrip.gp");
        var diagnosticsPath = Path.Combine(tempDir, "schema-reference.diagnostics.json");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{jsonPath}\" --format json",
                repoRoot);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{outputGp}\" --from-json --source-gp \"{sourceGp}\" --format json --diagnostics-out \"{diagnosticsPath}\" --diagnostics-json",
                repoRoot);

            var json = await File.ReadAllTextAsync(jsonPath, TestContext.Current.CancellationToken);
            json.Should().NotContain("\"PrimaryTuplet\"");
            json.Should().NotContain("\"SourceRhythm\"");
            json.Should().NotContain("\"SourceRhythmId\"");

            var gpifText = Encoding.UTF8.GetString(await ReadScoreGpifBytesAsync(outputGp, TestContext.Current.CancellationToken));
            var doc = XDocument.Parse(gpifText);

            var sectionText = doc.Root?
                .Element("MasterBars")?
                .Elements("MasterBar")
                .Select(mb => mb.Element("Section")?.Element("Text"))
                .FirstOrDefault(text => string.Equals(text?.Value, "Voices & Rhythms", StringComparison.Ordinal));

            sectionText.Should().NotBeNull();
            sectionText!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Voices & Rhythms");

            var tuplet = doc.Root?
                .Element("Rhythms")?
                .Elements("Rhythm")
                .Select(r => r.Element("PrimaryTuplet"))
                .FirstOrDefault(element =>
                    element?.Attribute("num")?.Value == "3"
                    && element.Attribute("den")?.Value == "2");

            tuplet.Should().NotBeNull();
            tuplet!.Elements().Should().BeEmpty();

            File.Exists(diagnosticsPath).Should().BeTrue();
            using var diagnostics = JsonDocument.Parse(await File.ReadAllTextAsync(diagnosticsPath, TestContext.Current.CancellationToken));
            var codes = diagnostics.RootElement
                .EnumerateArray()
                .Select(entry => entry.GetProperty("Code").GetString())
                .ToArray();

            codes.Should().Contain("RAW_GPIF_BYTE_DRIFT");
            codes.Should().NotContain("EMPTY_SCORE_NODES_DROPPED");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task No_edit_full_write_from_json_preserves_stem_orientation_for_test_fixture()
    {
        var sourceGp = GuitarProFixture.PathFor("test.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"gpio-cli-stem-rt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var jsonPath = Path.Combine(tempDir, "test.json");
        var outputGp = Path.Combine(tempDir, "test.roundtrip.gp");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{jsonPath}\" --format json",
                repoRoot);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{outputGp}\" --from-json --source-gp \"{sourceGp}\" --format json",
                repoRoot);

            var doc = XDocument.Parse(Encoding.UTF8.GetString(await ReadScoreGpifBytesAsync(outputGp, TestContext.Current.CancellationToken)));
            var beats = doc.Root?
                .Element("Beats")?
                .Elements("Beat")
                .ToDictionary(
                    beat => int.Parse(beat.Attribute("id")!.Value, System.Globalization.CultureInfo.InvariantCulture),
                    beat => beat);

            beats.Should().NotBeNull();
            beats![0].Element("TransposedPitchStemOrientation")?.Value.Should().Be("Downward");
            beats[0].Element("ConcertPitchStemOrientation")?.Value.Should().Be("Undefined");
            beats[8].Element("TransposedPitchStemOrientation")?.Value.Should().Be("Upward");
            beats[8].Element("ConcertPitchStemOrientation")?.Value.Should().Be("Undefined");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Cli_can_route_by_extensions_and_explicit_format_flags_without_legacy_mode_switches()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"motif-cli-format-routing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var jsonPath = Path.Combine(tempDir, "schema-reference.json");
        var explicitJsonPath = Path.Combine(tempDir, "schema-reference.data");
        var explicitOutputPath = Path.Combine(tempDir, "schema-reference.roundtrip.data");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{jsonPath}\"",
                repoRoot);

            File.Exists(jsonPath).Should().BeTrue();

            File.Copy(jsonPath, explicitJsonPath, overwrite: true);
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{explicitJsonPath}\" \"{explicitOutputPath}\" --input-format json --output-format gp --source-gp \"{sourceGp}\"",
                repoRoot);

            using var archive = ZipFile.OpenRead(explicitOutputPath);
            archive.GetEntry("Content/score.gpif").Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Cli_can_route_extensionless_gp_and_gpif_inputs_when_explicit_formats_are_supplied()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"motif-cli-explicit-input-routing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var extensionlessGpPath = Path.Combine(tempDir, "schema-reference.input");
        var extensionlessJsonPath = Path.Combine(tempDir, "schema-reference.output");
        var extensionlessGpifPath = Path.Combine(tempDir, "schema-reference.raw");
        var jsonFromGpifPath = Path.Combine(tempDir, "schema-reference.from-gpif.output");
        var motifFromGpPath = Path.Combine(tempDir, "schema-reference.from-gp.archive");
        var extensionlessMotifPath = Path.Combine(tempDir, "schema-reference.archive");
        var jsonFromMotifPath = Path.Combine(tempDir, "schema-reference.from-motif.output");

        try
        {
            File.Copy(sourceGp, extensionlessGpPath, overwrite: true);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{extensionlessGpPath}\" \"{extensionlessJsonPath}\" --input-format gp --output-format json",
                repoRoot);

            var mappedJson = await File.ReadAllTextAsync(extensionlessJsonPath, TestContext.Current.CancellationToken);
            mappedJson.Should().Contain("\"Tracks\"");

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{extensionlessGpPath}\" \"{motifFromGpPath}\" --input-format gp --output-format motif",
                repoRoot);

            using (var motifArchive = ZipFile.OpenRead(motifFromGpPath))
            using (var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(motifArchive, "manifest.json", TestContext.Current.CancellationToken)))
            {
                var sources = manifest.RootElement.GetProperty("sources").EnumerateArray().ToArray();
                sources.Should().ContainSingle();
                sources[0].GetProperty("format").GetString().Should().Be(".gp");
                sources[0].GetProperty("fileName").GetString().Should().Be("schema-reference.input");
            }

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{extensionlessGpifPath}\" --output-format gpif",
                repoRoot);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{extensionlessGpifPath}\" \"{jsonFromGpifPath}\" --input-format gpif --output-format json",
                repoRoot);

            var jsonFromGpif = await File.ReadAllTextAsync(jsonFromGpifPath, TestContext.Current.CancellationToken);
            jsonFromGpif.Should().Contain("\"TimelineBars\"");

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{extensionlessJsonPath}\" \"{extensionlessMotifPath}\" --input-format json --output-format motif",
                repoRoot);

            File.Exists(extensionlessMotifPath).Should().BeTrue();

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{extensionlessMotifPath}\" \"{jsonFromMotifPath}\" --input-format motif --output-format json",
                repoRoot);

            var jsonFromMotif = await File.ReadAllTextAsync(jsonFromMotifPath, TestContext.Current.CancellationToken);
            jsonFromMotif.Should().Contain("\"Tracks\"");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Cli_can_route_between_json_gp_gpif_and_motif_inputs_and_outputs()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"motif-cli-gp-gpif-routing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var jsonPath = Path.Combine(tempDir, "schema-reference.json");
        var motifPath = Path.Combine(tempDir, "schema-reference.motif");
        var jsonFromMotifPath = Path.Combine(tempDir, "schema-reference.from-motif.json");
        var gpifPath = Path.Combine(tempDir, "schema-reference.score.gpif");
        var motifFromGpifPath = Path.Combine(tempDir, "schema-reference.from-gpif.motif");
        var gpFromMotifPath = Path.Combine(tempDir, "schema-reference.from-motif.gp");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{jsonPath}\"",
                repoRoot);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{motifPath}\"",
                repoRoot);

            File.Exists(motifPath).Should().BeTrue();
            using (var motifArchive = ZipFile.OpenRead(motifPath))
            {
                motifArchive.GetEntry("manifest.json").Should().NotBeNull();
                motifArchive.GetEntry("score.json").Should().NotBeNull();
                motifArchive.GetEntry("extensions/guitarpro.json").Should().NotBeNull();
                motifArchive.GetEntry("resources/guitarpro/Content/Preferences.json").Should().NotBeNull();

                using var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(motifArchive, "manifest.json", TestContext.Current.CancellationToken));
                manifest.RootElement.GetProperty("formatVersion").GetString().Should().Be("1.0");
                manifest.RootElement.GetProperty("createdBy").GetString().Should().Be("Motif.Core");
                var sources = manifest.RootElement.GetProperty("sources").EnumerateArray().ToArray();
                sources.Should().ContainSingle();
                sources[0].GetProperty("format").GetString().Should().Be(".gp");
                sources[0].GetProperty("fileName").GetString().Should().Be("schema-reference.gp");
                manifest.RootElement.GetProperty("extensions").EnumerateArray()
                    .Select(element => element.GetString())
                    .Should().Contain("guitarpro");
            }

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{motifPath}\" \"{jsonFromMotifPath}\"",
                repoRoot);

            File.Exists(jsonFromMotifPath).Should().BeTrue();
            var jsonFromMotif = await File.ReadAllTextAsync(jsonFromMotifPath, TestContext.Current.CancellationToken);
            jsonFromMotif.Should().Contain("\"Tracks\"");
            jsonFromMotif.Should().Contain("\"TimelineBars\"");

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{gpifPath}\"",
                repoRoot);

            File.Exists(gpifPath).Should().BeTrue();
            var gpif = await File.ReadAllTextAsync(gpifPath, TestContext.Current.CancellationToken);
            gpif.Should().Contain("<GPIF");

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{gpifPath}\" \"{motifFromGpifPath}\"",
                repoRoot);

            using (var motifFromGpifArchive = ZipFile.OpenRead(motifFromGpifPath))
            {
                motifFromGpifArchive.GetEntry("manifest.json").Should().NotBeNull();
                motifFromGpifArchive.GetEntry("score.json").Should().NotBeNull();

                using var manifest = JsonDocument.Parse(await ReadArchiveEntryTextAsync(motifFromGpifArchive, "manifest.json", TestContext.Current.CancellationToken));
                var sources = manifest.RootElement.GetProperty("sources").EnumerateArray().ToArray();
                sources.Should().ContainSingle();
                sources[0].GetProperty("format").GetString().Should().Be(".gpif");
                sources[0].GetProperty("fileName").GetString().Should().Be("schema-reference.score.gpif");
            }

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{motifPath}\" \"{gpFromMotifPath}\"",
                repoRoot);

            using (var sourceArchive = ZipFile.OpenRead(sourceGp))
            using (var archive = ZipFile.OpenRead(gpFromMotifPath))
            {
                archive.GetEntry("Content/score.gpif").Should().NotBeNull();
                archive.Entries.Select(entry => entry.FullName)
                    .Should().BeEquivalentTo(sourceArchive.Entries.Select(entry => entry.FullName));

                foreach (var entryName in new[] { "VERSION", "meta.json", "Content/Preferences.json", "Content/LayoutConfiguration", "Content/PartConfiguration" })
                {
                    var sourceBytes = await ReadArchiveEntryBytesAsync(sourceArchive, entryName, TestContext.Current.CancellationToken);
                    var outputBytes = await ReadArchiveEntryBytesAsync(archive, entryName, TestContext.Current.CancellationToken);
                    outputBytes.Should().Equal(sourceBytes, $"entry '{entryName}' should survive gp -> motif -> gp");
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Cli_can_round_trip_gp_through_motif_after_editing_embedded_score_json()
    {
        var sourceGp = GuitarProFixture.PathFor("test.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"motif-cli-edited-archive-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var motifPath = Path.Combine(tempDir, "test.edited.motif");
        var gpFromMotifPath = Path.Combine(tempDir, "test.edited.from-motif.gp");
        var diagnosticsPath = Path.Combine(tempDir, "test.edited.from-motif.diagnostics.json");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{motifPath}\"",
                repoRoot);

            await UpdateMotifScoreJsonAsync(motifPath, root =>
            {
                root["Title"] = "Edited From Motif";
            });

            var result = await RunDotNetForResultAsync(
                $"run --project \"{toolProject}\" -- \"{motifPath}\" \"{gpFromMotifPath}\" --diagnostics-out \"{diagnosticsPath}\" --diagnostics-json",
                repoRoot);
            result.ExitCode.Should().Be(0, result.Stderr);
            result.Stdout.Should().Contain("Warnings: 0");

            var gpifText = Encoding.UTF8.GetString(await ReadScoreGpifBytesAsync(gpFromMotifPath, TestContext.Current.CancellationToken));
            var doc = XDocument.Parse(gpifText);
            doc.Root?
                .Element("Score")?
                .Element("Title")?
                .Value
                .Should().Be("Edited From Motif");

            File.Exists(diagnosticsPath).Should().BeFalse("the CLI only writes a diagnostics file when warnings are present");

            using (var sourceArchive = ZipFile.OpenRead(sourceGp))
            using (var archive = ZipFile.OpenRead(gpFromMotifPath))
            {
                foreach (var entryName in new[] { "VERSION", "meta.json", "Content/Preferences.json", "Content/LayoutConfiguration", "Content/PartConfiguration" })
                {
                    var sourceBytes = await ReadArchiveEntryBytesAsync(sourceArchive, entryName, TestContext.Current.CancellationToken);
                    var outputBytes = await ReadArchiveEntryBytesAsync(archive, entryName, TestContext.Current.CancellationToken);
                    outputBytes.Should().Equal(sourceBytes, $"entry '{entryName}' should survive motif score.json edits");
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Cli_rejects_non_v1_formats_and_unknown_output_extensions()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"motif-cli-unimplemented-routing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var jsonPath = Path.Combine(tempDir, "schema-reference.json");
        var musicXmlInputPath = Path.Combine(tempDir, "score.musicxml");
        var mxlInputPath = Path.Combine(tempDir, "score.mxl");
        var midiInputPath = Path.Combine(tempDir, "score.mid");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{jsonPath}\"",
                repoRoot);

            await File.WriteAllTextAsync(musicXmlInputPath, "<score-partwise version=\"4.0\" />", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(mxlInputPath, "not-a-real-archive", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(midiInputPath, "not-a-real-midi", TestContext.Current.CancellationToken);

            var failureCases = new (string Arguments, string ExpectedMessage)[]
            {
                ($"run --project \"{toolProject}\" -- \"{musicXmlInputPath}\" \"{Path.Combine(tempDir, "musicxml.json")}\"", "Unable to infer input format"),
                ($"run --project \"{toolProject}\" -- \"{mxlInputPath}\" \"{Path.Combine(tempDir, "mxl.json")}\"", "Unable to infer input format"),
                ($"run --project \"{toolProject}\" -- \"{midiInputPath}\" \"{Path.Combine(tempDir, "midi.json")}\"", "Unable to infer input format"),
                ($"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{Path.Combine(tempDir, "score.musicxml")}\"", "Unable to infer output format"),
                ($"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{Path.Combine(tempDir, "score.mxl")}\"", "Unable to infer output format"),
                ($"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{Path.Combine(tempDir, "score.mid")}\"", "Unable to infer output format"),
                ($"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{Path.Combine(tempDir, "score.gp")}\" --output-format musicxml", "Unknown format 'musicxml'."),
                ($"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{Path.Combine(tempDir, "score.gp")}\" --output-format midi", "Unknown format 'midi'.")
            };

            foreach (var failureCase in failureCases)
            {
                var stderr = await RunDotNetExpectFailureAsync(failureCase.Arguments, repoRoot);
                stderr.Should().Contain(failureCase.ExpectedMessage);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Batch_roundtrip_diagnostics_writes_summary_and_file_results_for_fixture_directory()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"gpio-cli-batch-rt-{Guid.NewGuid():N}");
        var inputDir = Path.Combine(tempDir, "input");
        var outputDir = Path.Combine(tempDir, "output");
        Directory.CreateDirectory(Path.Combine(inputDir, "nested"));
        Directory.CreateDirectory(outputDir);

        var copiedGp = Path.Combine(inputDir, "nested", "schema-reference.gp");
        File.Copy(sourceGp, copiedGp, overwrite: true);

        var summaryPath = Path.Combine(outputDir, "batch-roundtrip-summary.json");
        var fileResultsPath = Path.Combine(outputDir, "batch-file-results.jsonl");
        var diagnosticsPath = Path.Combine(outputDir, "batch-diagnostics.jsonl");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- --batch-input-dir \"{inputDir}\" --batch-output-dir \"{outputDir}\" --batch-roundtrip-diagnostics",
                repoRoot);

            File.Exists(summaryPath).Should().BeTrue();
            File.Exists(fileResultsPath).Should().BeTrue();
            File.Exists(diagnosticsPath).Should().BeTrue();

            using var summary = JsonDocument.Parse(await File.ReadAllTextAsync(summaryPath, TestContext.Current.CancellationToken));
            summary.RootElement.GetProperty("TotalFiles").GetInt32().Should().Be(1);
            summary.RootElement.GetProperty("SucceededFiles").GetInt32().Should().Be(1);
            summary.RootElement.GetProperty("FailedFiles").GetInt32().Should().Be(0);
            summary.RootElement.GetProperty("FilesWithDiagnostics").GetInt32().Should().BeGreaterThan(0);
            summary.RootElement.GetProperty("FilesWithWarnings").GetInt32().Should().Be(0);
            summary.RootElement.GetProperty("FilesWithInfos").GetInt32().Should().BeGreaterThan(0);
            summary.RootElement.GetProperty("FilesWithByteDrift").GetInt32().Should().BeGreaterThan(0);
            summary.RootElement.GetProperty("TotalWarnings").GetInt32().Should().Be(0);
            summary.RootElement.GetProperty("TotalInfos").GetInt32().Should().BeGreaterThan(0);

            var diagnosticCodes = summary.RootElement
                .GetProperty("DiagnosticCodes")
                .EnumerateArray()
                .Select(entry => entry.GetProperty("Name").GetString())
                .ToArray();

            diagnosticCodes.Should().Contain("RAW_GPIF_BYTE_DRIFT");

            var fileResults = await File.ReadAllLinesAsync(fileResultsPath, TestContext.Current.CancellationToken);
            fileResults.Should().ContainSingle();

            using var fileResult = JsonDocument.Parse(fileResults[0]);
            fileResult.RootElement.GetProperty("RelativePath").GetString().Should().Be(Path.Combine("nested", "schema-reference.gp"));
            fileResult.RootElement.GetProperty("DiagnosticCount").GetInt32().Should().BeGreaterThan(0);

            var diagnostics = await File.ReadAllLinesAsync(diagnosticsPath, TestContext.Current.CancellationToken);
            diagnostics.Should().NotBeEmpty();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Batch_export_can_extract_gpif_when_output_format_is_gpif()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"motif-cli-batch-gpif-{Guid.NewGuid():N}");
        var inputDir = Path.Combine(tempDir, "input");
        var outputDir = Path.Combine(tempDir, "output");
        Directory.CreateDirectory(Path.Combine(inputDir, "nested"));
        Directory.CreateDirectory(outputDir);

        var copiedGp = Path.Combine(inputDir, "nested", "schema-reference.gp");
        File.Copy(sourceGp, copiedGp, overwrite: true);

        var extractedGpif = Path.Combine(outputDir, "nested", "schema-reference.score.gpif");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- --batch-input-dir \"{inputDir}\" --batch-output-dir \"{outputDir}\" --output-format gpif",
                repoRoot);

            File.Exists(extractedGpif).Should().BeTrue();
            var gpif = await File.ReadAllTextAsync(extractedGpif, TestContext.Current.CancellationToken);
            gpif.Should().Contain("<GPIF");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Batch_export_can_write_motif_archives_when_output_format_is_motif()
    {
        var sourceGp = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"motif-cli-batch-motif-{Guid.NewGuid():N}");
        var inputDir = Path.Combine(tempDir, "input");
        var outputDir = Path.Combine(tempDir, "output");
        Directory.CreateDirectory(Path.Combine(inputDir, "nested"));
        Directory.CreateDirectory(outputDir);

        var copiedGp = Path.Combine(inputDir, "nested", "schema-reference.gp");
        File.Copy(sourceGp, copiedGp, overwrite: true);

        var motifArchivePath = Path.Combine(outputDir, "nested", "schema-reference.motif");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- --batch-input-dir \"{inputDir}\" --batch-output-dir \"{outputDir}\" --output-format motif",
                repoRoot);

            File.Exists(motifArchivePath).Should().BeTrue();
            using var archive = ZipFile.OpenRead(motifArchivePath);
            archive.GetEntry("manifest.json").Should().NotBeNull();
            archive.GetEntry("score.json").Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Source", "Motif.CLI", "Motif.CLI.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root from test output directory.");
    }

    private static async Task RunDotNetAsync(string arguments, string workingDirectory)
    {
        var result = await RunDotNetForResultAsync(arguments, workingDirectory);

        if (result.ExitCode != 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"dotnet {arguments} failed with exit code {result.ExitCode}{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");
        }
    }

    private static async Task<string> RunDotNetExpectFailureAsync(string arguments, string workingDirectory)
    {
        var result = await RunDotNetForResultAsync(arguments, workingDirectory);

        if (result.ExitCode == 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"dotnet {arguments} unexpectedly succeeded.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");
        }

        return result.Stderr;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunDotNetForResultAsync(string arguments, string workingDirectory)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        return (process.ExitCode, stdout, stderr);
    }

    private static async Task<byte[]> ReadScoreGpifBytesAsync(string gpPath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(gpPath);
        return await ReadArchiveEntryBytesAsync(archive, "Content/score.gpif", cancellationToken);
    }

    private static async Task UpdateMotifScoreJsonAsync(string motifPath, Action<JsonObject> update)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motifPath);
        ArgumentNullException.ThrowIfNull(update);

        using var archive = ZipFile.Open(motifPath, ZipArchiveMode.Update);
        var entry = archive.GetEntry("score.json");
        entry.Should().NotBeNull();

        string json;
        await using (var stream = entry!.Open())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false))
        {
            json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        }

        entry.Delete();

        var root = JsonNode.Parse(json)?.AsObject();
        root.Should().NotBeNull();
        update(root!);

        var updatedEntry = archive.CreateEntry("score.json");
        await using var output = updatedEntry.Open();
        using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: false);
        await writer.WriteAsync(root!.ToJsonString());
    }

    private static async Task<byte[]> ReadArchiveEntryBytesAsync(
        ZipArchive archive,
        string entryName,
        CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static async Task<string> ReadArchiveEntryTextAsync(
        ZipArchive archive,
        string entryName,
        CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
