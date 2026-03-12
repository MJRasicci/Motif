namespace Motif.Models;

/// <summary>
/// Written pitch spelling made up of step, accidental, and octave.
/// </summary>
public sealed class PitchValue
{
    /// <summary>
    /// Gets or sets the diatonic pitch step, such as <c>C</c>.
    /// </summary>
    public string Step { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accidental text associated with the pitch.
    /// </summary>
    public string Accidental { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the octave number, when available.
    /// </summary>
    public int? Octave { get; set; }
}
