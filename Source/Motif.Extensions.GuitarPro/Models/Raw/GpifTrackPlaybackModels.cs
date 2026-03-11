namespace Motif.Extensions.GuitarPro.Models.Raw;

internal sealed class GpifPlaybackState
{
    public string Value { get; init; } = string.Empty;
}

internal sealed class GpifAudioEngineState
{
    public string Value { get; init; } = string.Empty;
}

internal sealed class GpifMidiConnection
{
    public int? Port { get; init; }

    public int? PrimaryChannel { get; init; }

    public int? SecondaryChannel { get; init; }

    public bool? ForceOneChannelPerString { get; init; }
}

internal sealed class GpifLyrics
{
    public bool? Dispatched { get; init; }

    public IReadOnlyList<GpifLyricsLine> Lines { get; init; } = Array.Empty<GpifLyricsLine>();
}

internal sealed class GpifLyricsLine
{
    public string Text { get; init; } = string.Empty;

    public int? Offset { get; init; }
}

internal sealed class GpifTranspose
{
    public int? Chromatic { get; init; }

    public int? Octave { get; init; }
}

internal sealed class GpifAutomation
{
    public string Type { get; init; } = string.Empty;

    public bool? Linear { get; init; }

    public int? Bar { get; init; }

    public int? Position { get; init; }

    public bool? Visible { get; init; }

    public string Value { get; init; } = string.Empty;
}
