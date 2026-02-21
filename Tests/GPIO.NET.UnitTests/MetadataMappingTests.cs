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
        score.Tracks.Any(t => t.Metadata.TuningPitches.Length > 0).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Staffs.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.InstrumentSet.Name)).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Sounds.Count > 0).Should().BeTrue();
        score.Tracks.Any(t => !string.IsNullOrWhiteSpace(t.Metadata.PlaybackState.Value)).Should().BeTrue();
        score.Tracks.Any(t => t.Metadata.Automations.Count > 0).Should().BeTrue();
        score.MasterTrack.TrackIds.Should().NotBeEmpty();
        score.MasterTrack.Automations.Should().NotBeEmpty();
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
                            LineCount = 6
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
                                MidiProgram = 25
                            }
                        ],
                        Rse = new RseMetadata
                        {
                            ChannelStripVersion = "E56",
                            ChannelStripParameters = "0.5 0.5"
                        },
                        PlaybackState = new PlaybackStateMetadata
                        {
                            Value = "Default"
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
                            Fermatas = [ new FermataMetadata { Type = "Short", Offset = "Middle", Length = 1.2m } ],
                            XProperties = new Dictionary<string,int> { ["1124204545"] = 2 },
                            Clef = "G2",
                            BarProperties = new Dictionary<string,string> { ["BarDisplay"] = "Both" },
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
            track.Metadata.TuningPitches.Should().Equal(40,45,50,55,59,64);
            track.Metadata.TuningInstrument.Should().Be("Guitar");
            track.Metadata.TuningLabel.Should().Be("Std");
            track.Metadata.TuningLabelVisible.Should().BeTrue();
            track.Metadata.Staffs.Should().NotBeEmpty();
            track.Metadata.Staffs[0].CapoFret.Should().Be(2);
            track.Metadata.InstrumentSet.Name.Should().Be("Steel Guitar");
            track.Metadata.InstrumentSet.Type.Should().Be("steelGuitar");
            track.Metadata.InstrumentSet.LineCount.Should().Be(6);
            track.Metadata.Sounds.Should().ContainSingle();
            track.Metadata.Sounds[0].MidiProgram.Should().Be(25);
            track.Metadata.Rse.ChannelStripVersion.Should().Be("E56");
            track.Metadata.PlaybackState.Value.Should().Be("Default");
            track.Metadata.Automations.Should().ContainSingle();
            track.Metadata.Automations[0].Type.Should().Be("Sound");

            readBack.MasterTrack.TrackIds.Should().Contain(0);
            readBack.MasterTrack.Automations.Should().ContainSingle();
            readBack.MasterTrack.Automations[0].Type.Should().Be("Tempo");
            readBack.MasterTrack.TempoMap.Should().NotBeEmpty();
            readBack.MasterTrack.TempoMap[0].Bpm.Should().Be(120m);

            var measure = track.Measures[0];
            measure.KeyAccidentalCount.Should().Be(1);
            measure.KeyMode.Should().Be("Major");
            measure.KeyTransposeAs.Should().Be("C");
            measure.Fermatas.Should().ContainSingle();
            measure.XProperties.Should().ContainKey("1124204545");
            measure.Clef.Should().Be("G2");
            measure.BarProperties.Should().ContainKey("BarDisplay");
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
