namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Write;

internal interface IScoreUnmapper
{
    ValueTask<WriteResult> UnmapAsync(Score score, CancellationToken cancellationToken = default);
}
