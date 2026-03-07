namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Models;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public class CliPatchPlannerRegressionTests
{
    [Fact]
    public async Task No_edit_patch_from_json_is_zero_op_for_zero_based_ids()
    {
        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "GPIO.NET.Tool", "GPIO.NET.Tool.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"gpio-cli-regression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var sourceGp = Path.Combine(tempDir, "zero-based-source.gp");
        var jsonPath = Path.Combine(tempDir, "zero-based-source.json");
        var planPath = Path.Combine(tempDir, "zero-based-source.plan.json");
        var outputGp = Path.Combine(tempDir, "zero-based-source.patched.gp");

        try
        {
            var writer = new GPIO.NET.GuitarProWriter();
            await writer.WriteAsync(CreateZeroBasedPatchPlannerSourceScore(), sourceGp, TestContext.Current.CancellationToken);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{jsonPath}\" --format json",
                repoRoot);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{planPath}\" --from-json --patch-from-json --source-gp \"{sourceGp}\" --format json --plan-only",
                repoRoot);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{outputGp}\" --from-json --patch-from-json --source-gp \"{sourceGp}\" --format json",
                repoRoot);

            using var planDocument = JsonDocument.Parse(await File.ReadAllTextAsync(planPath, TestContext.Current.CancellationToken));
            var patchElement = planDocument.RootElement.GetProperty("Patch");
            var operationCount = patchElement.EnumerateObject().Sum(property => property.Value.GetArrayLength());
            var unsupportedCount = planDocument.RootElement.GetProperty("UnsupportedChanges").GetArrayLength();

            operationCount.Should().Be(0);
            unsupportedCount.Should().Be(0);

            var sourceBytes = await ReadScoreGpifBytesAsync(sourceGp, TestContext.Current.CancellationToken);
            var outputBytes = await ReadScoreGpifBytesAsync(outputGp, TestContext.Current.CancellationToken);
            outputBytes.Should().Equal(sourceBytes);
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
    public async Task No_edit_full_write_from_json_preserves_triplet_rhythm_and_section_cdata_for_schema_reference_fixture()
    {
        var sourceGp = FixturePath("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var repoRoot = FindRepositoryRoot();
        var toolProject = Path.Combine(repoRoot, "Source", "GPIO.NET.Tool", "GPIO.NET.Tool.csproj");
        Directory.Exists(Path.GetDirectoryName(toolProject)!).Should().BeTrue();

        var tempDir = Path.Combine(Path.GetTempPath(), $"gpio-cli-roundtrip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var jsonPath = Path.Combine(tempDir, "schema-reference.json");
        var outputGp = Path.Combine(tempDir, "schema-reference.roundtrip.gp");

        try
        {
            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{sourceGp}\" \"{jsonPath}\" --format json",
                repoRoot);

            await RunDotNetAsync(
                $"run --project \"{toolProject}\" -- \"{jsonPath}\" \"{outputGp}\" --from-json --source-gp \"{sourceGp}\" --format json",
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
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static GuitarProScore CreateZeroBasedPatchPlannerSourceScore()
        => new()
        {
            Tracks =
            [
                new TrackModel
                {
                    Id = 0,
                    Name = "Guitar",
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 0,
                            TimeSignature = "4/4",
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 0,
                                    Duration = 0.25m,
                                    Notes =
                                    [
                                        new NoteModel
                                        {
                                            Id = 0,
                                            MidiPitch = 60
                                        }
                                    ]
                                },
                                new BeatModel
                                {
                                    Id = 1,
                                    Duration = 0.25m,
                                    Notes =
                                    [
                                        new NoteModel
                                        {
                                            Id = 1,
                                            MidiPitch = 64
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

    private static string FixturePath(string fixtureName)
        => Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Source", "GPIO.NET.Tool", "GPIO.NET.Tool.csproj")))
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
