namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Models;
using System.Text.Json;

public class RoundTripFidelityTests
{
    [Theory]
    [InlineData("test.gp")]
    [InlineData("schema-reference.gp")]
    public async Task Round_trip_preserves_core_structural_invariants(string fixtureName)
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        File.Exists(fixturePath).Should().BeTrue();

        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
        var original = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        var tempGp = Path.Combine(Path.GetTempPath(), $"gpio-rt-{Path.GetFileNameWithoutExtension(fixtureName)}-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(original, tempGp, TestContext.Current.CancellationToken);

            File.Exists(tempGp).Should().BeTrue();

            var roundTripped = await reader.ReadAsync(tempGp, cancellationToken: TestContext.Current.CancellationToken);

            AssertCoreInvariants(original, roundTripped);
        }
        finally
        {
            if (File.Exists(tempGp))
            {
                File.Delete(tempGp);
            }
        }
    }

    [Fact]
    public async Task Round_trip_json_pipeline_preserves_core_structural_invariants()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "schema-reference.gp");
        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
        var original = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        var json = original.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<GuitarProScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();

        var tempGp = Path.Combine(Path.GetTempPath(), $"gpio-rt-json-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(fromJson!, tempGp, TestContext.Current.CancellationToken);

            var roundTripped = await reader.ReadAsync(tempGp, cancellationToken: TestContext.Current.CancellationToken);
            AssertCoreInvariants(original, roundTripped);
        }
        finally
        {
            if (File.Exists(tempGp))
            {
                File.Delete(tempGp);
            }
        }
    }

    private static void AssertCoreInvariants(GuitarProScore original, GuitarProScore roundTripped)
    {
        roundTripped.Tracks.Count.Should().Be(original.Tracks.Count);
        roundTripped.PlaybackMasterBarSequence.Count.Should().BeGreaterThan(0);

        var oTrackMetrics = BuildTrackMetrics(original);
        var rTrackMetrics = BuildTrackMetrics(roundTripped);

        rTrackMetrics.Should().HaveCount(oTrackMetrics.Count);
        for (var i = 0; i < oTrackMetrics.Count; i++)
        {
            rTrackMetrics[i].Measures.Should().Be(oTrackMetrics[i].Measures);
            rTrackMetrics[i].Beats.Should().Be(oTrackMetrics[i].Beats);
            rTrackMetrics[i].Notes.Should().Be(oTrackMetrics[i].Notes);
            rTrackMetrics[i].SlideNotes.Should().Be(oTrackMetrics[i].SlideNotes);
            rTrackMetrics[i].HarmonicNotes.Should().Be(oTrackMetrics[i].HarmonicNotes);
            rTrackMetrics[i].BendNotes.Should().Be(oTrackMetrics[i].BendNotes);
            rTrackMetrics[i].TieOriginNotes.Should().Be(oTrackMetrics[i].TieOriginNotes);
            rTrackMetrics[i].TieDestinationNotes.Should().Be(oTrackMetrics[i].TieDestinationNotes);
        }
    }

    private static IReadOnlyList<TrackMetrics> BuildTrackMetrics(GuitarProScore score)
        => score.Tracks
            .OrderBy(t => t.Id)
            .Select(t =>
            {
                var notes = t.Measures.SelectMany(m => m.Beats).SelectMany(b => b.Notes).ToArray();
                return new TrackMetrics(
                    Measures: t.Measures.Count,
                    Beats: t.Measures.SelectMany(m => m.Beats).Count(),
                    Notes: notes.Length,
                    SlideNotes: notes.Count(n => n.Articulation.Slides.Count > 0),
                    HarmonicNotes: notes.Count(n => n.Articulation.Harmonic is not null),
                    BendNotes: notes.Count(n => n.Articulation.Bend is not null),
                    TieOriginNotes: notes.Count(n => n.Articulation.TieOrigin),
                    TieDestinationNotes: notes.Count(n => n.Articulation.TieDestination));
            })
            .ToArray();

    private sealed record TrackMetrics(
        int Measures,
        int Beats,
        int Notes,
        int SlideNotes,
        int HarmonicNotes,
        int BendNotes,
        int TieOriginNotes,
        int TieDestinationNotes);
}
