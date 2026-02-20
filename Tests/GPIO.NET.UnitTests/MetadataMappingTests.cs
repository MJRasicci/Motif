namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Models;

public class MetadataMappingTests
{
    [Fact]
    public async Task Reader_maps_score_and_track_metadata_from_fixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "schema-reference.gp");
        var reader = new GPIO.NET.GuitarProReader();

        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.Metadata.SubTitle.Should().NotBeNullOrWhiteSpace();
        score.Metadata.Copyright.Should().NotBeNullOrWhiteSpace();
        score.Metadata.Notices.Should().NotBeNullOrWhiteSpace();

        score.Tracks.Should().NotBeEmpty();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.ShortName)).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.Color)).Should().BeTrue();
    }

    [Fact]
    public async Task Writer_round_trip_preserves_custom_score_and_track_metadata()
    {
        var score = new GuitarProScore
        {
            Title = "T",
            Artist = "A",
            Album = "B",
            Metadata = new ScoreMetadata
            {
                SubTitle = "Sub",
                Copyright = "(c) test",
                Notices = "notice",
                Instructions = "instructions",
                ScoreZoomPolicy = "Value",
                ScoreZoom = "1.5"
            },
            Tracks =
            [
                new TrackModel
                {
                    Id = 0,
                    Name = "Guitar",
                    Metadata = new TrackMetadata
                    {
                        ShortName = "gtr",
                        Color = "255 0 0",
                        SystemsDefaultLayout = "3",
                        SystemsLayout = "3 3",
                        PalmMute = 0.3m,
                        AutoAccentuation = 0.2m,
                        AutoBrush = true,
                        PlayingStyle = "StringedPick",
                        UseOneChannelPerString = true,
                        IconId = 1,
                        ForcedSound = -1
                    },
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 0,
                            TimeSignature = "4/4",
                            Beats = [ new BeatModel { Id = 1, Duration = 0.25m } ]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-meta-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new GPIO.NET.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var reader = new GPIO.NET.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            readBack.Metadata.SubTitle.Should().Be("Sub");
            readBack.Metadata.Copyright.Should().Be("(c) test");
            readBack.Metadata.Notices.Should().Be("notice");
            readBack.Metadata.Instructions.Should().Be("instructions");
            readBack.Metadata.ScoreZoomPolicy.Should().Be("Value");
            readBack.Metadata.ScoreZoom.Should().Be("1.5");

            var track = readBack.Tracks[0];
            track.Metadata.ShortName.Should().Be("gtr");
            track.Metadata.Color.Should().Be("255 0 0");
            track.Metadata.SystemsDefaultLayout.Should().Be("3");
            track.Metadata.SystemsLayout.Should().Be("3 3");
            track.Metadata.PalmMute.Should().Be(0.3m);
            track.Metadata.AutoAccentuation.Should().Be(0.2m);
            track.Metadata.AutoBrush.Should().BeTrue();
            track.Metadata.PlayingStyle.Should().Be("StringedPick");
            track.Metadata.UseOneChannelPerString.Should().BeTrue();
            track.Metadata.IconId.Should().Be(1);
            track.Metadata.ForcedSound.Should().Be(-1);
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
