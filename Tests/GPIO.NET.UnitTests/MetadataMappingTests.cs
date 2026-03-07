namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;

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
        score.Metadata.ExplicitEmptyOptionalElements.Should().Contain(["WordsAndMusic", "PageHeader"]);

        score.Tracks.Should().NotBeEmpty();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.ShortName)).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.Color)).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.TuningPitches.Length > 0).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Staffs.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.InstrumentSet.Name)).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Sounds.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.PlaybackState.Value)).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Automations.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.AudioEngineState.Value)).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.MidiConnection.PrimaryChannel.HasValue).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Lyrics.Lines.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Transpose.Octave.HasValue || t.Metadata.Transpose.Chromatic.HasValue).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Rse.Automations.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Sounds.Any(s => !string.IsNullOrWhiteSpace(s.Rse.SoundbankPatch))).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.InstrumentSet.Elements.Count > 0).Should().BeTrue();
        score.MasterTrack.TrackIds.Should().NotBeEmpty();
        score.MasterTrack.Automations.Should().NotBeEmpty();
        score.MasterTrack.AutomationTimeline.Should().NotBeEmpty();
        score.MasterTrack.AutomationTimeline.Should().Contain(a => a.Scope == AutomationScopeKind.MasterTrack);
        score.MasterTrack.AutomationTimeline.Should().Contain(a => a.Scope == AutomationScopeKind.Track);
        score.MasterTrack.Rse.MasterEffects.Should().NotBeEmpty();
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
                ExplicitEmptyOptionalElements = ["WordsAndMusic", "PageHeader"],
                SubTitle = "Sub",
                Copyright = "(c) test",
                Notices = "notice",
                Instructions = "instructions",
                ScoreZoomPolicy = "Value",
                ScoreZoom = "1.5"
            },
            MasterTrack = new MasterTrackMetadata
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
                    },
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 0,
                            TimeSignature = "4/4",
                            KeyAccidentalCount = 1,
                            KeyMode = "Major",
                            KeyTransposeAs = "C",
                            DirectionProperties = new Dictionary<string,string> { ["Jump"] = "DaCapo", ["Target"] = "Segno", ["Fine"] = "1" },
                            Fermatas = [ new FermataMetadata { Type = "Short", Offset = "Middle", Length = 1.2m } ],
                            XProperties = new Dictionary<string,int> { ["1124204545"] = 2 },
                            Clef = "G2",
                            BarProperties = new Dictionary<string,string> { ["BarDisplay"] = "Both" },
                            Beats = [ new BeatModel { Id = 1, Duration = 0.25m, VoiceProperties = new Dictionary<string,string>{{"PartedSlur","true"}}, VoiceDirectionTags = new[]{"Coda"} } ]
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
            readBack.Metadata.ExplicitEmptyOptionalElements.Should().Contain(["WordsAndMusic", "PageHeader"]);

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
            track.Metadata.TuningPitches.Should().Equal(40,45,50,55,59,64);
            track.Metadata.TuningInstrument.Should().Be("Guitar");
            track.Metadata.TuningLabel.Should().Be("Std");
            track.Metadata.TuningLabelVisible.Should().BeTrue();
            track.Metadata.Staffs.Should().NotBeEmpty();
            track.Metadata.Staffs[0].CapoFret.Should().Be(2);
            track.Metadata.InstrumentSet.Name.Should().Be("Steel Guitar");
            track.Metadata.InstrumentSet.Type.Should().Be("steelGuitar");
            track.Metadata.InstrumentSet.LineCount.Should().Be(6);
            track.Metadata.InstrumentSet.Elements.Should().ContainSingle();
            track.Metadata.InstrumentSet.Elements[0].Articulations.Should().ContainSingle();
            track.Metadata.Sounds.Should().ContainSingle();
            track.Metadata.Sounds[0].MidiProgram.Should().Be(25);
            track.Metadata.Sounds[0].Rse.SoundbankPatch.Should().Be("D-Steel");
            track.Metadata.Sounds[0].Rse.EffectChain.Should().ContainSingle();
            track.Metadata.Rse.ChannelStripVersion.Should().Be("E56");
            track.Metadata.Rse.Bank.Should().Be("D-Steel");
            track.Metadata.Rse.Automations.Should().ContainSingle();
            track.Metadata.PlaybackState.Value.Should().Be("Default");
            track.Metadata.AudioEngineState.Value.Should().Be("RSE");
            track.Metadata.MidiConnection.PrimaryChannel.Should().Be(0);
            track.Metadata.MidiConnection.SecondaryChannel.Should().Be(1);
            track.Metadata.MidiConnection.ForceOneChannelPerString.Should().BeFalse();
            track.Metadata.Lyrics.Dispatched.Should().BeTrue();
            track.Metadata.Lyrics.Lines.Should().HaveCount(2);
            track.Metadata.Lyrics.Lines[0].Text.Should().Be("Hello");
            track.Metadata.Transpose.Octave.Should().Be(-1);
            track.Metadata.Automations.Should().ContainSingle();
            track.Metadata.Automations[0].Type.Should().Be("Sound");

            readBack.MasterTrack.TrackIds.Should().Contain(0);
            readBack.MasterTrack.Automations.Should().ContainSingle();
            readBack.MasterTrack.Automations[0].Type.Should().Be("Tempo");
            readBack.MasterTrack.TempoMap.Should().NotBeEmpty();
            readBack.MasterTrack.TempoMap[0].Bpm.Should().Be(120m);
            readBack.MasterTrack.AutomationTimeline.Should().HaveCount(2);
            readBack.MasterTrack.AutomationTimeline[0].Scope.Should().Be(AutomationScopeKind.MasterTrack);
            readBack.MasterTrack.AutomationTimeline[0].Type.Should().Be("Tempo");
            readBack.MasterTrack.AutomationTimeline[0].Tempo.Should().NotBeNull();
            readBack.MasterTrack.AutomationTimeline[0].Tempo!.Bpm.Should().Be(120m);
            readBack.MasterTrack.AutomationTimeline[1].Scope.Should().Be(AutomationScopeKind.Track);
            readBack.MasterTrack.AutomationTimeline[1].TrackId.Should().Be(0);
            readBack.MasterTrack.AutomationTimeline[1].Type.Should().Be("Sound");

            var measure = track.Measures[0];
            measure.KeyAccidentalCount.Should().Be(1);
            measure.KeyMode.Should().Be("Major");
            measure.KeyTransposeAs.Should().Be("C");
            measure.Fermatas.Should().ContainSingle();
            measure.XProperties.Should().ContainKey("1124204545");
            measure.Clef.Should().Be("G2");
            measure.BarProperties.Should().ContainKey("BarDisplay");
            measure.DirectionProperties.Should().ContainKey("Fine");
            measure.Jump.Should().Be("DaCapo");
            measure.Target.Should().Be("Segno");
            measure.Beats[0].VoiceProperties.Should().ContainKey("PartedSlur");
            measure.Beats[0].VoiceDirectionTags.Should().Contain("Coda");
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
        var score = new GuitarProScore
        {
            Title = "RSE Typed",
            Artist = "A",
            Album = "B",
            MasterTrack = new MasterTrackMetadata
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
            },
            Tracks =
            [
                new TrackModel
                {
                    Id = 0,
                    Name = "Track",
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

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-master-rse-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new GPIO.NET.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var reader = new GPIO.NET.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            readBack.MasterTrack.Rse.MasterEffects.Should().HaveCount(2);
            readBack.MasterTrack.Rse.MasterEffects[0].Id.Should().Be("I01_VolumeAndPan");
            readBack.MasterTrack.Rse.MasterEffects[0].Parameters.Should().Be("0.76 0.5");
            readBack.MasterTrack.Rse.MasterEffects[1].Id.Should().Be("M03_StudioReverbRoomStudioA");
            readBack.MasterTrack.Rse.MasterEffects[1].Bypass.Should().BeTrue();
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
        var timeline = score.MasterTrack.AutomationTimeline;

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
        var reader = new GPIO.NET.GuitarProReader();

        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.MasterTrack.DynamicMap.Should().NotBeEmpty();
        score.MasterTrack.DynamicMap[0].Dynamic.Should().NotBeNullOrWhiteSpace();
        score.MasterTrack.DynamicMap[0].Kind.Should().NotBe(DynamicKind.Unknown);
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
        var dynamicMap = score.MasterTrack.DynamicMap;

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
