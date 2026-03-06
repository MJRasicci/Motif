namespace GPIO.NET.Utilities;

using GPIO.NET.Models;
using GPIO.NET.Models.Raw;

internal static class ArticulationDecoders
{
    private const decimal BendValueScale = 50m;
    private const decimal BendOffsetScale = 100m;
    private const decimal Epsilon = 0.0001m;

    public static IReadOnlyList<SlideType> DecodeSlides(int? flags)
    {
        if (!flags.HasValue || flags.Value <= 0)
        {
            return Array.Empty<SlideType>();
        }

        var f = flags.Value;
        var slides = new List<SlideType>();

        if ((f & 1) != 0) slides.Add(SlideType.Shift);
        if ((f & 2) != 0) slides.Add(SlideType.Legato);
        if ((f & 4) != 0) slides.Add(SlideType.OutDown);
        if ((f & 8) != 0) slides.Add(SlideType.OutUp);
        if ((f & 16) != 0) slides.Add(SlideType.IntoFromBelow);
        if ((f & 32) != 0) slides.Add(SlideType.IntoFromAbove);
        if ((f & 64) != 0) slides.Add(SlideType.Unknown64);
        if ((f & 128) != 0) slides.Add(SlideType.Unknown128);

        return slides;
    }

    public static int? EncodeSlides(IReadOnlyList<SlideType> slides)
    {
        if (slides.Count == 0)
        {
            return null;
        }

        var flags = 0;
        foreach (var slide in slides)
        {
            flags |= slide switch
            {
                SlideType.Shift => 1,
                SlideType.Legato => 2,
                SlideType.OutDown => 4,
                SlideType.OutUp => 8,
                SlideType.IntoFromBelow => 16,
                SlideType.IntoFromAbove => 32,
                SlideType.Unknown64 => 64,
                SlideType.Unknown128 => 128,
                _ => 0
            };
        }

        return flags == 0 ? null : flags;
    }

    public static BendModel? DecodeBend(GpifNoteArticulation a, bool tieDestination)
    {
        var hasCurveData = a.BendOriginValue.HasValue
            || a.BendMiddleValue.HasValue
            || a.BendDestinationValue.HasValue
            || a.BendOriginOffset.HasValue
            || a.BendMiddleOffset1.HasValue
            || a.BendMiddleOffset2.HasValue
            || a.BendDestinationOffset.HasValue;

        if (!a.BendEnabled && !hasCurveData)
        {
            return null;
        }

        var originValue = NormalizeBendValue(a.BendOriginValue);
        var middleValue = NormalizeBendValue(a.BendMiddleValue);
        var destinationValue = NormalizeBendValue(a.BendDestinationValue);
        var inferredType = InferBendType(originValue, middleValue, destinationValue, tieDestination, a.BendEnabled);

        return new BendModel
        {
            Enabled = a.BendEnabled || hasCurveData,
            Type = inferredType,
            OriginOffset = NormalizeBendOffset(a.BendOriginOffset),
            OriginValue = originValue,
            MiddleOffset1 = NormalizeBendOffset(a.BendMiddleOffset1),
            MiddleOffset2 = NormalizeBendOffset(a.BendMiddleOffset2),
            MiddleValue = middleValue,
            DestinationOffset = NormalizeBendOffset(a.BendDestinationOffset),
            DestinationValue = destinationValue
        };
    }

    public static EncodedBend EncodeBend(BendModel? bend)
    {
        if (bend is null)
        {
            return default;
        }

        return new EncodedBend(
            Enabled: bend.Enabled || bend.Type is not BendTypeKind.None and not BendTypeKind.Unknown,
            OriginOffset: DenormalizeBendOffset(bend.OriginOffset),
            OriginValue: DenormalizeBendValue(bend.OriginValue),
            MiddleOffset1: DenormalizeBendOffset(bend.MiddleOffset1),
            MiddleOffset2: DenormalizeBendOffset(bend.MiddleOffset2),
            MiddleValue: DenormalizeBendValue(bend.MiddleValue),
            DestinationOffset: DenormalizeBendOffset(bend.DestinationOffset),
            DestinationValue: DenormalizeBendValue(bend.DestinationValue));
    }

    public static HarmonicModel? DecodeHarmonic(GpifNoteArticulation a)
    {
        if (!a.HarmonicEnabled
            && a.HarmonicType is null
            && string.IsNullOrWhiteSpace(a.HarmonicTypeText)
            && a.HarmonicFret is null)
        {
            return null;
        }

        var kind = ParseHarmonicTypeKind(a.HarmonicTypeText, a.HarmonicType);
        var typeNumber = a.HarmonicType ?? MapHarmonicTypeNumber(kind);
        var typeName = !string.IsNullOrWhiteSpace(a.HarmonicTypeText)
            ? a.HarmonicTypeText
            : MapHarmonicTypeName(kind);

        return new HarmonicModel
        {
            Enabled = a.HarmonicEnabled || a.HarmonicFret.HasValue || kind != HarmonicTypeKind.NoHarmonic,
            Type = typeNumber,
            TypeName = typeName,
            Kind = kind,
            Fret = a.HarmonicFret
        };
    }

