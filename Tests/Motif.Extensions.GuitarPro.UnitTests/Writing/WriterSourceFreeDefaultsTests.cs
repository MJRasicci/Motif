namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;

public class WriterSourceFreeDefaultsTests
{
    [Fact]
    public async Task Source_free_unmap_populates_string_track_defaults_from_profile()
    {
        var score = CreateSourceFreeGuitarScore();

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);
        var track = result.RawDocument.Tracks.Single();
        var staff = track.Staffs.Single();

        track.Color.Should().NotBeNullOrWhiteSpace();
        track.IconId.Should().NotBeNull();
        track.AutoBrush.Should().BeTrue();
        track.AutoAccentuation.Should().BeGreaterThan(0m);
        track.PalmMute.Should().BeGreaterThan(0m);
        track.UseOneChannelPerString.Should().BeTrue();
        track.ForcedSound.Should().Be(-1);
        track.Sounds.Should().ContainSingle();
        track.Sounds[0].Name.Should().NotBeNullOrWhiteSpace();
        track.Sounds[0].Label.Should().NotBeNullOrWhiteSpace();
        track.Sounds[0].Rse.EffectChain.Should().NotBeEmpty();
        track.ChannelRse.Automations.Select(automation => automation.Type).Should().Contain("DSPParam_11");
        track.ChannelRse.Automations.Select(automation => automation.Type).Should().Contain("DSPParam_12");
        track.InstrumentSet.Elements.Should().ContainSingle();
        track.InstrumentSet.Elements[0].Articulations.Should().NotBeEmpty();
        track.Lyrics.Dispatched.Should().BeTrue();
        staff.TuningPitches.Should().Equal(40, 45, 50, 55, 59, 64);
        staff.TuningInstrument.Should().Be("Guitar");
        staff.FretCount.Should().Be(24);
        staff.PartialCapoFret.Should().Be(0);
        staff.PartialCapoStringFlags.Should().Be("000000");
        staff.EmitChordCollection.Should().BeTrue();
        staff.EmitDiagramCollection.Should().BeTrue();
        staff.EmitTuningFlatElement.Should().BeTrue();
        staff.EmitTuningFlatProperty.Should().BeTrue();
        staff.Name.Should().Be("Standard");
    }

    [Fact]
    public async Task Source_free_gpif_writer_emits_guitar_defaults_expected_by_guitar_pro()
    {
        var score = CreateSourceFreeGuitarScore();
        var outFile = Path.Combine(Path.GetTempPath(), $"motif-source-free-defaults-{Guid.NewGuid():N}.gpif");

        try
        {
            var writer = MotifScore.CreateWriter("gpif");
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var gpif = await File.ReadAllTextAsync(outFile, TestContext.Current.CancellationToken);

            gpif.Should().Contain("<Color>237 116 116</Color>");
            gpif.Should().Contain("<AutoBrush />");
            gpif.Should().Contain("<PalmMute>0.3</PalmMute>");
            gpif.Should().Contain("<AutoAccentuation>0.2</AutoAccentuation>");
            gpif.Should().Contain("<UseOneChannelPerString />");
            gpif.Should().Contain("<IconId>1</IconId>");
            gpif.Should().Contain("DSPParam_11");
            gpif.Should().Contain("DSPParam_12");
            gpif.Should().Contain("Steel Mart");
            gpif.Should().Contain("E30_EqGEq");
            gpif.Should().Contain("<Bitset>000000</Bitset>");
            gpif.Should().Contain("<Instrument>Guitar</Instrument>");
            gpif.Should().Contain("<Property name=\"TuningFlat\"><Enable /></Property>");
            gpif.Should().Contain("<Name>Standard</Name>");
        }
        finally
        {
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }
        }
    }

    private static Score CreateSourceFreeGuitarScore()
        => new()
        {
            Title = "Source Free Guitar",
            Artist = "Motif",
            Album = "GP Export",
            TempoChanges =
            [
                new TempoChange
                {
                    BarIndex = 0,
                    Position = 0,
                    BeatsPerMinute = 96m
                }
            ],
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
                    Id = 0,
                    Name = "Steel Guitar",
                    Instrument = new TrackInstrument
                    {
                        Family = InstrumentFamilyKind.Guitar,
                        Kind = InstrumentKind.SteelStringGuitar,
                        Role = TrackRoleKind.Pitched
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
                            Measures =
                            [
                                new StaffMeasure
                                {
                                    Index = 0,
                                    StaffIndex = 0,
                                    Beats =
                                    [
                                        new Beat
                                        {
                                            Id = 1,
                                            Duration = 0.25m,
                                            Notes =
                                            [
                                                new Note
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
                }
            ]
        };
}
