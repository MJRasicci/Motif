namespace Motif;

using Motif.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class ScoreJson
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Fallback JSON options path used for non-default formatting only.")]
    public static string ToJson(
        this Score score,
        bool indented = true,
        bool ignoreNullValues = false,
        bool ignoreDefaultValues = false)
    {
        ArgumentNullException.ThrowIfNull(score);

        if (indented && !ignoreNullValues && !ignoreDefaultValues)
        {
            return JsonSerializer.Serialize(score, MotifJsonContext.Default.Score);
        }

        var ignoreCondition = ignoreDefaultValues
            ? JsonIgnoreCondition.WhenWritingDefault
            : ignoreNullValues
                ? JsonIgnoreCondition.WhenWritingNull
                : JsonIgnoreCondition.Never;
        var options = CachedOptions.GetOrAdd(
            (indented, ignoreCondition),
            static key => new JsonSerializerOptions(DefaultOptions)
            {
                WriteIndented = key.Indented,
                DefaultIgnoreCondition = key.IgnoreCondition
            });
        return JsonSerializer.Serialize(score, options);
    }

    private static readonly ConcurrentDictionary<(bool Indented, JsonIgnoreCondition IgnoreCondition), JsonSerializerOptions> CachedOptions = new();
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
