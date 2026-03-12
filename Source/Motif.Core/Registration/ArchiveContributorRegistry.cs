namespace Motif;

using Motif.Models;
using System.Reflection;

internal static class ArchiveContributorRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly List<ExplicitRegistration> ExplicitRegistrations = [];

    public static IDisposable Register(IArchiveContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        ValidateContributor(contributor);

        var registration = new ExplicitRegistration(contributor);
        lock (SyncRoot)
        {
            ExplicitRegistrations.Add(registration);
        }

        return new RegistrationHandle(registration);
    }

    public static IReadOnlyList<IArchiveContributor> GetRegisteredContributors()
    {
        var contributors = new List<IArchiveContributor>();

        lock (SyncRoot)
        {
            for (var i = ExplicitRegistrations.Count - 1; i >= 0; i--)
            {
                contributors.Add(ExplicitRegistrations[i].Contributor);
            }
        }

        contributors.AddRange(DiscoverContributors());

        var unique = DeduplicateByType(contributors);
        EnsureDistinctContributorKeys(unique);
        return unique;
    }

    public static string NormalizeContributorKey(string contributorKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contributorKey);

        var normalized = contributorKey.Trim();
        if (normalized.Contains('/') || normalized.Contains('\\'))
        {
            throw new ArgumentException("Archive contributor keys cannot contain path separators.", nameof(contributorKey));
        }

        return normalized;
    }

    internal static IReadOnlyList<ArchiveEntry> PreserveArchiveEntries(
        Score score,
        IReadOnlyList<IArchiveContributor> contributors,
        out IReadOnlyList<string> manifestExtensionKeys)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(contributors);

        var contributorKeys = new HashSet<string>(
            contributors.Select(contributor => NormalizeContributorKey(contributor.ContributorKey)),
            StringComparer.OrdinalIgnoreCase);

        var preservedState = score.GetExtension<MotifArchivePreservedStateExtension>();
        var manifestKeys = new HashSet<string>(
            preservedState?.ManifestExtensions ?? [],
            StringComparer.OrdinalIgnoreCase);
        var combinedEntries = new Dictionary<string, ArchiveEntry>(StringComparer.OrdinalIgnoreCase);

        if (preservedState is not null)
        {
            foreach (var preservedEntry in preservedState.PreservedEntries)
            {
                var normalizedPath = MotifArchivePaths.NormalizeEntryPath(preservedEntry.EntryPath);
                if (MotifArchivePaths.IsReservedEntry(normalizedPath))
                {
                    continue;
                }

                if (MotifArchivePaths.TryGetContributorKey(normalizedPath, out var preservedKey)
                    && contributorKeys.Contains(preservedKey))
                {
                    manifestKeys.Remove(preservedKey);
                    continue;
                }

                combinedEntries[normalizedPath] = new ArchiveEntry(normalizedPath, preservedEntry.Data);
                if (MotifArchivePaths.TryGetContributorKey(normalizedPath, out preservedKey))
                {
                    manifestKeys.Add(preservedKey);
                }
            }
        }

        foreach (var contributor in contributors)
        {
            var contributorKey = NormalizeContributorKey(contributor.ContributorKey);
            var entries = contributor.GetArchiveEntries(score) ?? [];
            foreach (var entry in entries)
            {
                ValidateContributorEntry(contributorKey, entry);
                var normalizedPath = MotifArchivePaths.NormalizeEntryPath(entry.EntryPath);
                combinedEntries[normalizedPath] = new ArchiveEntry(normalizedPath, entry.Data);
                manifestKeys.Add(contributorKey);
            }

            if (entries.Count == 0)
            {
                manifestKeys.Remove(contributorKey);
            }
        }

        manifestExtensionKeys = manifestKeys
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return combinedEntries.Values
            .OrderBy(entry => entry.EntryPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static void RestoreArchiveEntries(
        Score score,
        MotifArchiveManifest manifest,
        IReadOnlyList<ArchiveEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(entries);

        if (entries.Count > 0 || manifest.Extensions.Count > 0)
        {
            score.SetExtension(new MotifArchivePreservedStateExtension
            {
                PreservedEntries = entries.Select(entry => new ArchiveEntry(entry.EntryPath, entry.Data)).ToArray(),
                ManifestExtensions = manifest.Extensions.ToArray()
            });
        }

        if (entries.Count == 0)
        {
            return;
        }

        var entriesByContributor = entries
            .Select(entry => new
            {
                Entry = entry,
                HasContributor = MotifArchivePaths.TryGetContributorKey(entry.EntryPath, out var key),
                ContributorKey = key
            })
            .Where(item => item.HasContributor)
            .GroupBy(item => item.ContributorKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ArchiveEntry>)group.Select(item => item.Entry).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var contributor in GetRegisteredContributors())
        {
            var contributorKey = NormalizeContributorKey(contributor.ContributorKey);
            if (!entriesByContributor.TryGetValue(contributorKey, out var contributorEntries))
            {
                continue;
            }

            contributor.RestoreFromArchive(score, contributorEntries);
        }
    }

    private static IReadOnlyList<IArchiveContributor> DiscoverContributors()
    {
        var contributors = new List<IArchiveContributor>();
        var seenContributorTypes = new HashSet<Type>();

        foreach (var assembly in MotifAssemblyDiscovery.EnumerateCandidateAssemblies().OrderBy(a => a.FullName, StringComparer.Ordinal))
        {
            foreach (var attribute in assembly.GetCustomAttributes<MotifArchiveContributorAttribute>())
            {
                var contributorType = attribute.ContributorType;
                if (!seenContributorTypes.Add(contributorType))
                {
                    continue;
                }

                if (!typeof(IArchiveContributor).IsAssignableFrom(contributorType) || contributorType.IsAbstract)
                {
                    throw new InvalidOperationException(
                        $"Assembly '{assembly.GetName().Name}' declared archive contributor '{contributorType.FullName}', but it does not implement {nameof(IArchiveContributor)} as a concrete type.");
                }

                if (Activator.CreateInstance(contributorType, nonPublic: true) is not IArchiveContributor contributor)
                {
                    throw new InvalidOperationException(
                        $"Assembly '{assembly.GetName().Name}' declared archive contributor '{contributorType.FullName}', but Motif could not create an instance.");
                }

                ValidateContributor(contributor);
                contributors.Add(contributor);
            }
        }

        return contributors
            .OrderBy(contributor => NormalizeContributorKey(contributor.ContributorKey), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<IArchiveContributor> DeduplicateByType(IEnumerable<IArchiveContributor> contributors)
    {
        var unique = new List<IArchiveContributor>();
        var seenTypes = new HashSet<Type>();

        foreach (var contributor in contributors)
        {
            if (seenTypes.Add(contributor.GetType()))
            {
                unique.Add(contributor);
            }
        }

        return unique;
    }

    private static void EnsureDistinctContributorKeys(IReadOnlyList<IArchiveContributor> contributors)
    {
        var contributorKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var contributor in contributors)
        {
            var normalizedKey = NormalizeContributorKey(contributor.ContributorKey);
            if (contributorKeys.TryGetValue(normalizedKey, out var existingTypeName))
            {
                throw new InvalidOperationException(
                    $"Archive contributor '{contributor.GetType().FullName}' declared duplicate contributor key '{normalizedKey}', which is already used by '{existingTypeName}'.");
            }

            contributorKeys[normalizedKey] = contributor.GetType().FullName ?? contributor.GetType().Name;
        }
    }

    private static void ValidateContributor(IArchiveContributor contributor)
    {
        var normalizedKey = NormalizeContributorKey(contributor.ContributorKey);
        if (string.Equals(normalizedKey, "core", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Archive contributor key 'core' is reserved.");
        }
    }

    private static void ValidateContributorEntry(string contributorKey, ArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var normalizedPath = MotifArchivePaths.NormalizeEntryPath(entry.EntryPath);
        if (MotifArchivePaths.IsReservedEntry(normalizedPath))
        {
            throw new InvalidOperationException(
                $"Archive contributor '{contributorKey}' cannot write reserved archive entry '{normalizedPath}'.");
        }

        if (!MotifArchivePaths.IsValidContributorEntryPath(contributorKey, normalizedPath))
        {
            throw new InvalidOperationException(
                $"Archive contributor '{contributorKey}' produced '{normalizedPath}', but contributor entries must live under 'extensions/{contributorKey}*' or 'resources/{contributorKey}/'.");
        }
    }

    private sealed class ExplicitRegistration(IArchiveContributor contributor)
    {
        public IArchiveContributor Contributor { get; } = contributor;
    }

    private sealed class RegistrationHandle(ExplicitRegistration registration) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            lock (SyncRoot)
            {
                ExplicitRegistrations.Remove(registration);
            }

            disposed = true;
        }
    }
}
