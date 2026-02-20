namespace GPIO.NET.Abstractions;

using GPIO.NET.Models.Raw;

public interface INavigationResolver
{
    IReadOnlyList<int> BuildPlaybackSequence(IReadOnlyList<GpifMasterBar> masterBars);
}
