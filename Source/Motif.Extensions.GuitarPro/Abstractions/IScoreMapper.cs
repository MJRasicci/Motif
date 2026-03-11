namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;

/// <summary>
/// Maps raw GPIF structures into the public, traversal-friendly domain model.
/// </summary>
internal interface IScoreMapper
{
    ValueTask<Score> MapAsync(GpifDocument source, CancellationToken cancellationToken = default);
}
