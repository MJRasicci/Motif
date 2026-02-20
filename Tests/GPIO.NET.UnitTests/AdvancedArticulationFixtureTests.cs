namespace GPIO.NET.UnitTests;

using FluentAssertions;

public class AdvancedArticulationFixtureTests
{
    [Fact]
    public async Task Schema_reference_fixture_exposes_slide_bend_and_harmonic_data()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "schema-reference.gp");
        File.Exists(fixturePath).Should().BeTrue();

        var reader = new GPIO.NET.GuitarProReader();
        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        var notes = score.Tracks
            .SelectMany(t => t.Measures)
            .SelectMany(m => m.Beats)
            .SelectMany(b => b.Notes)
            .ToArray();

        notes.Should().NotBeEmpty();
        notes.Any(n => n.Articulation.Slides.Count > 0).Should().BeTrue();
        notes.Any(n => n.Articulation.Bend is not null).Should().BeTrue();
        notes.Any(n => n.Articulation.Harmonic is not null).Should().BeTrue();

        var seenSlideFlags = notes
            .Select(n => n.Articulation.SlideFlags)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .OrderBy(v => v)
            .ToArray();

        seenSlideFlags.Should().Contain(new[] { 1, 2, 4, 8, 16, 32, 64, 128 });
    }
}
