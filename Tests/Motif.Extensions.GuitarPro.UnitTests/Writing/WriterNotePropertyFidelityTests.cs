namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public class WriterNotePropertyFidelityTests
{
    private static GpNoteMetadata NoteMetadataOf(Note note)
        => note.GetRequiredGuitarPro().Metadata;

    private static string BuildGpif(string noteBody)
    {
        return $"""
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <Tracks><Track id="0"><Name>Guitar</Name></Track></Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats>
            <Beat id="100">
              <Rhythm ref="1000" />
              <Notes>200</Notes>
            </Beat>
          </Beats>
          <Notes>
            <Note id="200">
              {noteBody}
            </Note>
          </Notes>
        </GPIF>
        """;
    }

    private static async Task<Score> DeserializeAndMap(string gpif)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var raw = await new XmlGpifDeserializer().DeserializeAsync(stream, TestContext.Current.CancellationToken);
        return await new DefaultScoreMapper().MapAsync(raw, TestContext.Current.CancellationToken);
    }

    private static async Task<XDocument> RoundTripThroughWrite(Score score)
    {
        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);
        await using var stream = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(result.RawDocument, stream, TestContext.Current.CancellationToken);
        return XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static async Task<XDocument> RoundTripThroughJsonAndWrite(string gpif)
    {
        var score = await DeserializeAndMap(gpif);
        var json = score.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<Score>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();
        fromJson!.ReattachGuitarProExtensionsFrom(score);
        return await RoundTripThroughWrite(fromJson!);
    }

    [Fact]
    public async Task Unmapper_uses_staff_specific_tuning_for_additional_staff_notes()
    {
        var score = new Score
        {
            Tracks =
            [
                new Track
                {
                    Id = 0,
                    Name = "Piano",
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
                                    StaffIndex = 0
                                }
                            ]
                        },
                        new Staff
                        {
                            StaffIndex = 1,
                            Measures =
                            [
                                new StaffMeasure
                                {
                                    Index = 0,
                                    StaffIndex = 1,
                                    Beats =
                                    [
                                        new Beat
                                        {
                                            Id = 1,
                                            Duration = 0.5m,
                                            Notes =
                                            [
                                                new Note
                                                {
                                                    Id = 22,
                                                    MidiPitch = 40,
                                                    StringNumber = 3
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
        score.Tracks[0].GetOrCreateGuitarPro().Metadata = new TrackMetadata
        {
            TuningPitches = [40, 45, 50, 55, 59, 64],
            Staffs =
            [
                new StaffMetadata
                {
                    TuningPitches = [40, 45, 50, 55, 59, 64]
                },
                new StaffMetadata
                {
                    TuningPitches = [23, 28, 33, 38, 43]
                }
            ]
        };

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);
        var note = result.RawDocument.NotesById.Values.Should().ContainSingle().Subject;

        note.Properties.Should().ContainSingle(p => p.Name == "Fret" && p.Fret == 2);
        note.Properties.Should().ContainSingle(p => p.Name == "String" && p.StringNumber == 3);
        note.Properties.Should().ContainSingle(p => p.Name == "Midi" && p.Number == 40);
    }

    [Fact]
    public async Task Source_pitch_payloads_round_trip_through_json_and_write_when_note_midi_is_unchanged()
    {
        var gpif = BuildGpif("""
            <InstrumentArticulation>8</InstrumentArticulation>
            <Properties>
              <Property name="ConcertPitch">
                <Pitch><Step>C</Step><Accidental></Accidental><Octave>-1</Octave></Pitch>
              </Property>
              <Property name="Fret"><Fret>36</Fret></Property>
              <Property name="Midi"><Number>36</Number></Property>
              <Property name="String"><String>0</String></Property>
              <Property name="TransposedPitch">
                <Pitch><Step>C</Step><Accidental></Accidental><Octave>-1</Octave></Pitch>
              </Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        var note = score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0];

        note.MidiPitch.Should().Be(36);
        NoteMetadataOf(note).SourceMidiPitch.Should().Be(36);
        NoteMetadataOf(note).SourceTransposedMidiPitch.Should().Be(36);
        note.ConcertPitch.Should().NotBeNull();
        note.ConcertPitch!.Step.Should().Be("C");
        note.ConcertPitch.Octave.Should().Be(-1);
        note.TransposedPitch.Should().NotBeNull();
        note.TransposedPitch!.Step.Should().Be("C");
        note.TransposedPitch.Octave.Should().Be(-1);

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var outputProperties = roundTrip.Root!
            .Element("Notes")!
            .Element("Note")!
            .Element("Properties")!
            .Elements("Property")
            .ToDictionary(p => (string)p.Attribute("name")!, p => p);

        outputProperties["ConcertPitch"].Element("Pitch")!.Element("Step")!.Value.Should().Be("C");
        outputProperties["ConcertPitch"].Element("Pitch")!.Element("Accidental")!.Value.Should().BeEmpty();
        outputProperties["ConcertPitch"].Element("Pitch")!.Element("Octave")!.Value.Should().Be("-1");
        outputProperties["TransposedPitch"].Element("Pitch")!.Element("Step")!.Value.Should().Be("C");
        outputProperties["TransposedPitch"].Element("Pitch")!.Element("Accidental")!.Value.Should().BeEmpty();
        outputProperties["TransposedPitch"].Element("Pitch")!.Element("Octave")!.Value.Should().Be("-1");
    }

    [Fact]
    public async Task Changed_note_midi_regenerates_pitch_payloads_instead_of_reusing_source_values()
    {
        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Drums",
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
                                        Id = 200,
                                        MidiPitch = 38,
                                        ConcertPitch = new PitchValue
                                        {
                                            Step = "C",
                                            Accidental = string.Empty,
                                            Octave = -1
                                        },
                                        TransposedPitch = new PitchValue
                                        {
                                            Step = "C",
                                            Accidental = string.Empty,
                                            Octave = -1
                                        },
                                        StringNumber = 0
                                    }
                                ]
                            }
                        ]
                    })
            ]
        };
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0].GetOrCreateGuitarPro().Metadata.SourceMidiPitch = 36;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0].GetOrCreateGuitarPro().Metadata.SourceTransposedMidiPitch = 36;

        var roundTrip = await RoundTripThroughWrite(score);
        var outputProperties = roundTrip.Root!
            .Element("Notes")!
            .Element("Note")!
            .Element("Properties")!
            .Elements("Property")
            .ToDictionary(p => (string)p.Attribute("name")!, p => p);

        outputProperties["ConcertPitch"].Element("Pitch")!.Element("Step")!.Value.Should().Be("D");
        outputProperties["ConcertPitch"].Element("Pitch")!.Element("Accidental")!.Value.Should().BeEmpty();
        outputProperties["ConcertPitch"].Element("Pitch")!.Element("Octave")!.Value.Should().Be("3");
        outputProperties["TransposedPitch"].Element("Pitch")!.Element("Step")!.Value.Should().Be("D");
        outputProperties["TransposedPitch"].Element("Pitch")!.Element("Accidental")!.Value.Should().BeEmpty();
        outputProperties["TransposedPitch"].Element("Pitch")!.Element("Octave")!.Value.Should().Be("3");
    }

    [Fact]
    public async Task Note_velocity_and_bend_float_payloads_round_trip_through_json_and_write()
    {
        var gpif = BuildGpif("""
            <Velocity>72</Velocity>
            <Properties>
              <Property name="Bended"><Enable /></Property>
              <Property name="BendMiddleOffset1"><Float>12.000000</Float></Property>
              <Property name="BendMiddleOffset2"><Float>34.000000</Float></Property>
              <Property name="BendDestinationOffset"><Float>50.000000</Float></Property>
              <Property name="Midi"><Number>60</Number></Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        var note = score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0];

        note.Velocity.Should().Be(72);
        note.Articulation.Bend.Should().NotBeNull();

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var outputNote = roundTrip.Root!.Element("Notes")!.Element("Note")!;
        var outputProperties = outputNote.Element("Properties")!.Elements("Property")
            .ToDictionary(p => (string)p.Attribute("name")!, p => p);

        outputNote.Element("Velocity")!.Value.Should().Be("72");
        outputProperties["BendMiddleOffset1"].Element("Float")!.Value.Should().Be("12.000000");
        outputProperties["BendMiddleOffset2"].Element("Float")!.Value.Should().Be("34.000000");
        outputProperties["BendDestinationOffset"].Element("Float")!.Value.Should().Be("50.000000");
    }
}
