namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Models;

public class WriterArticulationParityTests
{
    [Fact]
    public async Task Writer_round_trip_preserves_core_articulation_fields()
    {
        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat
                            {
                                Id = 1,
                                GraceType = "BeforeBeat",
                                PickStrokeDirection = "Up",
                                VibratoWithTremBarStrength = "Slight",
                                Slapped = true,
                                Popped = true,
                                PalmMuted = true,
                                Brush = true,
                                BrushIsUp = true,
                                Duration = 0.25m,
                                Notes =
                                [
                                    new Note
                                    {
                                        Id = 1,
                                        MidiPitch = 64,
                                        Articulation = new NoteArticulation
                                        {
                                            LeftFingering = "I",
                                            RightFingering = "M",
                                            Ornament = "Turn",
                                            LetRing = true,
                                            AntiAccent = true,
                                            PalmMuted = true,
                                            HopoOrigin = true,
                                            Slides = [SlideType.Shift, SlideType.OutUp],
                                            Harmonic = new Harmonic
                                            {
                                                Enabled = true,
                                                Type = 2,
                                                Kind = HarmonicTypeKind.Artificial,
                                                Fret = 12m
                                            },
                                            Bend = new Bend
                                            {
                                                Enabled = true,
                                                Type = BendTypeKind.Bend,
                                                OriginOffset = 0m,
                                                OriginValue = 0m,
                                                MiddleOffset1 = 0.12m,
                                                MiddleOffset2 = 0.12m,
                                                MiddleValue = 0.5m,
                                                DestinationOffset = 0.25m,
                                                DestinationValue = 1m
                                            }
                                        }
                                    }
                                ]
                            }
                        ]
                    })
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-articulation-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            var beat = readBack.Tracks[0].PrimaryMeasure(0).Beats[0];
            var note = beat.Notes[0];
            beat.GraceType.Should().Be("BeforeBeat");
            beat.PickStrokeDirection.Should().Be("Up");
            beat.VibratoWithTremBarStrength.Should().Be("Slight");
            beat.Slapped.Should().BeTrue();
            beat.Popped.Should().BeTrue();
            beat.PalmMuted.Should().BeTrue();
            beat.Brush.Should().BeTrue();
            beat.BrushIsUp.Should().BeTrue();
            note.Articulation.LeftFingering.Should().Be("I");
            note.Articulation.RightFingering.Should().Be("M");
            note.Articulation.Ornament.Should().Be("Turn");
            note.Articulation.LetRing.Should().BeTrue();
            note.Articulation.AntiAccent.Should().BeTrue();
            note.Articulation.PalmMuted.Should().BeTrue();
            note.Articulation.HopoOrigin.Should().BeTrue();
            note.Articulation.Slides.Should().Contain([SlideType.Shift, SlideType.OutUp]);
            note.Articulation.Harmonic.Should().NotBeNull();
            note.Articulation.Harmonic!.Kind.Should().Be(HarmonicTypeKind.Artificial);
            note.Articulation.Harmonic.Type.Should().Be(2);
            note.Articulation.Harmonic.TypeName.Should().Be("Artificial");
            note.Articulation.Bend.Should().NotBeNull();
            note.Articulation.Bend!.Type.Should().Be(BendTypeKind.Bend);
            note.Articulation.Bend.OriginValue.Should().Be(0m);
            note.Articulation.Bend.MiddleValue.Should().Be(0.5m);
            note.Articulation.Bend.DestinationValue.Should().Be(1m);
            note.Articulation.Bend.MiddleOffset1.Should().Be(0.12m);
            note.Articulation.Bend.DestinationOffset.Should().Be(0.25m);
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
