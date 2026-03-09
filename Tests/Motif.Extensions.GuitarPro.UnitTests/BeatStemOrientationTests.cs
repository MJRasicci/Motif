namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;
using System.Text.Json;

public class BeatStemOrientationTests
{
    [Fact]
    public async Task Mapper_and_unmapper_preserve_stem_orientation_fields_through_json_round_trip()
    {
        var sourceRaw = new GpifDocument
        {
            Score = new ScoreInfo
            {
                Title = "Stem Test",
                Artist = "GPIO",
                Album = "Tests"
            },
            MasterTrack = new GpifMasterTrack
            {
                TrackIds = [0]
            },
            Tracks =
            [
                new GpifTrack
                {
                    Id = 0,
                    Name = "Piano"
                }
            ],
            MasterBars =
            [
                new GpifMasterBar
                {
                    Index = 0,
                    Time = "4/4",
                    BarsReferenceList = "0"
                }
            ],
            BarsById = new Dictionary<int, GpifBar>
            {
                [0] = new()
                {
                    Id = 0,
                    VoicesReferenceList = "0"
                }
            },
            VoicesById = new Dictionary<int, GpifVoice>
            {
                [0] = new()
                {
                    Id = 0,
                    BeatsReferenceList = "0"
                }
            },
            BeatsById = new Dictionary<int, GpifBeat>
            {
                [0] = new()
                {
                    Id = 0,
                    RhythmRef = 0,
                    NotesReferenceList = "0",
                    TransposedPitchStemOrientation = "Undefined",
                    UserTransposedPitchStemOrientation = "Downward",
                    ConcertPitchStemOrientation = "Upward"
                }
            },
            NotesById = new Dictionary<int, GpifNote>
            {
                [0] = new()
                {
                    Id = 0,
                    MidiPitch = 60
                }
            },
            RhythmsById = new Dictionary<int, GpifRhythm>
            {
                [0] = new()
                {
                    Id = 0,
                    NoteValue = "Quarter"
                }
            }
        };

        var mapper = new DefaultScoreMapper();
        var score = await mapper.MapAsync(sourceRaw, TestContext.Current.CancellationToken);

        var beat = score.Tracks[0].Measures[0].Beats.Single();
        beat.TransposedPitchStemOrientation.Should().Be("Undefined");
        beat.UserTransposedPitchStemOrientation.Should().Be("Downward");
        beat.ConcertPitchStemOrientation.Should().Be("Upward");

        var json = score.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<GuitarProScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(fromJson!, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.BeatsById[0].TransposedPitchStemOrientation.Should().Be("Undefined");
        result.RawDocument.BeatsById[0].UserTransposedPitchStemOrientation.Should().Be("Downward");
        result.RawDocument.BeatsById[0].ConcertPitchStemOrientation.Should().Be("Upward");
    }
}
