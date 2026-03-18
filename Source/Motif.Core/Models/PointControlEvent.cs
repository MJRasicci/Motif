namespace Motif.Models;

/// <summary>
/// Authored point-in-time control anchored to exact written score time.
/// </summary>
public sealed class PointControlEvent
{
    /// <summary>
    /// Gets or sets the control kind.
    /// </summary>
    public PointControlKind Kind { get; set; }

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
    /// Gets or sets the exact written position of the control.
    /// </summary>
    public WrittenPosition Position { get; set; } = new();

    /// <summary>
    /// Gets or sets the text value for control kinds that use symbolic identifiers.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric value for control kinds that use quantitative payloads.
    /// </summary>
    public decimal? NumericValue { get; set; }

    /// <summary>
    /// Gets or sets an optional placement hint such as a fermata anchor label.
    /// </summary>
    public string Placement { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional length hint such as fermata playback length.
    /// </summary>
    public decimal? Length { get; set; }
}
