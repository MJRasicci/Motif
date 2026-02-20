namespace GPIO.NET.Abstractions;

using GPIO.NET.Models;

public interface IGuitarProWriter
{
    ValueTask WriteAsync(GuitarProScore score, string filePath, CancellationToken cancellationToken = default);
}
