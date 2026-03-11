namespace Motif;

using Motif.Models;

/// <summary>
/// Writes a Motif domain score to an encoded format-specific stream.
/// </summary>
public interface IScoreWriter
{
    /// <summary>
    /// Writes the score to the provided destination stream.
    /// Implementations must not dispose the caller-owned stream.
    /// </summary>
    ValueTask WriteAsync(Score score, Stream destination, CancellationToken cancellationToken = default);
}
