namespace Motif;

using Motif.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class GuitarProScoreJson
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Fallback JSON options path used for non-default formatting only.")]
    public static string ToJson(
        this GuitarProScore score,
        bool indented = true,
        bool ignoreNullValues = false,
        bool ignoreDefaultValues = false)
    {
        ArgumentNullException.ThrowIfNull(score);

        if (indented && !ignoreNullValues && !ignoreDefaultValues)
        {
            return JsonSerializer.Serialize(score, MotifJsonContext.Default.GuitarProScore);
        }

        var options = new JsonSerializerOptions(DefaultOptions)
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = ignoreDefaultValues
                ? JsonIgnoreCondition.WhenWritingDefault
                : ignoreNullValues
                    ? JsonIgnoreCondition.WhenWritingNull
                    : JsonIgnoreCondition.Never
        };

        return JsonSerializer.Serialize(score, options);
    }

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
