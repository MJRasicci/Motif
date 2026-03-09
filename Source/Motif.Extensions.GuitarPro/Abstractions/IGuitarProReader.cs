namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Extensions.GuitarPro.Models;
using Motif.Models;

/// <summary>
/// High-level entry point for reading a Guitar Pro file and returning a mapped domain score.
/// </summary>
public interface IGuitarProReader
{
    ValueTask<GuitarProScore> ReadAsync(string filePath, GpReadOptions? options = null, CancellationToken cancellationToken = default);
}
