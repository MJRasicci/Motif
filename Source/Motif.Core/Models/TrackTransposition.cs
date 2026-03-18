namespace Motif.Models;

/// <summary>
/// Track-level transposition expressed as chromatic and octave offsets.
/// </summary>
public sealed class TrackTransposition
{
    /// <summary>
    /// Gets or sets a value indicating whether the transposition was explicitly authored.
    /// </summary>
    public bool IsSpecified { get; set; }

    /// <summary>
    /// Gets or sets the chromatic transposition component.
    /// </summary>
    public int Chromatic { get; set; }

    /// <summary>
    /// Gets or sets the octave transposition component.
    /// </summary>
    public int Octave { get; set; }

    /// <summary>
    /// Gets the chromatic interval between sounding pitch and written pitch.
    /// </summary>
    public int WrittenMinusSoundingSemitones => Chromatic - (Octave * 12);

    /// <summary>
    /// Transposes a sounding pitch into its written staff pitch for this track.
    /// </summary>
    public Pitch ToWrittenPitch(Pitch soundingPitch)
    {
        ArgumentNullException.ThrowIfNull(soundingPitch);
        return soundingPitch.TransposeChromatically(WrittenMinusSoundingSemitones);
    }

    /// <summary>
    /// Transposes a written staff pitch into its sounding pitch for this track.
    /// </summary>
    public Pitch ToSoundingPitch(Pitch writtenPitch)
    {
        ArgumentNullException.ThrowIfNull(writtenPitch);
        return writtenPitch.TransposeChromatically(-WrittenMinusSoundingSemitones);
    }
}
