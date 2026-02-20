namespace GPIO.NET.Abstractions;

public interface IGpArchiveWriter
{
    ValueTask WriteArchiveAsync(Stream gpifContent, string filePath, CancellationToken cancellationToken = default);
}
