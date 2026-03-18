namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Models;
using System.Text.Json;

public class ScoreJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Score_can_round_trip_through_json()
    {
        var beat = new Beat
        {
            Id = 100,
            Dynamic = "mf",
            FreeText = "push",
            Offset = 0m,
            Duration = 0.25m,
            Notes =
            [
                new Note
                {
                    Id = 200,
                    Pitch = Pitch.FromMidiNumber(64),
                    Duration = 0.25m,
                    Articulation = new NoteArticulation
                    {
                        LetRing = true
                    }
                }
            ]
        };

        var source = new Score
        {
            Title = "Json Fixture",
            Artist = "Motif",
            Album = "Tests",
            Anacrusis = true,
            TempoChanges =
            [
                new TempoChange
                {
                    BarIndex = 0,
                    Position = 0,
                    BeatsPerMinute = 120m
                }
            ],
            PlaybackMasterBarSequence = [0, 1, 0, 1, 2],
            TimelineBars =
            [
                new TimelineBar
                {
                    Index = 0,
                    TimeSignature = "4/4",
                    RepeatStart = true,
                    SectionLetter = "A",
                    SectionText = "Verse"
                },
                new TimelineBar
                {
                    Index = 1,
                    TimeSignature = "4/4",
                    RepeatEnd = true,
                    RepeatCount = 2,
                    Jump = "DaCapoAlFine",
                    DirectionProperties = new Dictionary<string, string>
                    {
                        ["Fine"] = "1"
                    }
                }
            ],
            Tracks =
            [
                new Track
                {
                    Id = 1,
                    Name = "Lead",
                    Instrument = new TrackInstrument
                    {
                        Family = InstrumentFamilyKind.Guitar,
                        Kind = InstrumentKind.SteelStringGuitar,
                        Role = TrackRoleKind.Pitched
                    },
                    Transposition = new TrackTransposition
                    {
                        IsSpecified = true,
                        Chromatic = 0,
                        Octave = -1
                    },
                    Staves =
                    [
                        new Staff
                        {
                            StaffIndex = 0,
                            Tuning = new StaffTuning
                            {
                                Label = "Standard",
                                Pitches = [40, 45, 50, 55, 59, 64]
                            },
                            CapoFret = 2,
                            Measures =
                            [
                                new StaffMeasure
                                {
                                    Index = 0,
                                    StaffIndex = 0,
                                    Clef = "Treble",
                                    Voices =
                                    [
                                        new Voice
                                        {
                                            VoiceIndex = 0,
                                            Beats = [beat]
                                        }
                                    ],
                                    Beats = [beat]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var json = source.ToJson(indented: false);
        var roundTripped = JsonSerializer.Deserialize<Score>(json, JsonOptions);

        roundTripped.Should().NotBeNull();
        roundTripped!.Title.Should().Be(source.Title);
        roundTripped.Artist.Should().Be(source.Artist);
        roundTripped.Anacrusis.Should().BeTrue();
        roundTripped.TempoChanges.Should().ContainSingle();
        roundTripped.TempoChanges[0].BeatsPerMinute.Should().Be(120m);
        roundTripped.PlaybackMasterBarSequence.Should().Equal(0, 1, 0, 1, 2);
        roundTripped.TimelineBars.Should().HaveCount(2);
        roundTripped.TimelineBars[0].SectionText.Should().Be("Verse");
        roundTripped.TimelineBars[1].DirectionProperties["Fine"].Should().Be("1");
        roundTripped.Tracks.Should().ContainSingle();
        roundTripped.Tracks[0].Instrument.Family.Should().Be(InstrumentFamilyKind.Guitar);
        roundTripped.Tracks[0].Instrument.Kind.Should().Be(InstrumentKind.SteelStringGuitar);
        roundTripped.Tracks[0].Transposition.IsSpecified.Should().BeTrue();
        roundTripped.Tracks[0].Transposition.Octave.Should().Be(-1);
        roundTripped.Tracks[0].Staves.Should().ContainSingle();
        roundTripped.Tracks[0].Staves[0].Tuning.Pitches.Should().Equal(40, 45, 50, 55, 59, 64);
        roundTripped.Tracks[0].Staves[0].CapoFret.Should().Be(2);
        roundTripped.Tracks[0].PrimaryMeasure().Voices.Should().ContainSingle();
        roundTripped.Tracks[0].PrimaryMeasure().Beats.Should().ContainSingle();
        roundTripped.Tracks[0].PrimaryMeasure().Beats[0].Notes.Should().ContainSingle();
        roundTripped.Tracks[0].PrimaryMeasure().Beats[0].Notes[0].Articulation.LetRing.Should().BeTrue();
    }

    [Fact]
    public void Score_json_uses_camel_case_property_names()
    {
        var score = new Score
        {
            Title = "Example",
            TimelineBars =
            [
                new TimelineBar
                {
                    Index = 0,
                    TimeSignature = "4/4"
                }
            ],
            Tracks =
            [
                new Track
                {
                    Id = 1,
                    Name = "Lead"
                }
            ]
        };

        var json = score.ToJson();
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.OrdinalIgnoreCase);

        properties["title"].GetString().Should().Be("Example");
        properties["timelineBars"].GetArrayLength().Should().Be(1);
        properties["tempoChanges"].GetArrayLength().Should().Be(0);
        properties["playbackMasterBarSequence"].GetArrayLength().Should().Be(0);
        properties["tracks"].GetArrayLength().Should().Be(1);
    }
}
