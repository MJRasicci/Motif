namespace Motif.Extensions.GuitarPro.Serialization;

using Motif.Extensions.GuitarPro.Models;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GpMotifArchiveState))]
internal sealed partial class GpMotifArchiveJsonContext : JsonSerializerContext;
