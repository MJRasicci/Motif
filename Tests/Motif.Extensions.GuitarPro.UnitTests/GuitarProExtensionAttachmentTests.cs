namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Extensions.GuitarPro.Models.Raw;
using Motif.Models;
using System.Text.Json;

public class GuitarProExtensionAttachmentTests
{
    [Fact]
    public async Task Reader_attaches_score_track_measure_voice_beat_and_note_guitar_pro_extensions()
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
        score.Tracks[0].Measures[0].Beats[0].GetGuitarPro().Should().NotBeNull();
        score.Tracks[0].Measures[0].Beats[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Beat");
        score.Tracks[0].Measures[0].Beats[0].Notes[0].GetGuitarPro().Should().NotBeNull();
        score.Tracks[0].Measures[0].Beats[0].Notes[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Note");
    }

    [Fact]
    public async Task Mapper_attaches_staff_and_staff_measure_guitar_pro_extensions_for_multistaff_tracks()
    {
        var score = await new DefaultScoreMapper().MapAsync(CreateMultiStaffRawDocument(), TestContext.Current.CancellationToken);

        var piano = score.Tracks.Single(track => track.Name == "Piano");

        piano.Staves.Should().HaveCount(2);
        piano.Staves[0].GetRequiredGuitarPro().Metadata.Id.Should().Be(0);
        piano.Staves[1].GetRequiredGuitarPro().Metadata.Cref.Should().Be("65");
        piano.Staves[0].Measures.Should().ContainSingle();
        piano.Staves[0].Measures[0].GetRequiredGuitarPro().Metadata.SourceBarId.Should().Be(0);
        piano.Staves[1].Measures[0].GetRequiredGuitarPro().Metadata.SourceBarId.Should().Be(1);
    }

    [Fact]
    public async Task Json_round_trip_can_reattach_score_track_measure_voice_beat_and_note_guitar_pro_extensions_from_source_score()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var sourceScore = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);
        var json = sourceScore.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<Score>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();
        fromJson!.GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Measures[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Measures[0].Voices[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Measures[0].Beats[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Measures[0].Beats[0].Notes[0].GetGuitarPro().Should().BeNull();

        var reattachment = fromJson.ReattachGuitarProExtensionsFrom(sourceScore);

        fromJson.GetRequiredGuitarPro().Metadata.ScoreXml.Should().Contain("<Score");
        fromJson.GetRequiredGuitarPro().MasterTrack.TrackIds.Should().NotBeEmpty();
        fromJson.Tracks[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Track");
        fromJson.Tracks[0].Measures[0].GetRequiredGuitarPro().Metadata.MasterBarXml.Should().Contain("<MasterBar");
        fromJson.Tracks[0].Measures[0].Voices[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Voice");
        fromJson.Tracks[0].Measures[0].Beats[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Beat");
        fromJson.Tracks[0].Measures[0].Beats[0].Notes[0].GetRequiredGuitarPro().Metadata.Xml.Should().Contain("<Note");
        reattachment.ScoreAttached.Should().BeTrue();
        reattachment.HasUnmatchedTargets.Should().BeFalse();
        reattachment.TracksAttached.Should().Be(fromJson.Tracks.Count);
        reattachment.MeasuresAttached.Should().Be(fromJson.Tracks.Sum(track => track.Measures.Count));
        reattachment.VoicesAttached.Should().BeGreaterThan(0);
        reattachment.BeatsAttached.Should().BeGreaterThan(0);
        reattachment.NotesAttached.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Json_round_trip_can_reattach_guitar_pro_extensions_for_staff_only_tracks()
    {
        var sourceScore = await new DefaultScoreMapper().MapAsync(CreateMultiStaffRawDocument(), TestContext.Current.CancellationToken);
        var json = sourceScore.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<Score>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();
        foreach (var track in fromJson!.Tracks)
        {
            track.Measures = [];
        }

        fromJson.Tracks[0].Staves[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Staves[0].Measures[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Staves[0].Measures[0].Voices[0].GetGuitarPro().Should().BeNull();
        fromJson.Tracks[0].Staves[0].Measures[0].Voices[0].Beats[0].GetGuitarPro().Should().BeNull();

        var reattachment = fromJson.ReattachGuitarProExtensionsFrom(sourceScore);

        fromJson.Tracks[0].Staves[0].GetRequiredGuitarPro().Metadata.Id.Should().Be(0);
        fromJson.Tracks[0].Staves[1].GetRequiredGuitarPro().Metadata.Cref.Should().Be("65");
        fromJson.Tracks[0].Staves[0].Measures[0].GetRequiredGuitarPro().Metadata.SourceBarId.Should().Be(0);
        fromJson.Tracks[0].Staves[0].Measures[0].Voices[0].GetRequiredGuitarPro().Metadata.SourceVoiceId.Should().Be(10);
        fromJson.Tracks[0].Staves[0].Measures[0].Voices[0].Beats[0].GetGuitarPro().Should().NotBeNull();
        fromJson.Tracks[0].Staves[0].Measures[0].Voices[0].Beats[0].Notes[0].GetGuitarPro().Should().NotBeNull();
        reattachment.StaffsAttached.Should().BeGreaterThan(0);
        reattachment.VoicesAttached.Should().BeGreaterThan(0);
        reattachment.BeatsAttached.Should().BeGreaterThan(0);
        reattachment.NotesAttached.Should().BeGreaterThan(0);
        reattachment.HasUnmatchedTargets.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateGuitarProExtensions_removes_existing_extensions_from_the_full_score_tree()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var score = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.InvalidateGuitarProExtensions();

        score.GetGuitarPro().Should().BeNull();
        score.Tracks[0].GetGuitarPro().Should().BeNull();
        score.Tracks[0].Staves[0].GetGuitarPro().Should().BeNull();
        score.Tracks[0].Measures[0].GetGuitarPro().Should().BeNull();
        score.Tracks[0].Staves[0].Measures[0].GetGuitarPro().Should().BeNull();
        score.Tracks[0].Measures[0].Voices[0].GetGuitarPro().Should().BeNull();
        score.Tracks[0].Measures[0].Beats[0].GetGuitarPro().Should().BeNull();
        score.Tracks[0].Measures[0].Beats[0].Notes[0].GetGuitarPro().Should().BeNull();
    }

    [Fact]
    public async Task ReattachGuitarProExtensionsFrom_matches_measures_by_index_and_clears_stale_target_extensions()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var sourceScore = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);
        var sourceTrack = sourceScore.Tracks[0];
        var sourceMeasure = sourceTrack.Measures[0];

        var target = new Score
        {
            Tracks =
            [
                new TrackModel
                {
                    Id = sourceTrack.Id,
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 999,
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = -1,
                                    Notes =
                                    [
                                        new NoteModel
                                        {
                                            Id = -2
                                        }
                                    ]
                                }
                            ]
                        },
                        new MeasureModel
                        {
                            Index = sourceMeasure.Index,
                            Beats = sourceMeasure.Beats.Select(CloneBeat).ToArray()
                        }
                    ]
                }
            ]
        };
        target.Tracks[0].Measures[0].SetExtension(new GpMeasureExtension
        {
            Metadata = new GpMeasureMetadata
            {
                MasterBarXml = "<stale />"
            }
        });

        var reattachment = target.ReattachGuitarProExtensionsFrom(sourceScore);

        reattachment.MeasuresAttached.Should().Be(1);
        reattachment.MeasuresUnmatched.Should().BeGreaterThan(0);
        reattachment.HasUnmatchedTargets.Should().BeTrue();
        target.Tracks[0].Measures[1].GetRequiredGuitarPro().Metadata.MasterBarXml
            .Should().Be(sourceMeasure.GetRequiredGuitarPro().Metadata.MasterBarXml);
        target.Tracks[0].Measures[0].GetGuitarPro().Should().BeNull();
    }

    private static BeatModel CloneBeat(BeatModel beat)
        => new()
        {
            Id = beat.Id,
            Notes = beat.Notes.Select(note => new NoteModel
            {
                Id = note.Id
            }).ToArray()
        };

    private static GpifDocument CreateMultiStaffRawDocument()
        => new()
        {
            Score = new ScoreInfo
            {
                Title = "MultiStaff",
                Artist = "GPIO",
                Album = "Tests"
            },
            MasterTrack = new GpifMasterTrack
            {
                TrackIds = [9, 10]
            },
            Tracks =
            [
                new GpifTrack
                {
                    Id = 9,
                    Name = "Piano",
                    Staffs =
                    [
                        new GpifStaff { Id = 0, Cref = "64" },
                        new GpifStaff { Id = 1, Cref = "65" }
                    ]
                },
                new GpifTrack
                {
                    Id = 10,
                    Name = "Drums",
                    Staffs =
                    [
                        new GpifStaff { Id = 2, Cref = "128" }
                    ]
                }
            ],
            MasterBars =
            [
                new GpifMasterBar
                {
                    Index = 0,
                    Time = "4/4",
                    BarsReferenceList = "0 1 2"
                }
            ],
            BarsById = new Dictionary<int, GpifBar>
            {
                [0] = new()
                {
                    Id = 0,
                    VoicesReferenceList = "10",
                    Clef = "G2"
                },
                [1] = new()
                {
                    Id = 1,
                    VoicesReferenceList = "11",
                    Clef = "F4"
                },
                [2] = new()
                {
                    Id = 2,
                    VoicesReferenceList = "12",
                    Clef = "Neutral"
                }
            },
            VoicesById = new Dictionary<int, GpifVoice>
            {
                [10] = new()
                {
                    Id = 10,
                    BeatsReferenceList = "100"
                },
                [11] = new()
                {
                    Id = 11,
                    BeatsReferenceList = "101"
                },
                [12] = new()
                {
                    Id = 12,
                    BeatsReferenceList = "102"
                }
            },
            BeatsById = new Dictionary<int, GpifBeat>
            {
                [100] = new()
                {
                    Id = 100,
                    RhythmRef = 1000,
                    NotesReferenceList = "200"
                },
                [101] = new()
                {
                    Id = 101,
                    RhythmRef = 1000,
                    NotesReferenceList = "201"
                },
                [102] = new()
                {
                    Id = 102,
                    RhythmRef = 1000,
                    NotesReferenceList = "202"
                }
            },
            NotesById = new Dictionary<int, GpifNote>
            {
                [200] = new()
                {
                    Id = 200,
                    MidiPitch = 60
                },
                [201] = new()
                {
                    Id = 201,
                    MidiPitch = 48
                },
                [202] = new()
                {
                    Id = 202,
                    MidiPitch = 35
                }
            },
            RhythmsById = new Dictionary<int, GpifRhythm>
            {
                [1000] = new()
                {
                    Id = 1000,
                    NoteValue = "Quarter"
                }
            }
        };
}
