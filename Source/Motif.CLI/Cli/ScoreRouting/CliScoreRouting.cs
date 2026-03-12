namespace Motif.CLI;

using Motif;
using Motif.Models;

internal static class CliScoreRouting
{
    public static IScoreWriter CreateWriter(CliFormat format)
        => MotifScore.CreateWriter(format.ToToken());

    public static async ValueTask<Score> OpenAsync(
        string filePath,
        CliFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return await MotifScore.OpenAsync(filePath, format.ToToken(), cancellationToken).ConfigureAwait(false);
    }
}
