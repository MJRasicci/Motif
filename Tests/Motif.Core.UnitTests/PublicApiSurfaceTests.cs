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
        var score = new GuitarProScore
        {
            Title = "Example",
            Tracks =
            [
                new TrackModel
                {
                    Id = 1,
                    Name = "Lead"
                }
            ]
        };

        new GuitarProScore().Tracks.Should().BeEmpty();
        new TrackModel().Measures.Should().BeEmpty();

        var json = score.ToJson();
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.OrdinalIgnoreCase);

        properties["title"].GetString().Should().Be("Example");
        properties["tracks"].GetArrayLength().Should().Be(1);
    }
}
