namespace Motif.IntegrationTests;

internal static class GuitarProFixture
{
    public static string PathFor(string fixtureName)
        => Path.Combine(FindRepositoryRoot(), "Tests", "Fixtures", "GuitarPro", fixtureName);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Motif.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root from test output directory.");
    }
}
