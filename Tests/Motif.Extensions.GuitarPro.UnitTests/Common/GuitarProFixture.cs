namespace Motif.Extensions.GuitarPro.UnitTests;

internal static class GuitarProFixture
{
    public static string PathFor(string fixtureName)
        => Path.Combine(AppContext.BaseDirectory, "Fixtures", "GuitarPro", fixtureName);
}
