namespace Motif.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Semantic written rhythm shape for a beat.
/// </summary>
public sealed class RhythmValue
{
    /// <summary>
    /// Gets or sets the base note value before dots and tuplets.
    /// </summary>
    public NoteValueKind BaseValue { get; set; } = NoteValueKind.Unknown;

    /// <summary>
    /// Gets or sets the number of augmentation dots.
    /// </summary>
    public int AugmentationDots { get; set; }

    /// <summary>
    /// Gets or sets the primary tuplet ratio when present.
    /// </summary>
    public TupletRatio? PrimaryTuplet { get; set; }

    /// <summary>
    /// Gets or sets the secondary tuplet ratio when present.
    /// </summary>
    public TupletRatio? SecondaryTuplet { get; set; }

    /// <summary>
    /// Gets the exact written duration implied by the rhythm shape.
    /// </summary>
    [JsonIgnore]
    public ScoreTime Duration => ResolveDuration(this);

    /// <summary>
    /// Resolves the exact written duration implied by the supplied rhythm shape.
    /// </summary>
    public static ScoreTime ResolveDuration(RhythmValue? rhythm)
    {
        if (rhythm is null)
        {
            return ScoreTime.Zero;
        }

        var duration = rhythm.BaseValue switch
        {
            NoteValueKind.Whole => new ScoreTime(1, 1),
            NoteValueKind.Half => new ScoreTime(1, 2),
            NoteValueKind.Quarter => new ScoreTime(1, 4),
            NoteValueKind.Eighth => new ScoreTime(1, 8),
            NoteValueKind.Sixteenth => new ScoreTime(1, 16),
            NoteValueKind.ThirtySecond => new ScoreTime(1, 32),
            NoteValueKind.SixtyFourth => new ScoreTime(1, 64),
            NoteValueKind.OneHundredTwentyEighth => new ScoreTime(1, 128),
            NoteValueKind.TwoHundredFiftySixth => new ScoreTime(1, 256),
            _ => ScoreTime.Zero
        };

        if (duration == ScoreTime.Zero)
        {
            return duration;
        }

        var dotExtension = duration;
        for (var i = 0; i < rhythm.AugmentationDots; i++)
        {
            dotExtension = dotExtension.Multiply(1, 2);
            duration += dotExtension;
        }

        duration = ApplyTuplet(duration, rhythm.PrimaryTuplet);
        duration = ApplyTuplet(duration, rhythm.SecondaryTuplet);
        return duration;
    }

    private static ScoreTime ApplyTuplet(ScoreTime duration, TupletRatio? tuplet)
    {
        if (tuplet is null || tuplet.Numerator <= 0 || tuplet.Denominator <= 0)
        {
            return duration;
        }

        return duration.Multiply(tuplet.Denominator, tuplet.Numerator);
    }
}
