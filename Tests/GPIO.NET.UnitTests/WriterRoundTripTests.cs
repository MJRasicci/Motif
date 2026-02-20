namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Models;

public class WriterRoundTripTests
{
    [Fact]
    public async Task Writer_creates_gp_archive_that_reader_can_open()
    {
        var score = new GuitarProScore
        {
            Title = "RoundTrip",
            Artist = "GPIO",
            Album = "Tests",
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
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 1,
                                    Duration = 0.25m,
                                    Notes =
                                    [
                                        new NoteModel
                                        {
                                            Id = 1,
                                            MidiPitch = 64,
                                            Articulation = new NoteArticulationModel { LetRing = true }
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-roundtrip-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new GPIO.NET.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            File.Exists(outFile).Should().BeTrue();

            var reader = new GPIO.NET.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            readBack.Title.Should().Be("RoundTrip");
            readBack.Tracks.Should().HaveCount(1);
            readBack.Tracks[0].Measures.Should().NotBeEmpty();
            readBack.Tracks[0].Measures[0].Beats.Should().NotBeEmpty();
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
