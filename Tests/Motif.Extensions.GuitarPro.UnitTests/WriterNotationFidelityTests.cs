namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public class WriterNotationFidelityTests
{
    private static GpBeatMetadata BeatMetadataOf(Beat beat)
        => beat.GetRequiredGuitarPro().Metadata;

    private static GpNoteMetadata NoteMetadataOf(Note note)
        => note.GetRequiredGuitarPro().Metadata;

    private static string BuildGpif()
    {
        return """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <Tracks>
            <Track id="0">
              <Name>Lead</Name>
              <Properties>
                <Property name="Tuning">
                  <Pitches>62</Pitches>
                  <Instrument>Guitar</Instrument>
                  <Label><![CDATA[]]></Label>
                  <LabelVisible>true</LabelVisible>
                </Property>
              </Properties>
            </Track>
          </Tracks>
          <MasterBars>
            <MasterBar>
              <Time>4/4</Time>
              <DoubleBar />
              <TripletFeel>Triplet8th</TripletFeel>
              <Repeat start="false" end="false" count="0" />
              <Bars>1</Bars>
              <XProperties>
                <XProperty id="1124073984"><Double>1.020630</Double></XProperty>
                <XProperty id="1124073985"><Int>2</Int></XProperty>
              </XProperties>
            </MasterBar>
          </MasterBars>
          <Bars>
            <Bar id="1">
              <Voices>10</Voices>
              <Clef>G2</Clef>
              <XProperties>
                <XProperty id="1124139520"><Double>0.965927</Double></XProperty>
                <XProperty id="1124139521"><Int>7</Int></XProperty>
              </XProperties>
            </Bar>
          </Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats>
            <Beat id="100">
              <Rhythm ref="1000" />
              <TransposedPitchStemOrientation>Downward</TransposedPitchStemOrientation>
              <ConcertPitchStemOrientation>Undefined</ConcertPitchStemOrientation>
              <Hairpin>Crescendo</Hairpin>
              <Ottavia>8va</Ottavia>
              <Whammy originValue="0.000000" middleValue="-50.000000" destinationValue="-100.000000" originOffset="0.000000" middleOffset1="35.000000" middleOffset2="35.000000" destinationOffset="99.000000" />
              <WhammyExtend />
              <Variation>2</Variation>
              <FreeText><![CDATA[Lead phrase]]></FreeText>
              <Legato origin="true" destination="false" />
              <Notes>200</Notes>
              <Properties>
                <Property name="PrimaryPickupVolume"><Float>0.500000</Float></Property>
              </Properties>
              <Lyrics>
                <Line><![CDATA[Hello]]></Line>
              </Lyrics>
              <XProperties>
                <XProperty id="687931393"><Int>234</Int></XProperty>
                <XProperty id="687931394"><Float>1</Float></XProperty>
              </XProperties>
            </Beat>
          </Beats>
          <Notes>
            <Note id="200">
              <Trill>7</Trill>
              <Properties>
                <Property name="Fret"><Fret>5</Fret></Property>
                <Property name="Midi"><Number>69</Number></Property>
                <Property name="String"><String>0</String></Property>
              </Properties>
              <XProperties>
                <XProperty id="688062467"><Int>480</Int></XProperty>
                <XProperty id="688062468"><Float>1.250000</Float></XProperty>
              </XProperties>
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
    public async Task Notation_and_xproperty_passthrough_round_trip_through_json_and_write()
    {
        var gpif = BuildGpif();
        var score = await DeserializeAndMap(gpif);

        var measure = score.Tracks[0].PrimaryMeasure(0);
        var timelineBar = score.TimelineBars[0];
        var beat = measure.Beats[0];
        var note = beat.Notes[0];

        timelineBar.DoubleBar.Should().BeTrue();
        timelineBar.TripletFeel.Should().Be("Triplet8th");
        timelineBar.RepeatStart.Should().BeFalse();
        timelineBar.RepeatStartAttributePresent.Should().BeTrue();
        timelineBar.RepeatEnd.Should().BeFalse();
        timelineBar.RepeatEndAttributePresent.Should().BeTrue();
        timelineBar.RepeatCount.Should().Be(0);
        timelineBar.RepeatCountAttributePresent.Should().BeTrue();
        timelineBar.XProperties.Should().Contain("1124073985", 2);
        measure.BarXProperties.Should().Contain("1124139521", 7);
        beat.Hairpin.Should().Be("Crescendo");
        BeatMetadataOf(beat).Variation.Should().Be("2");
        beat.Ottavia.Should().Be("8va");
        beat.LegatoOrigin.Should().BeTrue();
        beat.LegatoDestination.Should().BeFalse();
        BeatMetadataOf(beat).LyricsXml.Should().Contain("<Lyrics>");
        BeatMetadataOf(beat).WhammyUsesElement.Should().BeTrue();
        BeatMetadataOf(beat).WhammyExtendUsesElement.Should().BeTrue();
        beat.WhammyBar.Should().NotBeNull();
        beat.XProperties.Should().Contain("687931393", 234);
        NoteMetadataOf(note).SourceFret.Should().Be(5);
        NoteMetadataOf(note).SourceStringNumber.Should().Be(0);
        note.XProperties.Should().Contain("688062467", 480);
        NoteMetadataOf(note).XPropertiesXml.Should().Contain("688062468");
        note.Articulation.TrillSpeed.Should().Be(TrillSpeedKind.Sixteenth);

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var masterBar = roundTrip.Root!.Element("MasterBars")!.Element("MasterBar")!;
        var bar = roundTrip.Root.Element("Bars")!.Element("Bar")!;
        var outputBeat = roundTrip.Root.Element("Beats")!.Element("Beat")!;
        var outputNote = roundTrip.Root.Element("Notes")!.Element("Note")!;

        masterBar.Element("DoubleBar").Should().NotBeNull();
        masterBar.Element("TripletFeel")!.Value.Should().Be("Triplet8th");
        masterBar.Element("Repeat")!.Attribute("start")!.Value.Should().Be("false");
        masterBar.Element("Repeat")!.Attribute("end")!.Value.Should().Be("false");
        masterBar.Element("Repeat")!.Attribute("count")!.Value.Should().Be("0");
        masterBar.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("<Double>1.020630</Double>");
        masterBar.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="1124073985"><Int>2</Int></XProperty>""");

        bar.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("<Double>0.965927</Double>");
        bar.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="1124139521"><Int>7</Int></XProperty>""");

        outputBeat.Element("Hairpin")!.Value.Should().Be("Crescendo");
        outputBeat.Element("Variation")!.Value.Should().Be("2");
        outputBeat.Element("Ottavia")!.Value.Should().Be("8va");
        outputBeat.Element("Legato")!.Attribute("origin")!.Value.Should().Be("true");
        outputBeat.Element("Legato")!.Attribute("destination")!.Value.Should().Be("false");
        outputBeat.Element("Lyrics")!.Element("Line")!.Value.Should().Be("Hello");
        outputBeat.Element("Whammy")!.Attribute("originValue")!.Value.Should().Be("0.000000");
        outputBeat.Element("Whammy")!.Attribute("middleValue")!.Value.Should().Be("-50.000000");
        outputBeat.Element("Whammy")!.Attribute("destinationOffset")!.Value.Should().Be("99.000000");
        outputBeat.Element("WhammyExtend").Should().NotBeNull();
        outputBeat.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="687931393"><Int>234</Int></XProperty>""");
        outputBeat.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="687931394"><Float>1</Float></XProperty>""");

        outputNote.Element("Properties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("<Fret>5</Fret>");
        outputNote.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="688062467"><Int>480</Int></XProperty>""");
        outputNote.Element("XProperties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="688062468"><Float>1.250000</Float></XProperty>""");
    }

    [Fact]
    public async Task Edited_known_xproperties_regenerate_instead_of_reusing_stale_source_xml()
    {
        var sourceScore = await DeserializeAndMap(BuildGpif());
        var beat = sourceScore.Tracks[0].PrimaryMeasure(0).Beats[0];
        var note = beat.Notes[0];

        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Lead",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat
                            {
                                Id = beat.Id,
                                Duration = 0.25m,
                                BrushDurationTicks = 120,
                                XProperties = beat.XProperties,
                                Notes =
                                [
                                    new Note
                                    {
                                        Id = note.Id,
                                        MidiPitch = 69,
                                        XProperties = note.XProperties,
                                        Articulation = new NoteArticulation
                                        {
                                            TrillSpeed = TrillSpeedKind.ThirtySecond
                                        }
                                    }
                                ]
                            }
                        ]
                    })
            ]
        };
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetOrCreateGuitarPro().Metadata.BrushDurationXPropertyId = BeatMetadataOf(beat).BrushDurationXPropertyId;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetOrCreateGuitarPro().Metadata.XPropertiesXml = BeatMetadataOf(beat).XPropertiesXml;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0].GetOrCreateGuitarPro().Metadata.XPropertiesXml = NoteMetadataOf(note).XPropertiesXml;

        var roundTrip = await RoundTripThroughWrite(score);
        var outputBeatXProperties = roundTrip.Root!.Element("Beats")!.Element("Beat")!.Element("XProperties")!;
        var outputNoteXProperties = roundTrip.Root.Element("Notes")!.Element("Note")!.Element("XProperties")!;

        outputBeatXProperties.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="687931393"><Int>120</Int></XProperty>""");
        outputBeatXProperties.ToString(SaveOptions.DisableFormatting).Should().NotContain("687931394");
        outputNoteXProperties.ToString(SaveOptions.DisableFormatting).Should().Contain("""<XProperty id="688062467"><Int>120</Int></XProperty>""");
        outputNoteXProperties.ToString(SaveOptions.DisableFormatting).Should().NotContain("688062468");
    }

    [Fact]
    public async Task Edited_note_pitch_regenerates_fret_instead_of_reusing_stale_source_value()
    {
        var sourceScore = await DeserializeAndMap(BuildGpif());
        var beat = sourceScore.Tracks[0].PrimaryMeasure(0).Beats[0];
        var note = beat.Notes[0];

        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Lead",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat
                            {
                                Id = beat.Id,
                                Duration = beat.Duration,
                                Notes =
                                [
                                    new Note
                                    {
                                        Id = note.Id,
                                        MidiPitch = 71,
                                        StringNumber = note.StringNumber
                                    }
                                ]
                            }
                        ]
                    })
            ]
        };
        score.Tracks[0].GetOrCreateGuitarPro().Metadata = sourceScore.Tracks[0].GetRequiredGuitarPro().Metadata;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0].GetOrCreateGuitarPro().Metadata.SourceMidiPitch = NoteMetadataOf(note).SourceMidiPitch;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0].GetOrCreateGuitarPro().Metadata.SourceTransposedMidiPitch = NoteMetadataOf(note).SourceTransposedMidiPitch;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0].GetOrCreateGuitarPro().Metadata.SourceFret = NoteMetadataOf(note).SourceFret;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0].GetOrCreateGuitarPro().Metadata.SourceStringNumber = NoteMetadataOf(note).SourceStringNumber;

        var roundTrip = await RoundTripThroughWrite(score);
        var outputNote = roundTrip.Root!.Element("Notes")!.Element("Note")!;
        outputNote.Element("Properties")!.ToString(SaveOptions.DisableFormatting).Should().Contain("<Fret>9</Fret>");
    }

    [Fact]
    public async Task Source_brush_related_beat_xproperties_round_trip_without_being_collapsed()
    {
        const string gpif = """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <Tracks><Track id="0"><Name>Lead</Name></Track></Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats>
            <Beat id="100">
              <Rhythm ref="1000" />
              <XProperties>
                <XProperty id="687931393"><Int>126</Int></XProperty>
                <XProperty id="687931394"><Float>0.959596</Float></XProperty>
                <XProperty id="687935489"><Int>240</Int></XProperty>
                <XProperty id="687935490"><Float>0.555556</Float></XProperty>
              </XProperties>
            </Beat>
          </Beats>
          <Notes />
        </GPIF>
        """;

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var outputBeatXProperties = roundTrip.Root!
            .Element("Beats")!
            .Element("Beat")!
            .Element("XProperties")!
            .ToString(SaveOptions.DisableFormatting);

        outputBeatXProperties.Should().Contain("""<XProperty id="687931393"><Int>126</Int></XProperty>""");
        outputBeatXProperties.Should().Contain("""<XProperty id="687931394"><Float>0.959596</Float></XProperty>""");
        outputBeatXProperties.Should().Contain("""<XProperty id="687935489"><Int>240</Int></XProperty>""");
        outputBeatXProperties.Should().Contain("""<XProperty id="687935490"><Float>0.555556</Float></XProperty>""");
    }
}
