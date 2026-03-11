namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;

public class EndToEndReaderTests
{
    [Fact]
    public async Task Reader_can_open_gp_file_and_map_basic_score_structure()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        File.Exists(fixturePath).Should().BeTrue();

        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.Should().NotBeNull();
        score.Tracks.Should().NotBeEmpty();
        score.TimelineBars.Should().NotBeEmpty();
        score.PlaybackMasterBarSequence.Should().NotBeEmpty();

        var firstTrack = score.Tracks[0];
        firstTrack.Staves.Should().NotBeEmpty();
        firstTrack.Measures.Should().NotBeEmpty();

        firstTrack.Measures.SelectMany(m => m.Beats).Count().Should().BeGreaterThan(0);

        var json = score.ToJson();
        json.Should().Contain("\"Tracks\"");
        json.Should().Contain("\"Staves\"");
        json.Should().Contain("\"TimelineBars\"");
        json.Should().Contain("\"PlaybackMasterBarSequence\"");
    }
}
