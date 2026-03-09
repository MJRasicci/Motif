namespace Motif.Extensions.GuitarPro.Utilities;

internal static class ReferenceListFormatter
{
    public static string JoinRefs(IEnumerable<int> ids) => string.Join(' ', ids);
}
