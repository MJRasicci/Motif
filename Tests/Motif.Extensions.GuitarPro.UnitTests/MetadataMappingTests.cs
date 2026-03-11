namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;

public class MetadataMappingTests
{
    private static ScoreMetadata ScoreMetadataOf(Score score)
        => score.GetRequiredGuitarPro().Metadata;

    private static MasterTrackMetadata MasterTrackMetadataOf(Score score)
        => score.GetRequiredGuitarPro().MasterTrack;

    private static TrackMetadata TrackMetadataOf(Track track)
        => track.GetRequiredGuitarPro().Metadata;

    private static GpVoiceMetadata VoiceMetadataOf(Voice voice)
        => voice.GetRequiredGuitarPro().Metadata;

    [Fact]
    public async Task Reader_maps_score_and_track_metadata_from_fixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "schema-reference.gp");
        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();

        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);
        var scoreMetadata = ScoreMetadataOf(score);
        var masterTrackMetadata = MasterTrackMetadataOf(score);

        scoreMetadata.SubTitle.Should().NotBeNullOrWhiteSpace();
        scoreMetadata.Copyright.Should().NotBeNullOrWhiteSpace();
        scoreMetadata.Notices.Should().NotBeNullOrWhiteSpace();
        scoreMetadata.ExplicitEmptyOptionalElements.Should().Contain(["WordsAndMusic", "PageHeader"]);

        score.Tracks.Should().NotBeEmpty();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(TrackMetadataOf(t).ShortName)).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(TrackMetadataOf(t).Color)).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).TuningPitches.Length > 0).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).Staffs.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(TrackMetadataOf(t).InstrumentSet.Name)).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).Sounds.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(TrackMetadataOf(t).PlaybackState.Value)).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).Automations.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(TrackMetadataOf(t).AudioEngineState.Value)).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).MidiConnection.PrimaryChannel.HasValue).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).Lyrics.Lines.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).Transpose.Octave.HasValue || TrackMetadataOf(t).Transpose.Chromatic.HasValue).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).Rse.Automations.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).Sounds.Any(s => !string.IsNullOrWhiteSpace(s.Rse.SoundbankPatch))).Should().BeTrue();
        score.Tracks.Any(t => TrackMetadataOf(t).InstrumentSet.Elements.Count > 0).Should().BeTrue();
        masterTrackMetadata.TrackIds.Should().NotBeEmpty();
        masterTrackMetadata.Automations.Should().NotBeEmpty();
        masterTrackMetadata.AutomationTimeline.Should().NotBeEmpty();
        masterTrackMetadata.AutomationTimeline.Should().Contain(a => a.Scope == AutomationScopeKind.MasterTrack);
        masterTrackMetadata.AutomationTimeline.Should().Contain(a => a.Scope == AutomationScopeKind.Track);
        masterTrackMetadata.Rse.MasterEffects.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Writer_round_trip_preserves_custom_score_and_track_metadata()
    {
        var beat = new Beat
        {
            Id = 1,
            Duration = 0.25m
        };
        var voice = new Voice
        {
            VoiceIndex = 0,
            Beats = [beat]
        };

        var score = new Score
        {
            Title = "T",
            Artist = "A",
            Album = "B",
            TimelineBars =
            [
                new TimelineBar
                {
                    Index = 0,
                    TimeSignature = "4/4",
                    KeyAccidentalCount = 1,
                    KeyMode = "Major",
                    KeyTransposeAs = "C",
                    DirectionProperties = new Dictionary<string, string> { ["Jump"] = "DaCapo", ["Target"] = "Segno", ["Fine"] = "1" },
                    Fermatas = [new FermataMetadata { Type = "Short", Offset = "Middle", Length = 1.2m }],
                    XProperties = new Dictionary<string, int> { ["1124204545"] = 2 }
                }
            ],
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Clef = "G2",
                        BarProperties = new Dictionary<string, string> { ["BarDisplay"] = "Both" },
                        Voices = [ voice ],
                        Beats = [ beat ]
                    })
            ]
        };
        score.GetOrCreateGuitarPro().Metadata = new ScoreMetadata
        {
            ExplicitEmptyOptionalElements = ["WordsAndMusic", "PageHeader"],
            SubTitle = "Sub",
            Copyright = "(c) test",
            Notices = "notice",
            Instructions = "instructions",
            ScoreZoomPolicy = "Value",
            ScoreZoom = "1.5"
        };
        score.GetRequiredGuitarPro().MasterTrack = new MasterTrackMetadata
        {
            TrackIds = [0],
            RseXml = "<RSE><ChannelStrip version=\"E56\"><Parameters>0.8 0.1</Parameters></ChannelStrip></RSE>",
            Automations =
            [
                new AutomationMetadata
                {
                    Type = "Tempo",
                    Linear = false,
                    Bar = 0,
                    Position = 0,
                    Visible = true,
                    Value = "120 2"
                }
            ]
        };
        score.Tracks[0].GetOrCreateGuitarPro().Metadata = new TrackMetadata
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
            ForcedSound = -1,
            TuningPitches = [40,45,50,55,59,64],
            TuningInstrument = "Guitar",
            TuningLabel = "Std",
            TuningLabelVisible = true,
            Staffs =
            [
                new StaffMetadata
                {
                    Id = 1,
                    Cref = "64",
                    TuningPitches = [40,45,50,55,59,64],
                    CapoFret = 2,
                    Properties = new Dictionary<string,string> { ["CapoFret"] = "2" }
                }
            ],
            InstrumentSet = new InstrumentSetMetadata
            {
                Name = "Steel Guitar",
                Type = "steelGuitar",
                LineCount = 6,
                Elements =
                [
                    new InstrumentElementMetadata
                    {
                        Name = "Pitched",
                        Type = "pitched",
                        SoundbankName = "D-Steel",
                        Articulations =
                        [
                            new InstrumentArticulationMetadata
                            {
                                Name = "Sustain",
                                StaffLine = 0,
                                Noteheads = "noteheadBlack",
                                TechniquePlacement = "outside",
                                TechniqueSymbol = "none",
                                InputMidiNumbers = "60 61",
                                OutputRseSound = "Steel Mart",
                                OutputMidiNumber = 60
                            }
                        ]
                    }
                ]
            },
            Sounds =
            [
                new SoundMetadata
                {
                    Name = "Steel Mart",
                    Label = "Steel Mart",
                    Path = "Stringed/Acoustic Guitars/Steel Guitar",
                    Role = "Factory",
                    MidiLsb = 0,
                    MidiMsb = 0,
                    MidiProgram = 25,
                    Rse = new SoundRseMetadata
                    {
                        SoundbankPatch = "D-Steel",
                        SoundbankSet = "Factory",
                        ElementsSettingsXml = "<ElementsSettings />",
                        Pickups = new SoundRsePickupsMetadata
                        {
                            OverloudPosition = "0",
                            Volumes = "1 1",
                            Tones = "1 1"
                        },
                        EffectChain =
                        [
                            new RseEffectMetadata
                            {
                                Id = "E30_EqGEq",
                                Parameters = "0.5 0.5 0.5 0.5 0.5 0.5 0.5 0.685714"
                            }
                        ]
                    }
                }
            ],
            Rse = new RseMetadata
            {
                Bank = "D-Steel",
                ChannelStripVersion = "E56",
                ChannelStripParameters = "0.5 0.5",
                Automations =
                [
                    new AutomationMetadata
                    {
                        Type = "DSPParam_12",
                        Linear = false,
                        Bar = 0,
                        Position = 0,
                        Visible = true,
                        Value = "0.72"
                    }
                ]
            },
            PlaybackState = new PlaybackStateMetadata
            {
                Value = "Default"
            },
            AudioEngineState = new AudioEngineStateMetadata
            {
                Value = "RSE"
            },
            MidiConnection = new MidiConnectionMetadata
            {
                Port = 0,
                PrimaryChannel = 0,
                SecondaryChannel = 1,
                ForceOneChannelPerString = false
            },
            Lyrics = new LyricsMetadata
            {
                Dispatched = true,
                Lines =
                [
                    new LyricsLineMetadata { Text = "Hello", Offset = 0 },
                    new LyricsLineMetadata { Text = "World", Offset = 1 }
                ]
            },
            Transpose = new TransposeMetadata
            {
                Chromatic = 0,
                Octave = -1
            },
            Automations =
            [
                new AutomationMetadata
                {
                    Type = "Sound",
                    Linear = false,
                    Bar = 0,
                    Position = 0,
                    Visible = true,
                    Value = "Stringed/Acoustic Guitars/Steel Guitar;Steel Mart;Factory"
                }
            ]
        };
        voice.GetOrCreateGuitarPro().Metadata.Properties =
            new Dictionary<string, string> { ["PartedSlur"] = "true" };
        voice.GetRequiredGuitarPro().Metadata.DirectionTags = ["Coda"];

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-meta-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);
            var readBackScoreMetadata = ScoreMetadataOf(readBack);
            var readBackMasterTrack = MasterTrackMetadataOf(readBack);

            readBackScoreMetadata.SubTitle.Should().Be("Sub");
            readBackScoreMetadata.Copyright.Should().Be("(c) test");
            readBackScoreMetadata.Notices.Should().Be("notice");
            readBackScoreMetadata.Instructions.Should().Be("instructions");
            readBackScoreMetadata.ScoreZoomPolicy.Should().Be("Value");
            readBackScoreMetadata.ScoreZoom.Should().Be("1.5");
            readBackScoreMetadata.ExplicitEmptyOptionalElements.Should().Contain(["WordsAndMusic", "PageHeader"]);

            var track = readBack.Tracks[0];
            var trackMetadata = TrackMetadataOf(track);
            trackMetadata.ShortName.Should().Be("gtr");
            trackMetadata.Color.Should().Be("255 0 0");
            trackMetadata.SystemsDefaultLayout.Should().Be("3");
            trackMetadata.SystemsLayout.Should().Be("3 3");
            trackMetadata.PalmMute.Should().Be(0.3m);
            trackMetadata.AutoAccentuation.Should().Be(0.2m);
            trackMetadata.AutoBrush.Should().BeTrue();
            trackMetadata.PlayingStyle.Should().Be("StringedPick");
            trackMetadata.UseOneChannelPerString.Should().BeTrue();
            trackMetadata.IconId.Should().Be(1);
            trackMetadata.ForcedSound.Should().Be(-1);
            trackMetadata.TuningPitches.Should().Equal(40,45,50,55,59,64);
            trackMetadata.TuningInstrument.Should().Be("Guitar");
            trackMetadata.TuningLabel.Should().Be("Std");
            trackMetadata.TuningLabelVisible.Should().BeTrue();
            trackMetadata.Staffs.Should().NotBeEmpty();
            trackMetadata.Staffs[0].CapoFret.Should().Be(2);
            trackMetadata.InstrumentSet.Name.Should().Be("Steel Guitar");
            trackMetadata.InstrumentSet.Type.Should().Be("steelGuitar");
            trackMetadata.InstrumentSet.LineCount.Should().Be(6);
            trackMetadata.InstrumentSet.Elements.Should().ContainSingle();
            trackMetadata.InstrumentSet.Elements[0].Articulations.Should().ContainSingle();
            trackMetadata.Sounds.Should().ContainSingle();
            trackMetadata.Sounds[0].MidiProgram.Should().Be(25);
            trackMetadata.Sounds[0].Rse.SoundbankPatch.Should().Be("D-Steel");
            trackMetadata.Sounds[0].Rse.EffectChain.Should().ContainSingle();
            trackMetadata.Rse.ChannelStripVersion.Should().Be("E56");
            trackMetadata.Rse.Bank.Should().Be("D-Steel");
            trackMetadata.Rse.Automations.Should().ContainSingle();
            trackMetadata.PlaybackState.Value.Should().Be("Default");
            trackMetadata.AudioEngineState.Value.Should().Be("RSE");
            trackMetadata.MidiConnection.PrimaryChannel.Should().Be(0);
            trackMetadata.MidiConnection.SecondaryChannel.Should().Be(1);
            trackMetadata.MidiConnection.ForceOneChannelPerString.Should().BeFalse();
            trackMetadata.Lyrics.Dispatched.Should().BeTrue();
            trackMetadata.Lyrics.Lines.Should().HaveCount(2);
            trackMetadata.Lyrics.Lines[0].Text.Should().Be("Hello");
            trackMetadata.Transpose.Octave.Should().Be(-1);
            trackMetadata.Automations.Should().ContainSingle();
            trackMetadata.Automations[0].Type.Should().Be("Sound");

            readBackMasterTrack.TrackIds.Should().Contain(0);
            readBackMasterTrack.Automations.Should().ContainSingle();
            readBackMasterTrack.Automations[0].Type.Should().Be("Tempo");
            readBackMasterTrack.TempoMap.Should().NotBeEmpty();
            readBackMasterTrack.TempoMap[0].Bpm.Should().Be(120m);
            readBackMasterTrack.AutomationTimeline.Should().HaveCount(2);
            readBackMasterTrack.AutomationTimeline[0].Scope.Should().Be(AutomationScopeKind.MasterTrack);
            readBackMasterTrack.AutomationTimeline[0].Type.Should().Be("Tempo");
            readBackMasterTrack.AutomationTimeline[0].Tempo.Should().NotBeNull();
            readBackMasterTrack.AutomationTimeline[0].Tempo!.Bpm.Should().Be(120m);
            readBackMasterTrack.AutomationTimeline[1].Scope.Should().Be(AutomationScopeKind.Track);
            readBackMasterTrack.AutomationTimeline[1].TrackId.Should().Be(0);
            readBackMasterTrack.AutomationTimeline[1].Type.Should().Be("Sound");

            var timelineBar = readBack.TimelineBars[0];
            var measure = track.PrimaryMeasure(0);
            timelineBar.KeyAccidentalCount.Should().Be(1);
            timelineBar.KeyMode.Should().Be("Major");
            timelineBar.KeyTransposeAs.Should().Be("C");
            timelineBar.Fermatas.Should().ContainSingle();
            timelineBar.XProperties.Should().ContainKey("1124204545");
            measure.Clef.Should().Be("G2");
            measure.BarProperties.Should().ContainKey("BarDisplay");
            timelineBar.DirectionProperties.Should().ContainKey("Fine");
            timelineBar.Jump.Should().Be("DaCapo");
            timelineBar.Target.Should().Be("Segno");
            VoiceMetadataOf(measure.Voices[0]).Properties.Should().ContainKey("PartedSlur");
            VoiceMetadataOf(measure.Voices[0]).DirectionTags.Should().Contain("Coda");
        }
        finally
        {
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }
        }
    }

    [Fact]
    public async Task Writer_round_trip_preserves_typed_master_track_rse_when_raw_xml_absent()
    {
        var score = new Score
        {
            Title = "RSE Typed",
            Artist = "A",
            Album = "B",
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Track",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats = [ new Beat { Id = 1, Duration = 0.25m } ]
                    })
            ]
        };
        score.GetOrCreateGuitarPro().MasterTrack = new MasterTrackMetadata
        {
            TrackIds = [0],
            Rse = new MasterTrackRseMetadata
            {
                MasterEffects =
                [
                    new RseEffectMetadata
                    {
                        Id = "I01_VolumeAndPan",
                        Parameters = "0.76 0.5"
                    },
                    new RseEffectMetadata
                    {
                        Id = "M03_StudioReverbRoomStudioA",
                        Bypass = true,
                        Parameters = "0 0 0 0 0"
                    }
                ]
            }
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-master-rse-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);
            var readBackMasterTrack = MasterTrackMetadataOf(readBack);

            readBackMasterTrack.Rse.MasterEffects.Should().HaveCount(2);
            readBackMasterTrack.Rse.MasterEffects[0].Id.Should().Be("I01_VolumeAndPan");
            readBackMasterTrack.Rse.MasterEffects[0].Parameters.Should().Be("0.76 0.5");
            readBackMasterTrack.Rse.MasterEffects[1].Id.Should().Be("M03_StudioReverbRoomStudioA");
            readBackMasterTrack.Rse.MasterEffects[1].Bypass.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }
        }
    }

    [Fact]
    public async Task Mapper_synthesizes_unified_automation_timeline_with_order_and_typed_values()
    {
        var document = new GpifDocument
        {
            MasterTrack = new GpifMasterTrack
            {
                TrackIds = [10, 20],
                Automations =
                [
                    new GpifAutomation
                    {
                        Type = "Tempo",
                        Bar = 1,
                        Position = 0,
                        Value = "132 2",
                        Linear = true,
                        Visible = true
                    },
                    new GpifAutomation
                    {
                        Type = "MasterVolume",
                        Bar = 0,
                        Position = 5,
                        Value = "0.8",
                        Linear = false,
                        Visible = true
                    }
                ]
            },
            Tracks =
            [
                new GpifTrack
                {
                    Id = 20,
                    Name = "Track 20",
                    Automations =
                    [
                        new GpifAutomation
                        {
                            Type = "Sound",
                            Bar = 0,
                            Position = 5,
                            Value = "Stringed/Acoustic Guitars/Steel Guitar;Steel Mart;Factory",
                            Visible = true
                        }
                    ]
                },
                new GpifTrack
                {
                    Id = 10,
                    Name = "Track 10",
                    Automations =
                    [
                        new GpifAutomation
                        {
                            Type = "DSPParam_12",
                            Bar = 0,
                            Position = 3,
                            Value = "0.72",
                            Linear = false,
                            Visible = true
                        }
                    ]
                }
            ]
        };

        var score = await new DefaultScoreMapper().MapAsync(document, TestContext.Current.CancellationToken);
        var timeline = MasterTrackMetadataOf(score).AutomationTimeline;

        timeline.Should().HaveCount(4);
        timeline.Select(a => (a.Scope, a.TrackId, a.Type)).Should().Equal(
            (AutomationScopeKind.Track, 10, "DSPParam_12"),
            (AutomationScopeKind.MasterTrack, null, "MasterVolume"),
            (AutomationScopeKind.Track, 20, "Sound"),
            (AutomationScopeKind.MasterTrack, null, "Tempo"));

        timeline[0].NumericValue.Should().Be(0.72m);
        timeline[0].ReferenceHint.Should().BeNull();

        timeline[1].NumericValue.Should().Be(0.8m);
        timeline[1].ReferenceHint.Should().BeNull();

        timeline[2].NumericValue.Should().BeNull();
        timeline[2].ReferenceHint.Should().BeNull();

        timeline[3].NumericValue.Should().Be(132m);
        timeline[3].ReferenceHint.Should().Be(2);
        timeline[3].Tempo.Should().NotBeNull();
        timeline[3].Tempo!.Bpm.Should().Be(132m);
        timeline[3].Tempo!.DenominatorHint.Should().Be(2);
    }

    [Fact]
    public async Task Reader_synthesizes_dynamic_map_from_fixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.gp");
        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();

        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);
        var dynamicMap = MasterTrackMetadataOf(score).DynamicMap;

        dynamicMap.Should().NotBeEmpty();
        dynamicMap[0].Dynamic.Should().NotBeNullOrWhiteSpace();
        dynamicMap[0].Kind.Should().NotBe(DynamicKind.Unknown);
    }

    [Fact]
    public async Task Mapper_synthesizes_dynamic_map_change_points_per_track_and_voice()
    {
        var document = new GpifDocument
        {
            MasterTrack = new GpifMasterTrack
            {
                TrackIds = [0]
            },
            Tracks =
            [
                new GpifTrack
                {
                    Id = 0,
                    Name = "Track 0"
                }
            ],
            MasterBars =
            [
                new GpifMasterBar
                {
                    Index = 0,
                    Time = "4/4",
                    BarsReferenceList = "1"
                }
            ],
            BarsById = new Dictionary<int, GpifBar>
            {
                [1] = new GpifBar
                {
                    Id = 1,
                    VoicesReferenceList = "10"
                }
            },
            VoicesById = new Dictionary<int, GpifVoice>
            {
                [10] = new GpifVoice
                {
                    Id = 10,
                    BeatsReferenceList = "100 101 102"
                }
            },
            BeatsById = new Dictionary<int, GpifBeat>
            {
                [100] = new GpifBeat
                {
                    Id = 100,
                    RhythmRef = 1000,
                    Dynamic = "MF"
                },
                [101] = new GpifBeat
                {
                    Id = 101,
                    RhythmRef = 1000,
                    Dynamic = "MF"
                },
                [102] = new GpifBeat
                {
                    Id = 102,
                    RhythmRef = 1000,
                    Dynamic = "FF"
                }
            },
            RhythmsById = new Dictionary<int, GpifRhythm>
            {
                [1000] = new GpifRhythm
                {
                    Id = 1000,
                    NoteValue = "Quarter"
                }
            }
        };

        var score = await new DefaultScoreMapper().MapAsync(document, TestContext.Current.CancellationToken);
        var dynamicMap = MasterTrackMetadataOf(score).DynamicMap;

        dynamicMap.Should().HaveCount(2);

        dynamicMap[0].TrackId.Should().Be(0);
        dynamicMap[0].MeasureIndex.Should().Be(0);
        dynamicMap[0].VoiceIndex.Should().Be(0);
        dynamicMap[0].BeatId.Should().Be(100);
        dynamicMap[0].BeatOffset.Should().Be(0m);
        dynamicMap[0].Dynamic.Should().Be("MF");
        dynamicMap[0].Kind.Should().Be(DynamicKind.MF);

        dynamicMap[1].TrackId.Should().Be(0);
        dynamicMap[1].MeasureIndex.Should().Be(0);
        dynamicMap[1].VoiceIndex.Should().Be(0);
        dynamicMap[1].BeatId.Should().Be(102);
        dynamicMap[1].BeatOffset.Should().Be(0.5m);
        dynamicMap[1].Dynamic.Should().Be("FF");
        dynamicMap[1].Kind.Should().Be(DynamicKind.FF);
    }
}
