namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Extensions.GuitarPro.Models.Raw;

internal interface IGpifSerializer
{
    ValueTask SerializeAsync(GpifDocument document, Stream output, CancellationToken cancellationToken = default);
}
