namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public class CliRoundTripRegressionTests
{
    [Fact]
    public async Task No_edit_full_write_from_json_preserves_triplet_rhythm_and_section_cdata_for_schema_reference_fixture()
    {
        var sourceGp = FixturePath("schema-reference.gp");
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
            json.Should().Contain("\"PrimaryTuplet\"");

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
        var sourceGp = FixturePath("test.gp");
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
    public async Task Batch_roundtrip_diagnostics_writes_summary_and_file_results_for_fixture_directory()
    {
        var sourceGp = FixturePath("schema-reference.gp");
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
            summary.RootElement.GetProperty("FilesWithByteDrift").GetInt32().Should().BeGreaterThan(0);

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

    private static string FixturePath(string fixtureName)
        => Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);

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

        if (process.ExitCode != 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"dotnet {arguments} failed with exit code {process.ExitCode}{Environment.NewLine}stdout:{Environment.NewLine}{stdout}{Environment.NewLine}stderr:{Environment.NewLine}{stderr}");
        }
    }

    private static async Task<byte[]> ReadScoreGpifBytesAsync(string gpPath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(gpPath);
        var entry = archive.GetEntry("Content/score.gpif");
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