    public static EncodedHarmonic EncodeHarmonic(HarmonicModel? harmonic)
    {
        if (harmonic is null)
        {
            return default;
        }

        var typeNumber = harmonic.Type ?? MapHarmonicTypeNumber(harmonic.Kind);
        var typeText = !string.IsNullOrWhiteSpace(harmonic.TypeName)
            ? harmonic.TypeName
            : MapHarmonicTypeName(harmonic.Kind);

        return new EncodedHarmonic(
            Enabled: harmonic.Enabled || harmonic.Fret.HasValue || typeNumber.HasValue || !string.IsNullOrWhiteSpace(typeText),
            TypeNumber: typeNumber,
            TypeText: typeText,
            Fret: harmonic.Fret);
    }

    private static BendTypeKind InferBendType(
        decimal? originValue,
        decimal? middleValue,
        decimal? destinationValue,
        bool tieDestination,
        bool bendEnabled)
    {
        if (!originValue.HasValue && !middleValue.HasValue && !destinationValue.HasValue)
        {
            return bendEnabled ? BendTypeKind.Bend : BendTypeKind.None;
        }

        var origin = originValue ?? 0m;
        var destination = destinationValue ?? origin;
        var middle = middleValue ?? ((origin + destination) / 2m);

        if (Close(origin, middle) && Close(middle, destination))
        {
            return tieDestination ? BendTypeKind.Hold : BendTypeKind.Prebend;
        }

        if (origin <= middle + Epsilon && middle <= destination + Epsilon)
        {
            return tieDestination || Close(origin, 0m)
                ? BendTypeKind.Bend
                : BendTypeKind.PrebendAndBend;
        }

        if (origin + Epsilon >= middle && middle + Epsilon >= destination)
        {
            return tieDestination
                ? BendTypeKind.Release
                : BendTypeKind.PrebendAndRelease;
        }

        if (origin <= middle + Epsilon && middle + Epsilon >= destination)
        {
            return BendTypeKind.BendAndRelease;
        }

        return BendTypeKind.Unknown;
    }

    private static HarmonicTypeKind ParseHarmonicTypeKind(string? typeText, int? typeNumber)
    {
        if (!string.IsNullOrWhiteSpace(typeText))
        {
            return typeText.Trim().ToUpperInvariant() switch
            {
                "NOHARMONIC" => HarmonicTypeKind.NoHarmonic,
                "NATURAL" => HarmonicTypeKind.Natural,
                "ARTIFICIAL" => HarmonicTypeKind.Artificial,
                "PINCH" => HarmonicTypeKind.Pinch,
                "TAP" => HarmonicTypeKind.Tap,
                "SEMI" => HarmonicTypeKind.Semi,
                "FEEDBACK" => HarmonicTypeKind.Feedback,
                _ => HarmonicTypeKind.Unknown
            };
        }

        if (!typeNumber.HasValue)
        {
            return HarmonicTypeKind.Unknown;
        }

        return typeNumber.Value switch
        {
            0 => HarmonicTypeKind.NoHarmonic,
            1 => HarmonicTypeKind.Natural,
            2 => HarmonicTypeKind.Artificial,
            3 => HarmonicTypeKind.Pinch,
            4 => HarmonicTypeKind.Tap,
            5 => HarmonicTypeKind.Semi,
            6 => HarmonicTypeKind.Feedback,
            _ => HarmonicTypeKind.Unknown
        };
    }

    private static int? MapHarmonicTypeNumber(HarmonicTypeKind kind)
        => kind switch
        {
            HarmonicTypeKind.NoHarmonic => 0,
            HarmonicTypeKind.Natural => 1,
            HarmonicTypeKind.Artificial => 2,
            HarmonicTypeKind.Pinch => 3,
            HarmonicTypeKind.Tap => 4,
            HarmonicTypeKind.Semi => 5,
            HarmonicTypeKind.Feedback => 6,
            _ => null
        };

    private static string MapHarmonicTypeName(HarmonicTypeKind kind)
        => kind switch
        {
            HarmonicTypeKind.NoHarmonic => "NoHarmonic",
            HarmonicTypeKind.Natural => "Natural",
            HarmonicTypeKind.Artificial => "Artificial",
            HarmonicTypeKind.Pinch => "Pinch",
            HarmonicTypeKind.Tap => "Tap",
            HarmonicTypeKind.Semi => "Semi",
            HarmonicTypeKind.Feedback => "Feedback",
            _ => string.Empty
        };

