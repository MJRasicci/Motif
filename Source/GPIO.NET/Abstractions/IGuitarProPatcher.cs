namespace GPIO.NET.Abstractions;

using GPIO.NET.Models.Patching;

public interface IGuitarProPatcher
{
    ValueTask<PatchResult> PatchAsync(string sourceGpPath, string outputGpPath, GpPatchDocument patch, CancellationToken cancellationToken = default);
}
