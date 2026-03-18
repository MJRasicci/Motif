namespace Motif.Models;

/// <summary>
/// Authored span control anchored to written start and optional end positions.
/// </summary>
public sealed class SpanControlEvent
{
    /// <summary>
    /// Gets or sets the control kind.
    /// </summary>
    public SpanControlKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the target scope for the control.
    /// </summary>
    public ControlScopeKind Scope { get; set; } = ControlScopeKind.Score;

    /// <summary>
    /// Gets or sets the owning track identifier when the scope is track-local or narrower.
    /// </summary>
    public int? TrackId { get; set; }

    /// <summary>
    /// Gets or sets the owning staff index when the scope is staff-local or narrower.
    /// </summary>
    public int? StaffIndex { get; set; }

    /// <summary>
    /// Gets or sets the owning voice index when the scope is voice-local.
    /// </summary>
    public int? VoiceIndex { get; set; }

    /// <summary>
    /// Gets or sets the span start position.
    /// </summary>
    public WrittenPosition Start { get; set; } = new();

    /// <summary>
    /// Gets or sets the span end position when the source or caller can resolve one.
    /// </summary>
    public WrittenPosition? End { get; set; }

    /// <summary>
    /// Gets or sets the symbolic value associated with the span.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
