namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Models;

public interface IGuitarProWriter
{
    ValueTask WriteAsync(GuitarProScore score, string filePath, CancellationToken cancellationToken = default);
}
