namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.Text.Json;

public class GuitarProExtensionAttachmentTests
{
    [Fact]
    public async Task Reader_attaches_score_track_measure_and_voice_guitar_pro_extensions()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");

        var score = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.GetGuitarPro().Should().NotBeNull();
        score.GetRequiredGuitarPro().Metadata.ScoreXml.Should().Contain("<Score");
        score.GetRequiredGuitarPro().MasterTrack.TrackIds.Should().NotBeEmpty();

        score.Tracks.Should().NotBeEmpty();
        score.Tracks[0].GetGuitarPro().Should().NotBeNull();
        score.Tracks[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Track");
        score.Tracks[0].Measures[0].GetGuitarPro().Should().NotBeNull();
        score.Tracks[0].Measures[0].GetRequiredGuitarPro().Metadata.MasterBarXml.Should().Contain("<MasterBar");
        score.Tracks[0].Measures[0].Voices[0].GetGuitarPro().Should().NotBeNull();
        score.Tracks[0].Measures[0].Voices[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Voice");
    }

    [Fact]
    public async Task Json_round_trip_can_reattach_score_track_measure_and_voice_guitar_pro_extensions_from_source_score()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var sourceScore = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);
        var json = sourceScore.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<GuitarProScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();
        fromJson!.GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Measures[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Measures[0].Voices[0].GetGuitarPro().Should().BeNull();

        fromJson.ReattachGuitarProExtensionsFrom(sourceScore);

        fromJson.GetRequiredGuitarPro().Metadata.ScoreXml.Should().Contain("<Score");
        fromJson.GetRequiredGuitarPro().MasterTrack.TrackIds.Should().NotBeEmpty();
        fromJson.Tracks[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Track");
        fromJson.Tracks[0].Measures[0].GetRequiredGuitarPro().Metadata.MasterBarXml.Should().Contain("<MasterBar");
        fromJson.Tracks[0].Measures[0].Voices[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Voice");
    }
}
