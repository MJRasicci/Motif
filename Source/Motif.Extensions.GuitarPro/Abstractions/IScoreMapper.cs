namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;

/// <summary>
/// Maps raw GPIF structures into the public, traversal-friendly domain model.
/// </summary>
public interface IScoreMapper
{
    ValueTask<GuitarProScore> MapAsync(GpifDocument source, CancellationToken cancellationToken = default);
}
