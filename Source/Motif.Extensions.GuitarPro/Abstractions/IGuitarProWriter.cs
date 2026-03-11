namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif;
using Motif.Extensions.GuitarPro.Models.Write;
using Motif.Models;

public interface IGuitarProWriter : IScoreWriter
{
    ValueTask WriteAsync(Score score, string filePath, CancellationToken cancellationToken = default);

    ValueTask<WriteDiagnostics> WriteWithDiagnosticsAsync(Score score, string filePath, CancellationToken cancellationToken = default);

    ValueTask<WriteDiagnostics> WriteWithDiagnosticsAsync(Score score, Stream destination, CancellationToken cancellationToken = default);
}
