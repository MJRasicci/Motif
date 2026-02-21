namespace GPIO.NET.Models.Patching;

public sealed class JsonPatchPlanResult
{
    public required GpPatchDocument Patch { get; init; }

    public IReadOnlyList<string> UnsupportedChanges { get; init; } = Array.Empty<string>();
}
