namespace GPIO.NET.Abstractions;

using GPIO.NET.Models;
using GPIO.NET.Models.Raw;

public interface IScoreUnmapper
{
    ValueTask<GpifDocument> UnmapAsync(GuitarProScore score, CancellationToken cancellationToken = default);
}
