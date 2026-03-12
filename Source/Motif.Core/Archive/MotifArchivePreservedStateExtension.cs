namespace Motif;

using Motif.Models;

internal sealed class MotifArchivePreservedStateExtension : IModelExtension
{
    public IReadOnlyList<ArchiveEntry> PreservedEntries { get; init; } = [];

    public IReadOnlyList<string> ManifestExtensions { get; init; } = [];

    public IReadOnlyList<MotifArchiveSource> ManifestSources { get; init; } = [];
}
