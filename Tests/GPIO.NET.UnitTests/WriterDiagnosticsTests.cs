namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;
using GPIO.NET.Models.Write;
using System.IO.Compression;

public class WriterDiagnosticsTests
{
    [Fact]
    public async Task Unmapper_emits_structured_warning_for_unrepresentable_duration()
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
                            Beats = [ new BeatModel { Id = 1, Duration = 0.17m } ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().NotBeEmpty();
        result.Diagnostics.Warnings[0].Code.Should().Be("RHYTHM_APPROXIMATED");
        result.Diagnostics.Warnings[0].Category.Should().Be("Rhythm");
    }

    [Fact]
    public async Task No_op_source_fidelity_diagnostics_warn_for_schema_reference_byte_drift_and_empty_score_nodes()
    {
        var fixturePath = FixturePath("schema-reference.gp");
        var sourceBytes = await ReadScoreGpifBytesAsync(fixturePath);
        var sourceRaw = await DeserializeRawAsync(sourceBytes);

        var reader = new GPIO.NET.GuitarProReader();
        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);
        var outputBytes = await SerializeRawAsync(result.RawDocument);

        GpifWriteFidelityDiagnostics.AppendNoOpSourceFidelityWarnings(
            sourceRaw,
            result.RawDocument,
            sourceBytes,
            outputBytes,
            result.Diagnostics);

        result.Diagnostics.Warnings.Select(w => w.Code).Should().Contain("EMPTY_SCORE_NODES_DROPPED");
        result.Diagnostics.Warnings.Select(w => w.Code).Should().Contain("RAW_GPIF_BYTE_DRIFT");

        var emptyScoreNodeWarning = result.Diagnostics.Warnings.Single(w => w.Code == "EMPTY_SCORE_NODES_DROPPED");
        emptyScoreNodeWarning.Message.Should().Contain("WordsAndMusic");
        emptyScoreNodeWarning.Message.Should().Contain("PageHeader");
    }

    [Fact]
    public void No_op_source_fidelity_diagnostics_warn_for_raw_count_and_slot_drift()
    {
        var sourceRaw = new GpifDocument
        {
            MasterBars =
            [
                new GpifMasterBar
                {
                    Index = 0,
                    BarsReferenceList = "0 1"
                }
            ],
            BarsById = new Dictionary<int, GpifBar>
            {
                [0] = new()
                {
                    Id = 0,
                    VoicesReferenceList = "10"
                },
                [1] = new()
                {
                    Id = 1,
                    VoicesReferenceList = "11"
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
                    MidiPitch = 64
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

        var outputRaw = new GpifDocument
        {
            MasterBars =
            [
                new GpifMasterBar
                {
                    Index = 0,
                    BarsReferenceList = "0"
                }
            ],
            BarsById = new Dictionary<int, GpifBar>
            {
                [0] = new()
                {
                    Id = 0,
                    VoicesReferenceList = "10"
                }
            },
            VoicesById = new Dictionary<int, GpifVoice>
            {
                [10] = new()
                {
                    Id = 10,
                    BeatsReferenceList = "100"
                }
            },
            BeatsById = new Dictionary<int, GpifBeat>
            {
                [100] = new()
                {
                    Id = 100,
                    RhythmRef = 1000,
                    NotesReferenceList = "200"
                }
            },
            NotesById = new Dictionary<int, GpifNote>
            {
                [200] = new()
                {
                    Id = 200,
                    MidiPitch = 60
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

        var diagnostics = new WriteDiagnostics();
        var identicalBytes = "<GPIF><Score /></GPIF>"u8.ToArray();

        GpifWriteFidelityDiagnostics.AppendNoOpSourceFidelityWarnings(
            sourceRaw,
            outputRaw,
            identicalBytes,
            identicalBytes,
            diagnostics);

        diagnostics.Warnings.Select(w => w.Code).Should().Contain("RAW_OBJECT_COUNT_DRIFT");
        diagnostics.Warnings.Select(w => w.Code).Should().Contain("RAW_REFERENCE_COUNT_DRIFT");
        diagnostics.Warnings.Select(w => w.Code).Should().Contain("MASTER_BAR_SLOT_COUNT_DRIFT");
        diagnostics.Warnings.Select(w => w.Code).Should().NotContain("RAW_GPIF_BYTE_DRIFT");
    }

    private static string FixturePath(string fixtureName)
        => Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);

    private static async Task<byte[]> ReadScoreGpifBytesAsync(string gpPath)
    {
        using var archive = ZipFile.OpenRead(gpPath);
        var entry = archive.GetEntry("Content/score.gpif");
        entry.Should().NotBeNull();

        await using var stream = entry!.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, TestContext.Current.CancellationToken);
        return buffer.ToArray();
    }

    private static async Task<GpifDocument> DeserializeRawAsync(byte[] gpifBytes)
    {
        await using var stream = new MemoryStream(gpifBytes, writable: false);
        return await new XmlGpifDeserializer().DeserializeAsync(stream, TestContext.Current.CancellationToken);
    }

    private static async Task<byte[]> SerializeRawAsync(GpifDocument raw)
    {
        await using var stream = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(raw, stream, TestContext.Current.CancellationToken);
        return stream.ToArray();
    }
}