    private static decimal? NormalizeBendValue(decimal? value)
        => value / BendValueScale;

    private static decimal? NormalizeBendOffset(decimal? value)
        => value / BendOffsetScale;

    private static decimal? DenormalizeBendValue(decimal? value)
        => value * BendValueScale;

    private static decimal? DenormalizeBendOffset(decimal? value)
        => value * BendOffsetScale;

    private static bool Close(decimal left, decimal right)
        => Math.Abs(left - right) <= Epsilon;

    public static WhammyBarModel? DecodeWhammyBar(GpifBeat beat)
    {
        var hasCurveData = beat.WhammyBarOriginValue.HasValue
            || beat.WhammyBarMiddleValue.HasValue
            || beat.WhammyBarDestinationValue.HasValue
            || beat.WhammyBarOriginOffset.HasValue
            || beat.WhammyBarMiddleOffset1.HasValue
            || beat.WhammyBarMiddleOffset2.HasValue
            || beat.WhammyBarDestinationOffset.HasValue;

        if (!beat.WhammyBar && !beat.WhammyBarExtended && !hasCurveData)
        {
            return null;
        }

        return new WhammyBarModel
        {
            Enabled = beat.WhammyBar || beat.WhammyBarExtended || hasCurveData,
            Extended = beat.WhammyBarExtended,
            OriginValue = beat.WhammyBarOriginValue / BendValueScale,
            MiddleValue = beat.WhammyBarMiddleValue / BendValueScale,
            DestinationValue = beat.WhammyBarDestinationValue / BendValueScale,
            OriginOffset = beat.WhammyBarOriginOffset / BendOffsetScale,
            MiddleOffset1 = beat.WhammyBarMiddleOffset1 / BendOffsetScale,
            MiddleOffset2 = beat.WhammyBarMiddleOffset2 / BendOffsetScale,
            DestinationOffset = beat.WhammyBarDestinationOffset / BendOffsetScale
        };
    }

    public static EncodedWhammyBar EncodeWhammyBar(WhammyBarModel? whammy)
    {
        if (whammy is null)
        {
            return default;
        }

        return new EncodedWhammyBar(
            Enabled: whammy.Enabled,
            Extended: whammy.Extended,
            OriginValue: whammy.OriginValue * BendValueScale,
            MiddleValue: whammy.MiddleValue * BendValueScale,
            DestinationValue: whammy.DestinationValue * BendValueScale,
            OriginOffset: whammy.OriginOffset * BendOffsetScale,
            MiddleOffset1: whammy.MiddleOffset1 * BendOffsetScale,
            MiddleOffset2: whammy.MiddleOffset2 * BendOffsetScale,
            DestinationOffset: whammy.DestinationOffset * BendOffsetScale);
    }

    /// <summary>
    /// Decodes trill speed from XProperty id 688062467.
    /// Thresholds: &gt;=240 → Sixteenth, &gt;=120 → ThirtySecond, &gt;=60 → SixtyFourth, lower values → OneHundredTwentyEighth.
    /// </summary>
    public static TrillSpeedKind DecodeTrillSpeed(IReadOnlyDictionary<string, int>? noteXProperties)
    {
        if (noteXProperties is null || !noteXProperties.TryGetValue("688062467", out var value))
        {
            return TrillSpeedKind.None;
        }

        return value switch
        {
            >= 240 => TrillSpeedKind.Sixteenth,
            >= 120 => TrillSpeedKind.ThirtySecond,
            >= 60 => TrillSpeedKind.SixtyFourth,
            _ => TrillSpeedKind.OneHundredTwentyEighth
        };
    }

    public static int? EncodeTrillSpeed(TrillSpeedKind speed)
        => speed switch
        {
            TrillSpeedKind.Sixteenth => 240,
            TrillSpeedKind.ThirtySecond => 120,
            TrillSpeedKind.SixtyFourth => 60,
            TrillSpeedKind.OneHundredTwentyEighth => 30,
            _ => null
        };
}

internal readonly record struct EncodedBend(
    bool Enabled,
    decimal? OriginOffset,
    decimal? OriginValue,
    decimal? MiddleOffset1,
    decimal? MiddleOffset2,
    decimal? MiddleValue,
    decimal? DestinationOffset,
    decimal? DestinationValue);

internal readonly record struct EncodedHarmonic(
    bool Enabled,
    int? TypeNumber,
    string TypeText,
    decimal? Fret);

internal readonly record struct EncodedWhammyBar(
    bool Enabled,
    bool Extended,
    decimal? OriginValue,
    decimal? MiddleValue,
    decimal? DestinationValue,
    decimal? OriginOffset,
    decimal? MiddleOffset1,
    decimal? MiddleOffset2,
    decimal? DestinationOffset);
