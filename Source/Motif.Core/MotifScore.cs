namespace Motif;

using Motif.Models;

/// <summary>
/// Central entry point for opening and saving Motif scores across registered formats.
/// </summary>
public static class MotifScore
{
    /// <summary>
    /// Opens a score from the provided file path by inferring the format from its extension.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <param name="cancellationToken">Cancels the read operation.</param>
    /// <returns>A mapped <see cref="Score"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is null, empty, whitespace, or has no extension.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for the inferred extension.</exception>
    public static ValueTask<Score> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var formatHint = FormatHandlerRegistry.GetFormatHintFromPath(filePath);
        return OpenAsync(filePath, formatHint, cancellationToken);
    }

    /// <summary>
    /// Opens a score from the provided file path using an explicit format hint.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <param name="formatHint">A file extension or format token such as <c>.gp</c> or <c>json</c>.</param>
    /// <param name="cancellationToken">Cancels the read operation.</param>
    /// <returns>A mapped <see cref="Score"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> or <paramref name="formatHint"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for <paramref name="formatHint"/>.</exception>
    public static async ValueTask<Score> OpenAsync(string filePath, string formatHint, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(formatHint);

        var reader = CreateReader(formatHint);
        var score = await reader.ReadAsync(filePath, cancellationToken).ConfigureAwait(false);
        MotifArchiveProvenance.AttachImportedSource(score, formatHint, filePath);
        return score;
    }

    /// <summary>
    /// Opens a score from the provided stream using an explicit format hint.
    /// </summary>
    /// <param name="source">The caller-owned source stream.</param>
    /// <param name="formatHint">A file extension or format token such as <c>.gp</c> or <c>json</c>.</param>
    /// <param name="cancellationToken">Cancels the read operation.</param>
    /// <returns>A mapped <see cref="Score"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="formatHint"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for <paramref name="formatHint"/>.</exception>
    public static ValueTask<Score> OpenAsync(Stream source, string formatHint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var reader = CreateReader(formatHint);
        return reader.ReadAsync(source, cancellationToken);
    }

    /// <summary>
    /// Creates a reader for the provided format hint using the currently registered handlers.
    /// </summary>
    /// <param name="formatHint">A file extension or format token such as <c>.gp</c> or <c>json</c>.</param>
    /// <returns>A format-specific <see cref="IScoreReader"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="formatHint"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for <paramref name="formatHint"/>.</exception>
    public static IScoreReader CreateReader(string formatHint)
        => ResolveHandlerOrThrow(formatHint).CreateReader();

    /// <summary>
    /// Saves a score to the provided file path by inferring the format from its extension.
    /// </summary>
    /// <param name="score">The score to serialize.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="cancellationToken">Cancels the write operation.</param>
    /// <returns>A task-like handle that completes when serialization finishes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="score"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is null, empty, whitespace, or has no extension.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for the inferred extension.</exception>
    public static async ValueTask SaveAsync(Score score, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var formatHint = FormatHandlerRegistry.GetFormatHintFromPath(filePath);
        var writer = CreateWriter(formatHint);
        await writer.WriteAsync(score, filePath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves a score to the provided stream using an explicit format hint.
    /// </summary>
    /// <param name="score">The score to serialize.</param>
    /// <param name="destination">The caller-owned destination stream.</param>
    /// <param name="formatHint">A file extension or format token such as <c>.gp</c> or <c>json</c>.</param>
    /// <param name="cancellationToken">Cancels the write operation.</param>
    /// <returns>A task-like handle that completes when serialization finishes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="score"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="formatHint"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for <paramref name="formatHint"/>.</exception>
    public static ValueTask SaveAsync(Score score, Stream destination, string formatHint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(destination);

        var writer = CreateWriter(formatHint);
        return writer.WriteAsync(score, destination, cancellationToken);
    }

    /// <summary>
    /// Creates a writer for the provided format hint using the currently registered handlers.
    /// </summary>
    /// <param name="formatHint">A file extension or format token such as <c>.gp</c> or <c>json</c>.</param>
    /// <returns>A format-specific <see cref="IScoreWriter"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="formatHint"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No handler is registered for <paramref name="formatHint"/>.</exception>
    public static IScoreWriter CreateWriter(string formatHint)
        => ResolveHandlerOrThrow(formatHint).CreateWriter();

    /// <summary>
    /// Returns all currently available score formats, including Motif's built-in JSON and archive support.
    /// </summary>
    public static IReadOnlyList<IFormatHandler> GetRegisteredFormats()
        => FormatHandlerRegistry.GetRegisteredHandlers();

    /// <summary>
    /// Returns whether Motif can open the provided file path with the currently registered handlers.
    /// </summary>
    /// <param name="filePath">The file path to inspect.</param>
    public static bool CanOpen(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var extension = Path.GetExtension(filePath);
        return !string.IsNullOrWhiteSpace(extension)
               && FormatHandlerRegistry.TryResolve(extension, out _);
    }

    /// <summary>
    /// Registers a format handler explicitly and returns a token that unregisters it on dispose.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    /// <returns>A disposable registration token.</returns>
    public static IDisposable RegisterHandler(IFormatHandler handler)
        => FormatHandlerRegistry.Register(handler);

    /// <summary>
    /// Registers an archive contributor explicitly and returns a token that unregisters it on dispose.
    /// </summary>
    /// <param name="contributor">The archive contributor to register.</param>
    /// <returns>A disposable registration token.</returns>
    public static IDisposable RegisterArchiveContributor(IArchiveContributor contributor)
        => ArchiveContributorRegistry.Register(contributor);

    private static IFormatHandler ResolveHandlerOrThrow(string formatHint)
    {
        var normalizedHint = FormatHandlerRegistry.NormalizeFormatHint(formatHint);
        return FormatHandlerRegistry.TryResolve(normalizedHint, out var handler)
            ? handler!
            : throw new InvalidOperationException(
                $"No format handler registered for '{normalizedHint}'. Reference the relevant Motif extension package or call {nameof(MotifScore)}.{nameof(RegisterHandler)}(...).");
    }
}
