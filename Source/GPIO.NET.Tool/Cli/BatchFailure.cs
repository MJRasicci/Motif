namespace GPIO.NET.Tool.Cli;

internal sealed class BatchFailure
{
    public required string File { get; init; }

    public required string Output { get; init; }

    public required string Error { get; init; }
}
