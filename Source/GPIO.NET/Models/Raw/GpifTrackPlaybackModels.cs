namespace GPIO.NET.Models.Raw;

public sealed class GpifPlaybackState
{
    public string Value { get; init; } = string.Empty;
}

public sealed class GpifAudioEngineState
{
    public string Value { get; init; } = string.Empty;
}

public sealed class GpifMidiConnection
{
    public int? Port { get; init; }

    public int? PrimaryChannel { get; init; }

    public int? SecondaryChannel { get; init; }

    public bool? ForceOneChannelPerString { get; init; }
}

public sealed class GpifLyrics
{
    public bool? Dispatched { get; init; }

    public IReadOnlyList<GpifLyricsLine> Lines { get; init; } = Array.Empty<GpifLyricsLine>();
}

public sealed class GpifLyricsLine
{
    public string Text { get; init; } = string.Empty;

    public int? Offset { get; init; }
}

public sealed class GpifTranspose
{
    public int? Chromatic { get; init; }

    public int? Octave { get; init; }
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
