namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;

public class WriterDiagnosticsTests
{
    [Fact]
    public async Task Unmapper_emits_structured_warning_for_unrepresentable_duration()
    {
        var score = new GuitarProScore
        {
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
                            Beats = [ new BeatModel { Id = 1, Duration = 0.17m } ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().NotBeEmpty();
        result.Diagnostics.Warnings[0].Code.Should().Be("RHYTHM_APPROXIMATED");
        result.Diagnostics.Warnings[0].Category.Should().Be("Rhythm");
    }
}
