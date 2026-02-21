namespace GPIO.NET.Models.Raw;

public sealed class GpifPlaybackState
{
    public string Value { get; init; } = string.Empty;
}

public sealed class GpifAutomation
{
    public string Type { get; init; } = string.Empty;

    public bool? Linear { get; init; }

    public int? Bar { get; init; }

    public int? Position { get; init; }

    public bool? Visible { get; init; }

    public string Value { get; init; } = string.Empty;
}
