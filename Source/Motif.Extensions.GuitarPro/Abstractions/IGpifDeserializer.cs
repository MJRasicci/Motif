namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Extensions.GuitarPro.Models.Raw;

/// <summary>
/// Deserializes GPIF XML into a raw object model that preserves source structure.
/// </summary>
internal interface IGpifDeserializer
{
    ValueTask<GpifDocument> DeserializeAsync(Stream scoreStream, CancellationToken cancellationToken = default);
}
