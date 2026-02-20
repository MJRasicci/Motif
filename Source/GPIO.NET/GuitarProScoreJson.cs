namespace GPIO.NET;

using GPIO.NET.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class GuitarProScoreJson
{
    public static string ToJson(
        this GuitarProScore score,
        bool indented = true,
        bool ignoreNullValues = false,
        bool ignoreDefaultValues = false)
    {
        ArgumentNullException.ThrowIfNull(score);

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
