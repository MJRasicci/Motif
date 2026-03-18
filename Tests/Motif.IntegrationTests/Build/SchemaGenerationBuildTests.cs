namespace Motif.IntegrationTests;

using FluentAssertions;
using System.Diagnostics;
using System.Text.Json.Nodes;

public class SchemaGenerationBuildTests
{
    [Fact]
    public async Task Building_the_cli_project_generates_current_schema_artifacts()
    {
        var repoRoot = FindRepositoryRoot();
        var cliProject = Path.Combine(repoRoot, "Source", "Motif.CLI", "Motif.CLI.csproj");
        var schemaDirectory = Path.Combine(repoRoot, "artifacts", "schema");

        if (Directory.Exists(schemaDirectory))
        {
            Directory.Delete(schemaDirectory, recursive: true);
        }

        await RunDotNetAsync(
            $"build \"{cliProject}\" --no-restore",
            repoRoot);

        var manifestSchemaPath = Path.Combine(schemaDirectory, "manifest.schema.json");
        var scoreSchemaPath = Path.Combine(schemaDirectory, "score.schema.json");
        var guitarProSchemaPath = Path.Combine(schemaDirectory, "guitarpro.schema.json");

        File.Exists(manifestSchemaPath).Should().BeTrue();
        File.Exists(scoreSchemaPath).Should().BeTrue();
        File.Exists(guitarProSchemaPath).Should().BeTrue();

        var scoreSchema = JsonNode.Parse(
            await File.ReadAllTextAsync(scoreSchemaPath, TestContext.Current.CancellationToken));
        scoreSchema.Should().NotBeNull();
        scoreSchema!["title"]!.GetValue<string>().Should().Be("Motif Score");
        scoreSchema["properties"]!["pointControls"]!["type"]!.GetValue<string>().Should().Be("array");
        scoreSchema["properties"]!["spanControls"]!["type"]!.GetValue<string>().Should().Be("array");
        scoreSchema["properties"]!["timelineBars"]!["items"]!["properties"]!["start"]!["required"]!
            .AsArray()
            .Select(node => node!.GetValue<string>())
            .Should()
            .Equal("numerator", "denominator");
        scoreSchema["properties"]!["timelineBars"]!["items"]!["properties"]!["duration"]!["required"]!
            .AsArray()
            .Select(node => node!.GetValue<string>())
            .Should()
            .Equal("numerator", "denominator");
        scoreSchema["properties"]!["tracks"]!["items"]!["properties"]!["staves"]!["items"]!["properties"]!["measures"]!["items"]!["properties"]!["voices"]!["items"]!["properties"]!["beats"]!["items"]!["properties"]!["notes"]!["items"]!["properties"]!["articulation"]!["properties"]!["relations"]!["type"]!
            .GetValue<string>()
            .Should()
            .Be("array");
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "Motif.slnx")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory)!;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
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
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        process.ExitCode.Should().Be(
            0,
            "dotnet {0} should succeed.{1}{2}",
            arguments,
            string.IsNullOrWhiteSpace(standardOutput) ? string.Empty : $"{Environment.NewLine}stdout:{Environment.NewLine}{standardOutput}",
            string.IsNullOrWhiteSpace(standardError) ? string.Empty : $"{Environment.NewLine}stderr:{Environment.NewLine}{standardError}");
    }
}
