namespace Motif.Extensions.GuitarPro.Abstractions;

using Motif.Extensions.GuitarPro.Models.Raw;

public interface INavigationResolver
{
    IReadOnlyList<int> BuildPlaybackSequence(IReadOnlyList<GpifMasterBar> masterBars, bool anacrusis = false);
}
