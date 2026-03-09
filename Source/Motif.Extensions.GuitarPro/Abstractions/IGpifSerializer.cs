namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Extensions.GuitarPro.Models.Raw;

public interface IGpifSerializer
{
    ValueTask SerializeAsync(GpifDocument document, Stream output, CancellationToken cancellationToken = default);
}
