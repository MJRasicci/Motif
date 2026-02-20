namespace GPIO.NET.Abstractions;

using GPIO.NET.Models.Raw;

public interface IGpifSerializer
{
    ValueTask SerializeAsync(GpifDocument document, Stream output, CancellationToken cancellationToken = default);
}
