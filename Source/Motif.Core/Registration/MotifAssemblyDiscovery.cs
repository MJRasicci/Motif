namespace Motif;

using System.Reflection;

internal static class MotifAssemblyDiscovery
{
    public static IReadOnlyCollection<Assembly> EnumerateCandidateAssemblies()
    {
        var assembliesByName = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            TryAddAssembly(assembliesByName, assembly);
        }

        // In single-file publish mode, companion DLLs are bundled inside the host
        // and won't appear on disk.  Walk referenced-assembly metadata from loaded
        // assemblies and load any Motif-prefixed references by name so the runtime
        // resolves them from the bundle.
        LoadReferencedMotifAssemblies(assembliesByName);

        var baseDirectory = AppContext.BaseDirectory;
        if (!Directory.Exists(baseDirectory))
        {
            return assembliesByName.Values;
        }

        foreach (var path in Directory.EnumerateFiles(baseDirectory, "Motif*.dll", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            AssemblyName assemblyName;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(path);
            }
            catch (BadImageFormatException)
            {
                continue;
            }
            catch (FileLoadException)
            {
                continue;
            }

            if (assembliesByName.Values.Any(loaded => AssemblyName.ReferenceMatchesDefinition(loaded.GetName(), assemblyName)))
            {
                continue;
            }

            try
            {
                TryAddAssembly(assembliesByName, Assembly.Load(assemblyName));
            }
            catch
            {
                // Ignore optional Motif companion assemblies that are not loadable in the current app.
            }
        }

        return assembliesByName.Values;
    }

    private static void LoadReferencedMotifAssemblies(Dictionary<string, Assembly> assembliesByName)
    {
        // Seed the search from every assembly already loaded (including the entry
        // assembly), but only follow and load references whose names start with "Motif".
        var queue = new Queue<Assembly>(assembliesByName.Values);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in queue)
        {
            var name = assembly.GetName().Name;
            if (!string.IsNullOrEmpty(name))
            {
                visited.Add(name);
            }
        }

        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            foreach (var referencedName in assembly.GetReferencedAssemblies())
            {
                if (referencedName.Name is null
                    || !referencedName.Name.StartsWith("Motif", StringComparison.OrdinalIgnoreCase)
                    || !visited.Add(referencedName.Name))
                {
                    continue;
                }

                try
                {
                    var loaded = Assembly.Load(referencedName);
                    TryAddAssembly(assembliesByName, loaded);
                    queue.Enqueue(loaded);
                }
                catch
                {
                    // Ignore optional Motif companion assemblies that are not loadable.
                }
            }
        }
    }

    private static void TryAddAssembly(Dictionary<string, Assembly> assembliesByName, Assembly assembly)
    {
        var identity = assembly.FullName;
        if (string.IsNullOrWhiteSpace(identity))
        {
            identity = assembly.GetName().Name ?? assembly.Location;
        }

        if (!string.IsNullOrWhiteSpace(identity) && !assembliesByName.ContainsKey(identity))
        {
            assembliesByName[identity] = assembly;
        }
    }
}
