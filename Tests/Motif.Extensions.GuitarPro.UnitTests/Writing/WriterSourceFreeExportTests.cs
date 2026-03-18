namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Extensions.GuitarPro.Models.Write;
using Motif.Models;
using System.Text.Json;

public class WriterSourceFreeExportTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Writer_can_export_source_free_guitar_score_from_core_semantics()
    {
        var score = CreateSourceFreeGuitarScore();

        var outFile = Path.Combine(Path.GetTempPath(), $"motif-source-free-guitar-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            var diagnostics = await writer.WriteWithDiagnosticsAsync(score, outFile, TestContext.Current.CancellationToken);

            diagnostics.Infos.Select(entry => entry.Code).Should().Contain("GP_TEMPLATE_DEFAULTS_APPLIED");
            diagnostics.Infos.Select(entry => entry.Code).Should().Contain("GP_DEFAULT_INSTRUMENT_PROFILE_APPLIED");

            var readBack = await new Motif.Extensions.GuitarPro.GuitarProReader().ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            readBack.TempoChanges.Should().ContainSingle();
            readBack.TempoChanges[0].BeatsPerMinute.Should().Be(96m);
            readBack.Tracks.Should().ContainSingle();
            readBack.Tracks[0].Instrument.Kind.Should().Be(InstrumentKind.SteelStringGuitar);
            readBack.Tracks[0].Transposition.Octave.Should().Be(-1);
            readBack.Tracks[0].Staves.Should().ContainSingle();
            readBack.Tracks[0].Staves[0].Tuning.Pitches.Should().Equal(40, 45, 50, 55, 59, 64);
            readBack.Tracks[0].PrimaryMeasure().Beats.Should().ContainSingle();
            readBack.Tracks[0].PrimaryMeasure().Beats[0].Notes.Should().ContainSingle();
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
    public async Task Writer_can_export_source_free_guitar_score_after_json_round_trip()
    {
        var source = CreateSourceFreeGuitarScore();
        var json = source.ToJson(indented: false);
        var roundTripped = JsonSerializer.Deserialize<Score>(json, JsonOptions);

        roundTripped.Should().NotBeNull();

        var outFile = Path.Combine(Path.GetTempPath(), $"motif-source-free-guitar-json-{Guid.NewGuid():N}.gp");
        try
        {
            var diagnostics = await new Motif.Extensions.GuitarPro.GuitarProWriter().WriteWithDiagnosticsAsync(roundTripped!, outFile, TestContext.Current.CancellationToken);

            diagnostics.Warnings.Should().BeEmpty();

            var readBack = await new Motif.Extensions.GuitarPro.GuitarProReader().ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);
            readBack.Tracks[0].Instrument.Kind.Should().Be(InstrumentKind.SteelStringGuitar);
            readBack.Tracks[0].Staves[0].Tuning.Pitches.Should().Equal(40, 45, 50, 55, 59, 64);
            readBack.TempoChanges[0].BeatsPerMinute.Should().Be(96m);
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
                    Offset = ScoreTime.Zero,
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
                                            Duration = new ScoreTime(1, 4),
                                            Notes =
                                            [
                                                new Note
                                                {
                                                    Id = 1,
                                                    Pitch = Pitch.FromMidiNumber(64),
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
