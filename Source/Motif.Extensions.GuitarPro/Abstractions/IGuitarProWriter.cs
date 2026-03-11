namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif;
using Motif.Models;

public interface IGuitarProWriter : IScoreWriter
{
    ValueTask WriteAsync(Score score, string filePath, CancellationToken cancellationToken = default);
}
