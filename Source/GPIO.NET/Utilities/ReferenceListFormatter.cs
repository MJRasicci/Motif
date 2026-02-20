namespace GPIO.NET.Utilities;

internal static class ReferenceListFormatter
{
    public static string JoinRefs(IEnumerable<int> ids) => string.Join(' ', ids);
}
