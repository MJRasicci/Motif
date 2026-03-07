namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

public class WriterReferenceReuseTests
{
    [Fact]
    public async Task Unmapper_preserves_shared_reference_counts_for_schema_reference_fixture()
    {
        var sourceGp = FixturePath("schema-reference.gp");
        File.Exists(sourceGp).Should().BeTrue();

        var reader = new GPIO.NET.GuitarProReader();
        var score = await reader.ReadAsync(sourceGp, cancellationToken: TestContext.Current.CancellationToken);
        var sourceRaw = await ReadRawAsync(sourceGp);

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        AssertStructuralCountsEqual(sourceRaw, result.RawDocument);
        result.RawDocument.VoicesById.Values.Sum(v => SplitRefs(v.BeatsReferenceList).Count)
            .Should().Be(sourceRaw.VoicesById.Values.Sum(v => SplitRefs(v.BeatsReferenceList).Count));
        result.RawDocument.BeatsById.Values.Sum(b => SplitRefs(b.NotesReferenceList).Count)
            .Should().Be(sourceRaw.BeatsById.Values.Sum(b => SplitRefs(b.NotesReferenceList).Count));
    }

    [Fact]
    public async Task Unmapper_reuses_shared_beats_notes_and_rhythms_when_ids_repeat()
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
                            Voices =
                            [
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 0,
                                    SourceVoiceId = 0,
                                    Beats =
                                    [
                                        CreateBeat(0, CreateNote(0, 48), CreateNote(1, 52)),
                                        CreateBeat(0, CreateNote(0, 48), CreateNote(1, 52)),
                                        CreateBeat(0, CreateNote(0, 48), CreateNote(1, 52)),
                                        CreateBeat(1, CreateNote(2, 55))
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.BeatsById.Should().HaveCount(2);
        result.RawDocument.NotesById.Should().HaveCount(3);
        result.RawDocument.RhythmsById.Should().HaveCount(1);

        var voice = result.RawDocument.VoicesById[0];
        voice.BeatsReferenceList.Should().Be("0 0 0 1");

        result.RawDocument.BeatsById[0].NotesReferenceList.Should().Be("0 1");
        result.RawDocument.BeatsById[1].NotesReferenceList.Should().Be("2");
    }

    [Fact]
    public async Task Unmapper_reuses_remapped_note_aliases_when_conflicting_source_ids_repeat()
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
                            Voices =
                            [
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 0,
                                    SourceVoiceId = 0,
                                    Beats =
                                    [
                                        CreateBeat(10, CreateNote(0, 48)),
                                        CreateBeat(11, CreateNote(0, 50)),
                                        CreateBeat(11, CreateNote(0, 50))
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Count(w => w.Code == "NOTE_ID_CONFLICT").Should().Be(1);
        result.Diagnostics.Warnings.Select(w => w.Code).Should().NotContain("BEAT_ID_CONFLICT");
        result.RawDocument.NotesById.Should().HaveCount(2);
        result.RawDocument.BeatsById.Should().HaveCount(2);
        result.RawDocument.VoicesById[0].BeatsReferenceList.Should().Be("10 11 11");
        result.RawDocument.BeatsById[10].NotesReferenceList.Should().Be("0");
        result.RawDocument.BeatsById[11].NotesReferenceList.Should().Be("1");
    }

    [Fact]
    public async Task Unmapper_does_not_split_shared_note_ids_when_beat_palm_mute_is_derived_from_other_notes()
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
                            Voices =
                            [
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 0,
                                    SourceVoiceId = 0,
                                    Beats =
                                    [
                                        CreateBeatWithPalmMute(0, true, CreateNote(0, 48), CreateNote(1, 52, palmMuted: true)),
                                        CreateBeat(2, CreateNote(0, 48), CreateNote(2, 55))
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.NotesById.Should().HaveCount(3);
        result.RawDocument.BeatsById.Should().HaveCount(2);
        result.RawDocument.BeatsById[0].NotesReferenceList.Should().Be("0 1");
        result.RawDocument.BeatsById[2].NotesReferenceList.Should().Be("0 2");
        result.RawDocument.NotesById[0].Articulation.PalmMuted.Should().BeFalse();
        result.RawDocument.NotesById[1].Articulation.PalmMuted.Should().BeTrue();
    }

    [Fact]
    public async Task Unmapper_preserves_measure_with_zero_voices_without_synthesizing_empty_voice()
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
                            SourceBarId = 5,
                            Voices = [],
                            Beats = []
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.VoicesById.Should().BeEmpty();
        result.RawDocument.BarsById.Should().ContainKey(5);
        result.RawDocument.BarsById[5].VoicesReferenceList.Should().Be("-1 -1 -1 -1");
        result.RawDocument.MasterBars.Should().ContainSingle();
        result.RawDocument.MasterBars[0].BarsReferenceList.Should().Be("5");
    }

    [Fact]
    public async Task Unmapper_does_not_copy_master_bar_xproperties_onto_written_bars()
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
                            XProperties = new Dictionary<string, int>
                            {
                                ["687931393"] = 60
                            }
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.MasterBars.Should().ContainSingle();
        result.RawDocument.MasterBars[0].XProperties.Should().Contain("687931393", 60);
        result.RawDocument.BarsById.Should().ContainSingle();
        result.RawDocument.BarsById.Values.Single().XProperties.Should().BeEmpty();
    }

    [Fact]
    public async Task Mapper_maps_multistaff_master_bar_slots_without_shifting_following_tracks()
    {
        var mapper = new DefaultScoreMapper();
        var score = await mapper.MapAsync(CreateMultiStaffRawDocument(), TestContext.Current.CancellationToken);

        var piano = score.Tracks.Single(track => track.Name == "Piano");
        var drums = score.Tracks.Single(track => track.Name == "Drums");

        piano.Measures.Should().ContainSingle();
        piano.Measures[0].SourceBarId.Should().Be(0);
        piano.Measures[0].Clef.Should().Be("G2");
        piano.Measures[0].AdditionalStaffBars.Should().ContainSingle();
        piano.Measures[0].AdditionalStaffBars[0].StaffIndex.Should().Be(1);
        piano.Measures[0].AdditionalStaffBars[0].SourceBarId.Should().Be(1);
        piano.Measures[0].AdditionalStaffBars[0].Clef.Should().Be("F4");

        drums.Measures.Should().ContainSingle();
        drums.Measures[0].SourceBarId.Should().Be(2);
        drums.Measures[0].Clef.Should().Be("Neutral");
    }

    [Fact]
    public async Task Json_round_trip_preserves_multistaff_master_bar_slot_counts()
    {
        var sourceRaw = CreateMultiStaffRawDocument();
        var roundTrippedRaw = await ReadJsonRoundTrippedRawAsync(sourceRaw);

        AssertStructuralCountsEqual(sourceRaw, roundTrippedRaw);
        SplitRefs(roundTrippedRaw.MasterBars[0].BarsReferenceList).Should().Equal([0, 1, 2]);
        roundTrippedRaw.BarsById[0].Clef.Should().Be("G2");
        roundTrippedRaw.BarsById[1].Clef.Should().Be("F4");
        roundTrippedRaw.BarsById[2].Clef.Should().Be("Neutral");
    }

    [Fact]
    public void To_json_preserves_source_rhythm_shape()
    {
        var json = CreateTupletScore().ToJson();

        json.Should().Contain("\"SourceRhythm\"");
        json.Should().Contain("\"PrimaryTuplet\"");
        json.Should().Contain("\"NoteValue\": \"32nd\"");
    }

    [Fact]
    public async Task Json_round_trip_preserves_tuplet_rhythm_shapes_for_schema_reference_fixture()
    {
        var sourceGp = FixturePath("schema-reference.gp");
        var sourceRaw = await ReadRawAsync(sourceGp);
        var roundTrippedRaw = await ReadJsonRoundTrippedRawAsync(sourceGp);

        var expectedTuplets = sourceRaw.RhythmsById.Values
            .Where(r => r.PrimaryTuplet is not null || r.SecondaryTuplet is not null)
            .OrderBy(r => r.Id)
            .Select(CreateRhythmSignature)
            .ToArray();

        var actualTuplets = roundTrippedRaw.RhythmsById.Values
            .Where(r => r.PrimaryTuplet is not null || r.SecondaryTuplet is not null)
            .OrderBy(r => r.Id)
            .Select(CreateRhythmSignature)
            .ToArray();

        actualTuplets.Should().BeEquivalentTo(expectedTuplets, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Json_round_trip_preserves_raw_counts_for_schema_reference_fixture()
    {
        var sourceGp = FixturePath("schema-reference.gp");
        var sourceRaw = await ReadRawAsync(sourceGp);
        var roundTrippedRaw = await ReadJsonRoundTrippedRawAsync(sourceGp);

        AssertStructuralCountsEqual(sourceRaw, roundTrippedRaw);
    }

    private static BeatModel CreateBeat(int id, params NoteModel[] notes)
        => new()
        {
            Id = id,
            Duration = 0.25m,
            Notes = notes
        };

    private static BeatModel CreateBeatWithPalmMute(int id, bool palmMuted, params NoteModel[] notes)
        => new()
        {
            Id = id,
            PalmMuted = palmMuted,
            Duration = 0.25m,
            Notes = notes
        };

    private static NoteModel CreateNote(int id, int midiPitch, bool palmMuted = false)
        => new()
        {
            Id = id,
            MidiPitch = midiPitch,
            Articulation = new NoteArticulationModel
            {
                PalmMuted = palmMuted
            }
        };

    private static string FixturePath(string fixtureName)
        => Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);

    private static async Task<GpifDocument> ReadRawAsync(string gpPath)
    {
        using var archive = ZipFile.OpenRead(gpPath);
        var entry = archive.GetEntry("Content/score.gpif");
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        return await new XmlGpifDeserializer().DeserializeAsync(stream, TestContext.Current.CancellationToken);
    }

    private static async Task<GpifDocument> ReadJsonRoundTrippedRawAsync(string gpPath)
    {
        var reader = new GPIO.NET.GuitarProReader();
        var score = await reader.ReadAsync(gpPath, cancellationToken: TestContext.Current.CancellationToken);
        return await ReadJsonRoundTrippedRawAsync(score);
    }

    private static async Task<GpifDocument> ReadJsonRoundTrippedRawAsync(GpifDocument sourceRaw)
    {
        var score = await new DefaultScoreMapper().MapAsync(sourceRaw, TestContext.Current.CancellationToken);
        return await ReadJsonRoundTrippedRawAsync(score);
    }

    private static async Task<GpifDocument> ReadJsonRoundTrippedRawAsync(GuitarProScore score)
    {
        var json = score.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<GuitarProScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();

        var unmapper = new DefaultScoreUnmapper();
        var writeResult = await unmapper.UnmapAsync(fromJson!, TestContext.Current.CancellationToken);
        writeResult.Diagnostics.Warnings.Should().BeEmpty();
        return writeResult.RawDocument;
    }

    private static void AssertStructuralCountsEqual(GpifDocument expected, GpifDocument actual)
    {
        actual.BarsById.Should().HaveCount(expected.BarsById.Count);
        actual.VoicesById.Should().HaveCount(expected.VoicesById.Count);
        actual.BeatsById.Should().HaveCount(expected.BeatsById.Count);
        actual.NotesById.Should().HaveCount(expected.NotesById.Count);
        actual.RhythmsById.Should().HaveCount(expected.RhythmsById.Count);
        actual.MasterBars.Select(masterBar => SplitRefs(masterBar.BarsReferenceList).Count)
            .Should().Equal(expected.MasterBars.Select(masterBar => SplitRefs(masterBar.BarsReferenceList).Count));
    }

    private static object CreateRhythmSignature(GpifRhythm rhythm)
        => new
        {
            rhythm.Id,
            rhythm.NoteValue,
            rhythm.AugmentationDots,
            Primary = CreateTupletSignature(rhythm.PrimaryTuplet),
            Secondary = CreateTupletSignature(rhythm.SecondaryTuplet)
        };

    private static string? CreateTupletSignature(TupletRatio? tuplet)
        => tuplet is null
            ? null
            : $"{tuplet.Numerator}/{tuplet.Denominator}";

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

    private static GuitarProScore CreateTupletScore()
        => new()
        {
            Tracks =
            [
                new TrackModel
                {
                    Id = 0,
                    Name = "Lead Guitar",
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 0,
                            TimeSignature = "4/4",
                            SectionLetter = "A",
                            SectionText = "Intro",
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 1,
                                    SourceRhythmId = 10,
                                    SourceRhythm = new RhythmShapeModel
                                    {
                                        NoteValue = "32nd",
                                        PrimaryTuplet = new TupletRatioModel
                                        {
                                            Numerator = 3,
                                            Denominator = 2
                                        }
                                    },
                                    Duration = 1m / 48m,
                                    Notes =
                                    [
                                        new NoteModel
                                        {
                                            Id = 1,
                                            MidiPitch = 64
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

    private static IReadOnlyList<int> SplitRefs(string refs)
        => refs.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(value => int.Parse(value, CultureInfo.InvariantCulture) >= 0)
            .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
            .ToArray();
}
