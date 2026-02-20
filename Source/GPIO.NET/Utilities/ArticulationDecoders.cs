namespace GPIO.NET.Utilities;

using GPIO.NET.Models;
using GPIO.NET.Models.Raw;

internal static class ArticulationDecoders
{
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

    public static BendModel? DecodeBend(GpifNoteArticulation a)
    {
        if (!a.BendEnabled && a.BendOriginValue is null && a.BendDestinationValue is null && a.BendMiddleValue is null)
        {
            return null;
        }

        return new BendModel
        {
            Enabled = a.BendEnabled,
            OriginOffset = a.BendOriginOffset,
            OriginValue = a.BendOriginValue,
            MiddleOffset1 = a.BendMiddleOffset1,
            MiddleOffset2 = a.BendMiddleOffset2,
            MiddleValue = a.BendMiddleValue,
            DestinationOffset = a.BendDestinationOffset,
            DestinationValue = a.BendDestinationValue
        };
    }

    public static HarmonicModel? DecodeHarmonic(GpifNoteArticulation a)
    {
        if (!a.HarmonicEnabled && a.HarmonicType is null && a.HarmonicFret is null)
        {
            return null;
        }

        return new HarmonicModel
        {
            Enabled = a.HarmonicEnabled,
            Type = a.HarmonicType,
            Fret = a.HarmonicFret
        };
    }
}
