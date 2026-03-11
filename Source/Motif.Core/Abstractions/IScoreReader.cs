namespace Motif;

using Motif.Models;

/// <summary>
/// Reads an encoded score stream into the Motif domain model.
/// </summary>
public interface IScoreReader
{
    /// <summary>
    /// Reads a score from the provided format-specific stream.
    /// Implementations must not dispose the caller-owned stream.
    /// </summary>
    ValueTask<Score> ReadAsync(Stream source, CancellationToken cancellationToken = default);
}
