namespace Motif.Extensions.GuitarPro.Utilities;

internal static class ReferenceListParser
{
    public static List<int> SplitRefs(string refs)
        => SplitRefsInternal(refs, keepNegativePlaceholders: false);

    public static List<int> SplitRefsPreservePlaceholders(string refs)
        => SplitRefsInternal(refs, keepNegativePlaceholders: true);

    private static List<int> SplitRefsInternal(string refs, bool keepNegativePlaceholders)
        => refs
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : -1)
            .Where(value => keepNegativePlaceholders || value >= 0)
            .ToList();
}
