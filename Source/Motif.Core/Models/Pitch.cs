namespace Motif.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Sounding pitch spelling made up of step, accidental, and octave.
/// </summary>
public sealed record Pitch
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
    /// Gets or sets the octave number using Motif's GP-compatible octave numbering.
    /// </summary>
    public int Octave { get; set; }

    /// <summary>
    /// Gets the resolved sounding MIDI note number for the pitch.
    /// </summary>
    [JsonIgnore]
    public int MidiNumber => (Octave * 12) + ResolvePitchClass(Step, Accidental);

    /// <summary>
    /// Creates a canonical sharp-based pitch spelling from a MIDI note number.
    /// </summary>
    public static Pitch FromMidiNumber(int midiNumber)
    {
        var octave = midiNumber / 12;
        var pitchClass = midiNumber % 12;

        return pitchClass switch
        {
            0 => new Pitch { Step = "C", Accidental = string.Empty, Octave = octave },
            1 => new Pitch { Step = "C", Accidental = "#", Octave = octave },
            2 => new Pitch { Step = "D", Accidental = string.Empty, Octave = octave },
            3 => new Pitch { Step = "D", Accidental = "#", Octave = octave },
            4 => new Pitch { Step = "E", Accidental = string.Empty, Octave = octave },
            5 => new Pitch { Step = "F", Accidental = string.Empty, Octave = octave },
            6 => new Pitch { Step = "F", Accidental = "#", Octave = octave },
            7 => new Pitch { Step = "G", Accidental = string.Empty, Octave = octave },
            8 => new Pitch { Step = "G", Accidental = "#", Octave = octave },
            9 => new Pitch { Step = "A", Accidental = string.Empty, Octave = octave },
            10 => new Pitch { Step = "A", Accidental = "#", Octave = octave },
            11 => new Pitch { Step = "B", Accidental = string.Empty, Octave = octave },
            _ => throw new ArgumentOutOfRangeException(nameof(midiNumber))
        };
    }

    /// <summary>
    /// Returns a new pitch transposed by the supplied chromatic interval.
    /// </summary>
    public Pitch TransposeChromatically(int semitones)
        => FromMidiNumber(MidiNumber + semitones);

    private static int ResolvePitchClass(string step, string accidental)
    {
        var stepClass = step.Trim().ToUpperInvariant() switch
        {
            "C" => 0,
            "D" => 2,
            "E" => 4,
            "F" => 5,
            "G" => 7,
            "A" => 9,
            "B" => 11,
            _ => throw new InvalidOperationException($"Unsupported pitch step '{step}'.")
        };

        return accidental.Trim().ToUpperInvariant() switch
        {
            "" => stepClass,
            "#" => stepClass + 1,
            "##" => stepClass + 2,
            "B" => stepClass - 1,
            "BB" => stepClass - 2,
            "N" => stepClass,
            _ => throw new InvalidOperationException($"Unsupported pitch accidental '{accidental}'.")
        };
    }
}
