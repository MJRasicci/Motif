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
            Offset = ScoreTime.Zero,
            Duration = new ScoreTime(1, 4),
            Rhythm = new RhythmValue
            {
                BaseValue = NoteValueKind.Quarter
            },
            Notes =
            [
                new Note
                {
                    Id = 200,
                    Pitch = Pitch.FromMidiNumber(64),
                    Duration = new ScoreTime(1, 4),
                    SoundingDuration = new ScoreTime(1, 2),
                    Articulation = new NoteArticulation
                    {
                        LetRing = true,
                        Relations =
                        [
                            new NoteRelation
                            {
                                Kind = NoteRelationKind.Legato,
                                TargetNoteId = 201
                            }
                        ]
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
                    Offset = ScoreTime.Zero,
                    BeatsPerMinute = 120m
                }
            ],
            PointControls =
            [
                new PointControlEvent
                {
                    Kind = PointControlKind.Dynamic,
                    Scope = ControlScopeKind.Voice,
                    TrackId = 1,
                    StaffIndex = 0,
                    VoiceIndex = 0,
                    Position = new WrittenPosition
                    {
                        BarIndex = 0,
                        Offset = ScoreTime.Zero
                    },
                    Value = "mf"
                },
                new PointControlEvent
                {
                    Kind = PointControlKind.Fermata,
                    Scope = ControlScopeKind.Score,
                    Position = new WrittenPosition
                    {
                        BarIndex = 1,
                        Offset = ScoreTime.Zero
                    },
                    Value = "Short",
                    Placement = "Middle",
                    Length = 1.2m
                }
            ],
            SpanControls =
            [
                new SpanControlEvent
                {
                    Kind = SpanControlKind.Legato,
                    Scope = ControlScopeKind.Voice,
                    TrackId = 1,
                    StaffIndex = 0,
                    VoiceIndex = 0,
                    Start = new WrittenPosition
                    {
                        BarIndex = 0,
                        Offset = ScoreTime.Zero
                    },
                    End = new WrittenPosition
                    {
                        BarIndex = 0,
                        Offset = new ScoreTime(1, 4)
                    }
                }
            ],
            PlaybackMasterBarSequence = [0, 1, 0, 1, 2],
            TimelineBars =
            [
                new TimelineBar
                {
                    Index = 0,
                    TimeSignature = "4/4",
                    Start = ScoreTime.Zero,
                    Duration = new ScoreTime(1, 1),
                    RepeatStart = true,
                    SectionLetter = "A",
                    SectionText = "Verse"
                },
                new TimelineBar
                {
                    Index = 1,
                    TimeSignature = "4/4",
                    Start = new ScoreTime(1, 1),
                    Duration = new ScoreTime(1, 1),
                    RepeatEnd = true,
                    RepeatCount = 2,
                    Jump = "DaCapoAlFine",
                    Target = "Fine"
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
        roundTripped.PointControls.Should().HaveCount(2);
        roundTripped.PointControls[0].Kind.Should().Be(PointControlKind.Dynamic);
        roundTripped.PointControls[1].Placement.Should().Be("Middle");
        roundTripped.SpanControls.Should().ContainSingle();
        roundTripped.SpanControls[0].Kind.Should().Be(SpanControlKind.Legato);
        roundTripped.PlaybackMasterBarSequence.Should().Equal(0, 1, 0, 1, 2);
        roundTripped.TimelineBars.Should().HaveCount(2);
        roundTripped.TimelineBars[0].Duration.Should().Be(new ScoreTime(1, 1));
        roundTripped.TimelineBars[0].SectionText.Should().Be("Verse");
        roundTripped.TimelineBars[1].Target.Should().Be("Fine");
        roundTripped.Tracks.Should().ContainSingle();
        roundTripped.Tracks[0].Instrument.Family.Should().Be(InstrumentFamilyKind.Guitar);
        roundTripped.Tracks[0].Instrument.Kind.Should().Be(InstrumentKind.SteelStringGuitar);
        roundTripped.Tracks[0].Transposition.Octave.Should().Be(-1);
        roundTripped.Tracks[0].Staves.Should().ContainSingle();
        roundTripped.Tracks[0].Staves[0].Tuning.Pitches.Should().Equal(40, 45, 50, 55, 59, 64);
        roundTripped.Tracks[0].Staves[0].CapoFret.Should().Be(2);
        roundTripped.Tracks[0].PrimaryMeasure().Voices.Should().ContainSingle();
        roundTripped.Tracks[0].PrimaryMeasure().Beats.Should().ContainSingle();
        roundTripped.Tracks[0].PrimaryMeasure().Beats[0].Notes.Should().ContainSingle();
        roundTripped.Tracks[0].PrimaryMeasure().Beats[0].Notes[0].Articulation.LetRing.Should().BeTrue();
        roundTripped.Tracks[0].PrimaryMeasure().Beats[0].Notes[0].Articulation.Relations.Should().ContainSingle();
        roundTripped.Tracks[0].PrimaryMeasure().Beats[0].Notes[0].SoundingDuration.Should().Be(new ScoreTime(1, 2));
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
        properties["pointControls"].GetArrayLength().Should().Be(0);
        properties["spanControls"].GetArrayLength().Should().Be(0);
        properties["playbackMasterBarSequence"].GetArrayLength().Should().Be(0);
        properties["tracks"].GetArrayLength().Should().Be(1);
    }
}
