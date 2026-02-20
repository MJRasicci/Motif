namespace GPIO.NET.UnitTests;

using FluentAssertions;

public class EndToEndReaderTests
{
    [Fact]
    public async Task Reader_can_open_gp_file_and_map_basic_score_structure()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.gp");
        File.Exists(fixturePath).Should().BeTrue();

        var reader = new GPIO.NET.GuitarProReader();
        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.Should().NotBeNull();
        score.Tracks.Should().NotBeEmpty();

        var firstTrack = score.Tracks[0];
        firstTrack.Measures.Should().NotBeEmpty();

        firstTrack.Measures.SelectMany(m => m.Beats).Count().Should().BeGreaterThan(0);
    }
}
