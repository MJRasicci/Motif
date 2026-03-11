namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;
using Motif.Extensions.GuitarPro.Models.Write;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

public class WriterDiagnosticsTests
{
    [Fact]
    public async Task Unmapper_emits_structured_warning_for_unrepresentable_duration()
    {
        var score = new Score
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
    public async Task No_op_source_fidelity_diagnostics_stop_warning_for_empty_score_nodes_once_preserved()
    {
        var fixturePath = FixturePath("schema-reference.gp");
        var sourceBytes = await ReadScoreGpifBytesAsync(fixturePath);
        var sourceRaw = await DeserializeRawAsync(sourceBytes);

        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
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

        var scoreElement = XDocument.Parse(System.Text.Encoding.UTF8.GetString(outputBytes)).Root!.Element("Score");
        scoreElement.Should().NotBeNull();
        scoreElement!.Element("WordsAndMusic").Should().NotBeNull();
        scoreElement.Element("PageHeader").Should().NotBeNull();
        result.Diagnostics.Warnings.Select(w => w.Code).Should().NotContain("EMPTY_SCORE_NODES_DROPPED");
    }

    [Fact]
    public async Task Unmapper_warns_when_guitar_pro_source_fidelity_was_invalidated_before_write()
    {
        var fixturePath = FixturePath("test.gp");
        var reader = new GuitarProReader();
        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.InvalidateGuitarProExtensions();

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Select(w => w.Code).Should().Contain("GP_SOURCE_FIDELITY_INVALIDATED");
    }

    [Fact]
    public async Task Unmapper_warns_when_guitar_pro_reattachment_was_partial_before_write()
    {
        var fixturePath = FixturePath("test.gp");
        var sourceScore = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);
        var sourceTrack = sourceScore.Tracks[0];
        var sourceMeasure = sourceTrack.Measures[0];

        var editedScore = new Score
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
                                    Notes = [new NoteModel { Id = -2 }]
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

        editedScore.ReattachGuitarProExtensionsFrom(sourceScore);

        var result = await new DefaultScoreUnmapper().UnmapAsync(editedScore, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().Contain(entry =>
            entry.Code == "GP_EXTENSION_REATTACHMENT_PARTIAL"
            && entry.Category == "RawFidelity"
            && entry.Message.Contains("Unmatched targets:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unmapper_warns_when_guitar_pro_extension_graph_is_partial_without_explicit_state_markers()
    {
        var fixturePath = FixturePath("test.gp");
        var score = await new GuitarProReader().ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        score.Tracks[0].Measures[0].Beats[0].Notes[0].RemoveExtension<GpNoteExtension>();

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().Contain(entry =>
            entry.Code == "GP_EXTENSION_GRAPH_PARTIAL"
            && entry.Category == "RawFidelity"
            && entry.Message.Contains("notes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unmapper_warns_when_source_track_staves_xml_is_regenerated()
    {
        var score = await DeserializeAndMapAsync(BuildSingleNoteGpif(
            trackBody: """
                <Staves>
                  <Staff>
                    <Properties>
                      <Property name="Tuning">
                        <Pitches>40 45 50 55 59 64</Pitches>
                        <Flat />
                        <Instrument>Guitar</Instrument>
                        <Label>Std</Label>
                        <LabelVisible>true</LabelVisible>
                      </Property>
                    </Properties>
                  </Staff>
                </Staves>
                """));

        score.Tracks[0].GetRequiredGuitarPro().Metadata.Staffs[0].CapoFret = 2;

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().Contain(entry =>
            entry.Code == "TRACK_STAVES_XML_REGENERATED"
            && entry.Category == "RawFidelity");
    }

    [Fact]
    public async Task Unmapper_warns_when_source_note_string_and_fret_are_regenerated()
    {
        var score = await DeserializeAndMapAsync(BuildSingleNoteGpif(
            trackBody: """
                <Staves>
                  <Staff>
                    <Properties>
                      <Property name="Tuning">
                        <Pitches>40 45 50 55 59 64</Pitches>
                        <Flat />
                        <Instrument>Guitar</Instrument>
                        <Label>Std</Label>
                        <LabelVisible>true</LabelVisible>
                      </Property>
                    </Properties>
                  </Staff>
                </Staves>
                """,
            noteBody: """
                <Note id="200">
                  <Properties>
                    <Property name="Fret"><Fret>0</Fret></Property>
                    <Property name="Midi"><Number>45</Number></Property>
                    <Property name="String"><String>1</String></Property>
                  </Properties>
                </Note>
                """));

        score.Tracks[0].Measures[0].Beats[0].Notes[0].MidiPitch = 47;

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().Contain(entry =>
            entry.Code == "NOTE_STRING_FRET_REGENERATED"
            && entry.Category == "RawFidelity");
    }

    [Fact]
    public async Task Unmapper_warns_when_source_pitch_payloads_are_regenerated()
    {
        var score = await DeserializeAndMapAsync(BuildSingleNoteGpif(
            noteBody: """
                <Note id="200">
                  <Properties>
                    <Property name="ConcertPitch">
                      <Pitch><Step>C</Step><Accidental></Accidental><Octave>-1</Octave></Pitch>
                    </Property>
                    <Property name="Midi"><Number>36</Number></Property>
                    <Property name="TransposedPitch">
                      <Pitch><Step>C</Step><Accidental></Accidental><Octave>-1</Octave></Pitch>
                    </Property>
                  </Properties>
                </Note>
                """));

        score.Tracks[0].Measures[0].Beats[0].Notes[0].MidiPitch = 38;

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().Contain(entry =>
            entry.Code == "NOTE_CONCERT_PITCH_REGENERATED"
            && entry.Category == "RawFidelity");
        result.Diagnostics.Warnings.Should().Contain(entry =>
            entry.Code == "NOTE_TRANSPOSED_PITCH_REGENERATED"
            && entry.Category == "RawFidelity");
    }

    [Fact]
    public async Task Unmapper_warns_when_source_rhythm_shape_is_regenerated()
    {
        var score = await DeserializeAndMapAsync(BuildSingleNoteGpif());
        score.Tracks[0].Measures[0].Beats[0].Duration = 0.5m;

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().Contain(entry =>
            entry.Code == "RHYTHM_SOURCE_SHAPE_REGENERATED"
            && entry.Category == "RawFidelity");
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

    [Fact]
    public void No_op_source_fidelity_diagnostics_capture_specific_xml_difference_entries()
    {
        var raw = new GpifDocument();
        var diagnostics = new WriteDiagnostics();

        var sourceBytes = Encoding.UTF8.GetBytes(
            """
            <GPIF>
              <Score>
                <Title><![CDATA[Title]]></Title>
                <Artist><![CDATA[Artist]]></Artist>
                <PageFooter><![CDATA[Page %page%/%pages%]]></PageFooter>
              </Score>
              <Tracks>
                <Track id="0">
                  <PlaybackState>Solo</PlaybackState>
                  <Properties>
                    <Property name="ChordCollection">
                      <Items>
                        <Item id="0" name="C" />
                      </Items>
                    </Property>
                  </Properties>
                </Track>
              </Tracks>
            </GPIF>
            """);

        var outputBytes = Encoding.UTF8.GetBytes(
            """
            <GPIF>
              <Score>
                <Title><![CDATA[]]></Title>
                <Artist><![CDATA[Artist]]></Artist>
              </Score>
              <Tracks>
                <Track id="0">
                  <AutoBrush />
                  <PlaybackState>Default</PlaybackState>
                  <Properties>
                    <Property name="ChordCollection">
                      <Items />
                    </Property>
                  </Properties>
                </Track>
              </Tracks>
            </GPIF>
            """);

        GpifWriteFidelityDiagnostics.AppendNoOpSourceFidelityWarnings(
            raw,
            raw,
            sourceBytes,
            outputBytes,
            diagnostics);

        diagnostics.Warnings.Select(w => w.Code).Should().Contain("RAW_XML_DIFFERENCE_SUMMARY");
        diagnostics.Warnings.Select(w => w.Code).Should().Contain("RAW_GPIF_BYTE_DRIFT");

        diagnostics.Infos.Should().Contain(entry =>
            entry.Code == "RAW_XML_VALUE_DRIFT"
            && entry.Path == "/GPIF/Score/Title"
            && entry.SourceValue == "Title"
            && entry.OutputValue == string.Empty);

        diagnostics.Infos.Should().Contain(entry =>
            entry.Code == "RAW_XML_ELEMENT_MISSING"
            && entry.Path == "/GPIF/Score/PageFooter"
            && entry.SourceValue!.Contains("PageFooter", StringComparison.Ordinal)
            && entry.OutputValue == null);

        diagnostics.Infos.Should().Contain(entry =>
            entry.Code == "RAW_XML_ELEMENT_ADDED"
            && entry.Path == "/GPIF/Tracks/Track[@id='0']/AutoBrush"
            && entry.SourceValue == null
            && entry.OutputValue!.Contains("AutoBrush", StringComparison.Ordinal));

        diagnostics.Infos.Should().Contain(entry =>
            entry.Code == "RAW_XML_VALUE_DRIFT"
            && entry.Path == "/GPIF/Tracks/Track[@id='0']/PlaybackState"
            && entry.SourceValue == "Solo"
            && entry.OutputValue == "Default");

        diagnostics.Infos.Should().Contain(entry =>
            entry.Code == "RAW_XML_ELEMENT_MISSING"
            && entry.Path == "/GPIF/Tracks/Track[@id='0']/Properties/Property[@name='ChordCollection']/Items/Item[@id='0']"
            && entry.SourceValue!.Contains("Item id=\"0\"", StringComparison.Ordinal)
            && entry.OutputValue == null);
    }

    [Fact]
    public void No_op_source_fidelity_diagnostics_capture_attribute_difference_entries()
    {
        var raw = new GpifDocument();
        var diagnostics = new WriteDiagnostics();

        var sourceBytes = Encoding.UTF8.GetBytes(
            """
            <GPIF>
              <Notes>
                <Note id="513">
                  <Tie origin="true" destination="false" />
                </Note>
              </Notes>
            </GPIF>
            """);

        var outputBytes = Encoding.UTF8.GetBytes(
            """
            <GPIF>
              <Notes>
                <Note id="513">
                  <Tie origin="true" destination="true" />
                </Note>
              </Notes>
            </GPIF>
            """);

        GpifWriteFidelityDiagnostics.AppendNoOpSourceFidelityWarnings(
            raw,
            raw,
            sourceBytes,
            outputBytes,
            diagnostics);

        diagnostics.Infos.Should().Contain(entry =>
            entry.Code == "RAW_XML_ATTRIBUTE_DRIFT"
            && entry.Path == "/GPIF/Notes/Note[@id='513']/Tie/@destination"
            && entry.SourceValue == "false"
            && entry.OutputValue == "true");
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

    private static string BuildSingleNoteGpif(
        string trackBody = "",
        string rhythmBody = """<Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm>""",
        string beatBody = """
            <Beat id="100">
              <Rhythm ref="1000" />
              <Notes>200</Notes>
            </Beat>
            """,
        string noteBody = """
            <Note id="200">
              <Properties>
                <Property name="Midi"><Number>60</Number></Property>
              </Properties>
            </Note>
            """)
        => $"""
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks>
            <Track id="0">
              <Name>Guitar</Name>
              {trackBody}
            </Track>
          </Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms>{rhythmBody}</Rhythms>
          <Beats>{beatBody}</Beats>
          <Notes>{noteBody}</Notes>
        </GPIF>
        """;

    private static async Task<Score> DeserializeAndMapAsync(string gpif)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var raw = await new XmlGpifDeserializer().DeserializeAsync(stream, TestContext.Current.CancellationToken);
        return await new DefaultScoreMapper().MapAsync(raw, TestContext.Current.CancellationToken);
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
}
