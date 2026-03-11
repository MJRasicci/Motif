namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Models;
using System.Text.Json;

public class PublicApiSurfaceTests
{
    [Fact]
    public void Core_domain_model_and_json_helpers_are_available()
    {
        var score = new Score
        {
            Title = "Example",
            Tracks =
            [
                new Track
                {
                    Id = 1,
                    Name = "Lead"
                }
            ]
        };

        typeof(IExtensibleModel).Should().NotBeNull();
        typeof(IModelExtension).Should().NotBeNull();
        typeof(IScoreReader).Should().NotBeNull();
        typeof(IScoreWriter).Should().NotBeNull();
        typeof(ScoreNavigation).Should().NotBeNull();
        typeof(TimelineBar).Should().NotBeNull();
        typeof(Staff).Should().NotBeNull();
        typeof(StaffMeasure).Should().NotBeNull();

        typeof(TimelineBar).GetProperty("XProperties").Should().BeNull();
        typeof(StaffMeasure).GetProperty("BarProperties").Should().BeNull();
        typeof(StaffMeasure).GetProperty("BarXProperties").Should().BeNull();
        typeof(Beat).GetProperty("Wah").Should().BeNull();
        typeof(Beat).GetProperty("VibratoWithTremBarStrength").Should().BeNull();
        typeof(Beat).GetProperty("Properties").Should().BeNull();
        typeof(Beat).GetProperty("XProperties").Should().BeNull();
        typeof(Note).GetProperty("XProperties").Should().BeNull();
        typeof(NoteArticulation).GetProperty("AntiAccentValue").Should().BeNull();

        new Score().Tracks.Should().BeEmpty();
        new Score().TimelineBars.Should().BeEmpty();
        new Track().Staves.Should().BeEmpty();
        new Staff().Measures.Should().BeEmpty();

        var json = score.ToJson();
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.OrdinalIgnoreCase);

        properties["title"].GetString().Should().Be("Example");
        properties["tracks"].GetArrayLength().Should().Be(1);
    }
}
