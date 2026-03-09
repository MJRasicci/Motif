namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Models;

public class GuitarProExtensionAttachmentTests
{
    [Fact]
    public async Task Reader_attaches_score_and_track_guitar_pro_extensions()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");

        var score = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.GetGuitarPro().Should().NotBeNull();
        score.GetRequiredGuitarPro().Metadata.Should().BeSameAs(score.Metadata);
        score.GetRequiredGuitarPro().MasterTrack.Should().BeSameAs(score.MasterTrack);

        score.Tracks.Should().NotBeEmpty();
        score.Tracks[0].GetGuitarPro().Should().NotBeNull();
        score.Tracks[0].GetRequiredGuitarPro().Metadata.Should().BeSameAs(score.Tracks[0].Metadata);
    }
}
