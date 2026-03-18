namespace Motif.SchemaExport;

using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var outputDirectory = args.Length > 0
            ? Path.GetFullPath(args[0], Environment.CurrentDirectory)
            : Path.Combine(ResolveRepositoryRoot(), "artifacts", "schema");

        Directory.CreateDirectory(outputDirectory);

        var scoreAssembly = typeof(Score).Assembly;
        var guitarProAssembly = typeof(GpScoreExtension).Assembly;
        var writeOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true
        };

        var schemas = new[]
        {
            new SchemaSpec(
                JsonFilePath: "manifest.json",
                SchemaFileName: "manifest.schema.json",
                Title: "Motif Archive Manifest",
                Description: "Schema for manifest.json inside a native .motif archive.",
                RootType: ResolveRootType(scoreAssembly, "Motif.MotifArchiveManifest"),
                ContextResolver: ResolveContext(scoreAssembly, "Motif.MotifArchiveJsonContext")),
            new SchemaSpec(
                JsonFilePath: "score.json",
                SchemaFileName: "score.schema.json",
                Title: "Motif Score",
                Description: "Schema for score.json inside a native .motif archive.",
                RootType: ResolveRootType(scoreAssembly, "Motif.Models.Score"),
                ContextResolver: ResolveContext(scoreAssembly, "Motif.MotifJsonContext")),
            new SchemaSpec(
                JsonFilePath: "extensions/guitarpro.json",
                SchemaFileName: "guitarpro.schema.json",
                Title: "Motif Guitar Pro Extension State",
                Description: "Schema for extensions/guitarpro.json inside a native .motif archive.",
                RootType: ResolveRootType(
                    guitarProAssembly,
                    "Motif.Extensions.GuitarPro.Models.GpMotifArchiveState"),
                ContextResolver: ResolveContext(
                    guitarProAssembly,
                    "Motif.Extensions.GuitarPro.Serialization.GpMotifArchiveJsonContext"))
        };

        foreach (var schema in schemas)
        {
            var schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    TypeInfoResolver = schema.ContextResolver
                },
                schema.RootType,
                exporterOptions);

            var outputPath = Path.Combine(outputDirectory, schema.SchemaFileName);
            var decoratedSchema = DecorateRootSchema(schema, schemaNode);

            await File.WriteAllTextAsync(
                outputPath,
                decoratedSchema.ToJsonString(writeOptions) + Environment.NewLine).ConfigureAwait(false);

            Console.WriteLine($"{schema.JsonFilePath} -> {outputPath}");
        }

        Console.WriteLine();
        Console.WriteLine($"Generated {schemas.Length} schema file(s) in {outputDirectory}");
        return 0;
    }

    private static string ResolveRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static IJsonTypeInfoResolver ResolveContext(
        Assembly assembly,
        string contextTypeName)
    {
        var contextType = assembly.GetType(contextTypeName, throwOnError: true)
            ?? throw new InvalidOperationException(
                $"Unable to load JSON context '{contextTypeName}' from assembly '{assembly.FullName}'.");
        var defaultContextProperty = contextType.GetProperty(
            "Default",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"JSON context '{contextTypeName}' does not expose a static Default property.");

        return defaultContextProperty.GetValue(null) as IJsonTypeInfoResolver
            ?? throw new InvalidOperationException(
                $"JSON context '{contextTypeName}' returned a null Default instance.");
    }

    private static Type ResolveRootType(Assembly assembly, string typeName)
    {
        return assembly.GetType(typeName, throwOnError: true)
            ?? throw new InvalidOperationException(
                $"Unable to load root type '{typeName}' from assembly '{assembly.FullName}'.");
    }

    private static JsonObject DecorateRootSchema(SchemaSpec schema, JsonNode schemaNode)
    {
        if (schemaNode is not JsonObject rootObject)
        {
            throw new InvalidOperationException(
                $"Expected an object JSON schema for '{schema.JsonFilePath}', but received '{schemaNode.GetValueKind()}'.");
        }

        var decorated = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = schema.Title,
            ["description"] = schema.Description
        };

        foreach (var property in rootObject)
        {
            decorated[property.Key] = property.Value?.DeepClone();
        }

        return decorated;
    }

    private sealed record SchemaSpec(
        string JsonFilePath,
        string SchemaFileName,
        string Title,
        string Description,
        Type RootType,
        IJsonTypeInfoResolver ContextResolver);
}
