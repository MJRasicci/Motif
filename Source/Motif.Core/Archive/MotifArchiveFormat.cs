namespace Motif;

internal static class MotifArchiveFormat
{
    public const string Extension = ".motif";
    public const string CurrentFormatVersion = "1.0";
    public const string CreatedBy = "Motif.Core";
    public const string ManifestEntryName = "manifest.json";
    public const string ScoreEntryName = "score.json";

    public static MotifArchiveManifest CreateManifest(
        IReadOnlyList<MotifArchiveSource>? sources = null,
        IEnumerable<string>? extensionKeys = null)
        => new()
        {
            FormatVersion = CurrentFormatVersion,
            CreatedBy = CreatedBy,
            Sources = sources?
                .Select(source => new MotifArchiveSource
                {
                    Format = source.Format,
                    FileName = source.FileName,
                    ImportedAt = source.ImportedAt
                })
                .ToList()
                ?? [],
            Extensions = extensionKeys?
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? []
        };

    public static void ValidateManifest(MotifArchiveManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (string.IsNullOrWhiteSpace(manifest.FormatVersion))
        {
            throw new InvalidDataException("The .motif archive manifest is missing a format version.");
        }

        if (!TryNormalizeVersion(CurrentFormatVersion, out var supportedVersion)
            || !TryNormalizeVersion(manifest.FormatVersion, out var archiveVersion))
        {
            throw new InvalidDataException(
                $"The .motif archive manifest declares an invalid format version '{manifest.FormatVersion}'.");
        }

        if (archiveVersion > supportedVersion)
        {
            throw new NotSupportedException(
                $"The .motif archive format version '{manifest.FormatVersion}' is newer than the supported version '{CurrentFormatVersion}'.");
        }
    }

    private static bool TryNormalizeVersion(string value, out Version version)
    {
        if (!Version.TryParse(value, out var parsed))
        {
            version = new Version(0, 0, 0, 0);
            return false;
        }

        version = new Version(
            parsed.Major,
            Math.Max(parsed.Minor, 0),
            Math.Max(parsed.Build, 0),
            Math.Max(parsed.Revision, 0));
        return true;
    }
}
