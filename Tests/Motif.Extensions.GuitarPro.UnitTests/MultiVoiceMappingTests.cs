namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Models;

public class MultiVoiceMappingTests
{
    [Fact]
    public async Task Reader_maps_multiple_voices_per_measure_from_fixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "schema-reference.gp");
        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();

        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        var measureWithVoices = score.Tracks
            .SelectMany(t => t.Measures)
            .FirstOrDefault(m => m.Voices.Count > 1);

        measureWithVoices.Should().NotBeNull();
        measureWithVoices!.Voices.Count.Should().BeGreaterThanOrEqualTo(2);
        measureWithVoices.Voices[0].VoiceIndex.Should().Be(0);
        measureWithVoices.Voices[0].Beats.Should().NotBeEmpty();
        measureWithVoices.Voices.Skip(1).SelectMany(v => v.Beats).Should().NotBeEmpty();
        measureWithVoices.Beats.Select(b => b.Id).Should().Equal(measureWithVoices.Voices[0].Beats.Select(b => b.Id));
    }

    [Fact]
    public async Task Writer_round_trip_preserves_multiple_voices_per_measure()
    {
        var voice0Beat = new BeatModel
        {
            Id = 1,
            Duration = 0.25m,
            VoiceProperties = new Dictionary<string, string> { ["PartedSlur"] = "true" },
            Notes =
            [
                new NoteModel
                {
                    Id = 1,
                    MidiPitch = 60
                }
            ]
        };

        var voice1Beat = new BeatModel
        {
            Id = 2,
            Duration = 0.5m,
            VoiceDirectionTags = ["Coda"],
            Notes =
            [
                new NoteModel
                {
                    Id = 2,
                    MidiPitch = 67
                }
            ]
        };

        var score = new GuitarProScore
        {
            Title = "MultiVoice RoundTrip",
            Tracks =
            [
                new TrackModel
                {
                    Id = 0,
                    Name = "Guitar",
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 0,
                            TimeSignature = "4/4",
                            Voices =
                            [
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 0,
                                    Properties = new Dictionary<string, string> { ["PartedSlur"] = "true" },
                                    Beats = [voice0Beat]
                                },
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 1,
                                    DirectionTags = ["Coda"],
                                    Beats = [voice1Beat]
                                }
                            ],
                            Beats = [voice0Beat]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-voice-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            var measure = readBack.Tracks[0].Measures[0];
            measure.Voices.Should().HaveCount(2);
            measure.Voices[0].VoiceIndex.Should().Be(0);
            measure.Voices[1].VoiceIndex.Should().Be(1);
            measure.Voices[0].Beats.Should().ContainSingle();
            measure.Voices[1].Beats.Should().ContainSingle();
            measure.Voices[0].Beats[0].Notes[0].MidiPitch.Should().Be(60);
            measure.Voices[1].Beats[0].Notes[0].MidiPitch.Should().Be(67);
            measure.Beats.Select(b => b.Id).Should().Equal(measure.Voices[0].Beats.Select(b => b.Id));
        }
        finally
        {
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }
        }
    }
}
