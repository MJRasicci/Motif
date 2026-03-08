namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public class WriterRemainingFidelityTests
{
    private static async Task<GuitarProScore> DeserializeAndMap(string gpif)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var raw = await new XmlGpifDeserializer().DeserializeAsync(stream, TestContext.Current.CancellationToken);
        return await new DefaultScoreMapper().MapAsync(raw, TestContext.Current.CancellationToken);
    }

    private static async Task<XDocument> RoundTripThroughWrite(GuitarProScore score)
    {
        var xml = await RoundTripThroughWriteText(score);
        return XDocument.Parse(xml);
    }

    private static async Task<string> RoundTripThroughWriteText(GuitarProScore score)
    {
        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);
        await using var stream = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(result.RawDocument, stream, TestContext.Current.CancellationToken);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async Task<XDocument> RoundTripThroughJsonAndWrite(string gpif)
    {
        var xml = await RoundTripThroughJsonAndWriteText(gpif);
        return XDocument.Parse(xml);
    }

    private static async Task<string> RoundTripThroughJsonAndWriteText(string gpif)
    {
        var score = await DeserializeAndMap(gpif);
        var json = score.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<GuitarProScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();
        return await RoundTripThroughWriteText(fromJson!);
    }

    [Fact]
    public async Task Score_and_master_bar_passthrough_round_trip_gp_revision_page_setup_whitespace_and_direction_shapes()
    {
        var gpif = """
        <GPIF>
          <GPVersion>7.0.0</GPVersion>
          <GPRevision>12015</GPRevision>
          <Score>
            <Title>T</Title>
            <Artist>A</Artist>
            <Album>B</Album>
            <SubTitle><![CDATA[ ]]></SubTitle>
            <Copyright><![CDATA[   ]]></Copyright>
            <Tabber><![CDATA[ ]]></Tabber>
            <PageSetup>
              <Width>210</Width>
              <Height>297</Height>
              <Orientation>Portrait</Orientation>
              <TopMargin>15</TopMargin>
              <LeftMargin>10</LeftMargin>
              <RightMargin>10</RightMargin>
              <BottomMargin>10</BottomMargin>
              <Scale>1</Scale>
            </PageSetup>
            <MultiVoice>0</MultiVoice>
          </Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks><Track id="0"><Name>Track</Name></Track></Tracks>
          <MasterBars>
            <MasterBar>
              <Time>4/4</Time>
              <FreeTime />
              <Section>
                <Letter><![CDATA[]]></Letter>
                <Text><![CDATA[]]></Text>
              </Section>
              <Directions>
                <Jump>DaCoda</Jump>
                <Jump>DaDoubleCoda</Jump>
                <Target>Segno</Target>
                <Target>SegnoSegno</Target>
              </Directions>
              <Bars>1</Bars>
            </MasterBar>
          </MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats><Beat id="100"><Rhythm ref="1000" /></Beat></Beats>
          <Notes />
        </GPIF>
        """;

        var score = await DeserializeAndMap(gpif);
        var measure = score.Tracks[0].Measures[0];

        score.Metadata.GpRevisionXml.Should().Contain("<GPRevision>12015</GPRevision>");
        score.Metadata.PageSetupXml.Should().Contain("<PageSetup>");
        score.Metadata.SubTitle.Should().Be(" ");
        score.Metadata.Copyright.Should().Be("   ");
        score.Metadata.Tabber.Should().Be(" ");
        measure.FreeTime.Should().BeTrue();
        measure.HasExplicitEmptySection.Should().BeTrue();
        measure.DirectionsXml.Should().Contain("<Jump>DaCoda</Jump>");
        measure.DirectionsXml.Should().Contain("<Target>SegnoSegno</Target>");

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var root = roundTrip.Root!;
        var gpRevision = root.Element("GPRevision")!;
        var scoreElement = root.Element("Score")!;
        var masterBar = root.Element("MasterBars")!.Element("MasterBar")!;
        var directions = masterBar.Element("Directions")!;

        gpRevision.Attribute("required").Should().BeNull();
        gpRevision.Attribute("recommended").Should().BeNull();
        gpRevision.Value.Should().Be("12015");
        scoreElement.Element("SubTitle")!.Value.Should().Be(" ");
        scoreElement.Element("Copyright")!.Value.Should().Be("   ");
        scoreElement.Element("Tabber")!.Value.Should().Be(" ");
        scoreElement.Element("PageSetup")!.Element("Width")!.Value.Should().Be("210");
        masterBar.Element("FreeTime").Should().NotBeNull();
        masterBar.Element("Section")!.Element("Letter")!.Value.Should().BeEmpty();
        masterBar.Element("Section")!.Element("Text")!.Value.Should().BeEmpty();
        directions.Elements("Jump").Select(element => element.Value).Should().Equal("DaCoda", "DaDoubleCoda");
        directions.Elements("Target").Select(element => element.Value).Should().Equal("Segno", "SegnoSegno");
    }

    [Fact]
    public async Task Score_metadata_cdata_round_trips_through_json_write_path()
    {
        const string gpif = """
        <GPIF>
          <Score>
            <Title><![CDATA[Song Title]]></Title>
            <Artist><![CDATA[Artist]]></Artist>
            <Album><![CDATA[Album]]></Album>
            <Words><![CDATA[Words]]></Words>
            <Music><![CDATA[Music]]></Music>
            <WordsAndMusic><![CDATA[Words & Music]]></WordsAndMusic>
            <Copyright><![CDATA[(c) test]]></Copyright>
            <PageFooter><![CDATA[<html>Page %page%/%pages%</html>]]></PageFooter>
            <ScoreSystemsDefaultLayout>4</ScoreSystemsDefaultLayout>
            <MultiVoice>0</MultiVoice>
          </Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks><Track id="0"><Name>Track</Name></Track></Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats><Beat id="100"><Rhythm ref="1000" /></Beat></Beats>
          <Notes />
        </GPIF>
        """;

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var scoreElement = roundTrip.Root!.Element("Score")!;

        scoreElement.Element("Title")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Song Title");
        scoreElement.Element("Artist")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Artist");
        scoreElement.Element("Album")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Album");
        scoreElement.Element("Words")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Words");
        scoreElement.Element("Music")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Music");
        scoreElement.Element("WordsAndMusic")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Words & Music");
        scoreElement.Element("Copyright")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("(c) test");
        scoreElement.Element("PageFooter")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("<html>Page %page%/%pages%</html>");
        scoreElement.Element("ScoreSystemsDefaultLayout")!.Nodes().OfType<XCData>().Should().BeEmpty();
        scoreElement.Element("MultiVoice")!.Nodes().OfType<XCData>().Should().BeEmpty();
    }

    [Fact]
    public async Task Empty_section_cdata_with_internal_line_breaks_round_trips_as_empty()
    {
        const string gpif = """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks><Track id="0"><Name>Track</Name></Track></Tracks>
          <MasterBars>
            <MasterBar>
              <Time>4/4</Time>
              <Section>
                <Letter>
                  <![CDATA[]]>
                </Letter>
                <Text>
                  <![CDATA[]]>
                </Text>
              </Section>
              <Bars>1</Bars>
            </MasterBar>
          </MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats><Beat id="100"><Rhythm ref="1000" /></Beat></Beats>
          <Notes />
        </GPIF>
        """;

        var score = await DeserializeAndMap(gpif);
        var measure = score.Tracks[0].Measures[0];

        measure.SectionLetter.Should().BeEmpty();
        measure.SectionText.Should().BeEmpty();
        measure.HasExplicitEmptySection.Should().BeTrue();

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var section = roundTrip.Root!.Element("MasterBars")!.Element("MasterBar")!.Element("Section")!;
        section.Element("Letter")!.Value.Should().BeEmpty();
        section.Element("Text")!.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Packed_structural_node_shapes_round_trip_through_json_write_path()
    {
        const string gpif = """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks><Track id="0"><Name>Track</Name></Track></Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><FreeTime /><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices><Clef>G2</Clef></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Beats><Beat id="100"><Rhythm ref="1000" /><FreeText><![CDATA[Dist.]]></FreeText><Notes>200</Notes></Beat></Beats>
          <Notes><Note id="200"><Properties><Property name="ConcertPitch"><Pitch><Step>E</Step><Accidental></Accidental><Octave>4</Octave></Pitch></Property><Property name="Midi"><Number>64</Number></Property><Property name="String"><String>1</String></Property><Property name="Fret"><Fret>0</Fret></Property></Properties></Note></Notes>
          <Rhythms><Rhythm id="1000"><NoteValue>Eighth</NoteValue><AugmentationDot count="1" /></Rhythm></Rhythms>
        </GPIF>
        """;

        var score = await DeserializeAndMap(gpif);
        var json = score.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<GuitarProScore>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();
        score.Metadata.ScoreXml.Should().Contain("<Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>");
        score.MasterTrack.Xml.Should().Contain("<MasterTrack><Tracks>0</Tracks></MasterTrack>");
        score.Tracks[0].Metadata.Xml.Should().Contain("<Track id=\"0\"><Name>Track</Name></Track>");
        score.Tracks[0].Measures[0].MasterBarXml.Should().Contain("<MasterBar><Time>4/4</Time><FreeTime /><Bars>1</Bars></MasterBar>");
        score.Tracks[0].Measures[0].Voices[0].Xml.Should().Contain("<Voice id=\"10\"><Beats>100</Beats></Voice>");
        score.Tracks[0].Measures[0].Beats[0].Xml.Should().Contain("<Beat id=\"100\"><Rhythm ref=\"1000\" /><FreeText><![CDATA[Dist.]]></FreeText><Notes>200</Notes></Beat>");
        score.Tracks[0].Measures[0].Beats[0].SourceRhythm!.Xml.Should().Contain("<Rhythm id=\"1000\"><NoteValue>Eighth</NoteValue><AugmentationDot count=\"1\" /></Rhythm>");
        fromJson!.Metadata.ScoreXml.Should().Contain("<Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>");
        fromJson.MasterTrack.Xml.Should().Contain("<MasterTrack><Tracks>0</Tracks></MasterTrack>");
        fromJson.Tracks[0].Metadata.Xml.Should().Contain("<Track id=\"0\"><Name>Track</Name></Track>");
        fromJson!.Tracks[0].Measures[0].MasterBarXml.Should().Contain("<MasterBar><Time>4/4</Time><FreeTime /><Bars>1</Bars></MasterBar>");
        fromJson.Tracks[0].Measures[0].Voices[0].Xml.Should().Contain("<Voice id=\"10\"><Beats>100</Beats></Voice>");
        fromJson.Tracks[0].Measures[0].Beats[0].Xml.Should().Contain("<Beat id=\"100\"><Rhythm ref=\"1000\" /><FreeText><![CDATA[Dist.]]></FreeText><Notes>200</Notes></Beat>");
        fromJson.Tracks[0].Measures[0].Beats[0].SourceRhythm!.Xml.Should().Contain("<Rhythm id=\"1000\"><NoteValue>Eighth</NoteValue><AugmentationDot count=\"1\" /></Rhythm>");

        var xml = await RoundTripThroughWriteText(fromJson);

        xml.Should().Contain("<Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>");
        xml.Should().Contain("<MasterTrack><Tracks>0</Tracks></MasterTrack>");
        xml.Should().Contain("<Track id=\"0\"><Name>Track</Name></Track>");
        xml.Should().Contain("<MasterBar><Time>4/4</Time><FreeTime /><Bars>1</Bars></MasterBar>");
        xml.Should().Contain("<Voice id=\"10\"><Beats>100</Beats></Voice>");
        xml.Should().Contain("<Beat id=\"100\"><Rhythm ref=\"1000\" /><FreeText><![CDATA[Dist.]]></FreeText><Notes>200</Notes></Beat>");
        xml.Should().Contain("<Rhythm id=\"1000\"><NoteValue>Eighth</NoteValue><AugmentationDot count=\"1\" /></Rhythm>");
    }

    [Fact]
    public async Task Track_and_bar_notation_passthrough_round_trip_let_ring_throughout_and_simile_mark()
    {
        var gpif = """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks>
            <Track id="0">
              <Name>Track</Name>
              <LetRingThroughout />
            </Track>
          </Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars>
            <Bar id="1">
              <Voices>10</Voices>
              <SimileMark>Simple</SimileMark>
            </Bar>
          </Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats><Beat id="100"><Rhythm ref="1000" /></Beat></Beats>
          <Notes />
        </GPIF>
        """;

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Metadata.LetRingThroughout.Should().BeTrue();
        score.Tracks[0].Measures[0].SimileMark.Should().Be("Simple");

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var track = roundTrip.Root!.Element("Tracks")!.Element("Track")!;
        var bar = roundTrip.Root.Element("Bars")!.Element("Bar")!;

        track.Element("LetRingThroughout").Should().NotBeNull();
        bar.Element("SimileMark")!.Value.Should().Be("Simple");
    }

    [Fact]
    public async Task Master_track_automation_and_top_level_media_passthrough_round_trip_through_json()
    {
        const string gpif = """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <MasterTrack>
            <Tracks>0</Tracks>
            <Automations>
              <Automation>
                <Type>Tempo</Type>
                <Linear>false</Linear>
                <Bar>0</Bar>
                <Position>0.333333</Position>
                <Visible>true</Visible>
                <Text><![CDATA[Lento]]></Text>
                <Value>110 2</Value>
              </Automation>
              <Automation>
                <Type>SyncPoint</Type>
                <Linear>false</Linear>
                <Bar>1</Bar>
                <Position>0.6625</Position>
                <Visible>true</Visible>
                <Value>
                  <BarIndex>3</BarIndex>
                  <BarOccurrence>1</BarOccurrence>
                  <ModifiedTempo>112.118645</ModifiedTempo>
                  <OriginalTempo>110</OriginalTempo>
                  <FrameOffset>70800</FrameOffset>
                </Value>
              </Automation>
            </Automations>
          </MasterTrack>
          <BackingTrack>
            <Name><![CDATA[Audio Track]]></Name>
            <ShortName><![CDATA[a.track]]></ShortName>
          </BackingTrack>
          <AudioTracks>
            <AudioTrack id="0">
              <Name><![CDATA[Mix]]></Name>
            </AudioTrack>
          </AudioTracks>
          <Tracks><Track id="0"><Name>Track</Name></Track></Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats><Beat id="100"><Rhythm ref="1000" /></Beat></Beats>
          <Notes />
          <Assets>
            <Asset id="0">
              <OriginalFilePath><![CDATA[a.ogg]]></OriginalFilePath>
            </Asset>
          </Assets>
        </GPIF>
        """;

        var score = await DeserializeAndMap(gpif);
        score.MasterTrack.AutomationsXml.Should().Contain("<Text><![CDATA[Lento]]></Text>");
        score.MasterTrack.AutomationsXml.Should().Contain("<BarIndex>3</BarIndex>");
        score.Metadata.BackingTrackXml.Should().Contain("<BackingTrack>");
        score.Metadata.AudioTracksXml.Should().Contain("<AudioTrack id=\"0\">");
        score.Metadata.AssetsXml.Should().Contain("<Asset id=\"0\">");

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var root = roundTrip.Root!;
        var automations = root.Element("MasterTrack")!.Element("Automations")!.Elements("Automation").ToArray();

        automations[0].Element("Position")!.Value.Should().Be("0.333333");
        automations[0].Element("Text")!.Value.Should().Be("Lento");
        automations[1].Element("Position")!.Value.Should().Be("0.6625");
        automations[1].Element("Value")!.Element("BarIndex")!.Value.Should().Be("3");
        automations[1].Element("Value")!.Element("FrameOffset")!.Value.Should().Be("70800");
        root.Element("BackingTrack")!.Element("Name")!.Value.Should().Be("Audio Track");
        root.Element("AudioTracks")!.Element("AudioTrack")!.Element("Name")!.Value.Should().Be("Mix");
        root.Element("Assets")!.Element("Asset")!.Element("OriginalFilePath")!.Value.Should().Be("a.ogg");
    }

    [Fact]
    public async Task Brush_direction_without_explicit_xproperty_does_not_gain_brush_duration_xproperties()
    {
        const string gpif = """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks><Track id="0"><Name>Track</Name></Track></Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
          <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
          <Beats>
            <Beat id="100">
              <Rhythm ref="1000" />
              <Properties>
                <Property name="Brush"><Direction>Down</Direction></Property>
              </Properties>
            </Beat>
          </Beats>
          <Notes />
        </GPIF>
        """;

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.Brush.Should().BeTrue();
        beat.BrushDurationTicks.Should().Be(60);
        beat.HasExplicitBrushDurationXProperty.Should().BeFalse();

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var outputBeat = roundTrip.Root!.Element("Beats")!.Element("Beat")!;

        outputBeat.Element("XProperties").Should().BeNull();
        outputBeat.Element("Properties")!
            .ToString(SaveOptions.DisableFormatting)
            .Should().Contain("<Property name=\"Brush\"><Direction>Down</Direction></Property>");
    }

    [Fact]
    public async Task Note_show_string_number_round_trips_through_json_and_write()
    {
        var gpif = """
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
              <Properties>
                <Property name="Midi"><Number>60</Number></Property>
                <Property name="ShowStringNumber"><Enable /></Property>
              </Properties>
            </Note>
          </Notes>
        </GPIF>
        """;

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].Notes[0].ShowStringNumber.Should().BeTrue();

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        var properties = roundTrip.Root!
            .Element("Notes")!
            .Element("Note")!
            .Element("Properties")!
            .Elements("Property")
            .ToDictionary(property => (string)property.Attribute("name")!, property => property);

        properties["ShowStringNumber"].Element("Enable").Should().NotBeNull();
    }

    [Fact]
    public async Task Unmapper_preserves_sparse_bar_voice_slots()
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
                            SourceBarId = 5,
                            Voices =
                            [
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 1,
                                    SourceVoiceId = 10,
                                    Beats =
                                    [
                                        new BeatModel
                                        {
                                            Id = 100,
                                            Duration = 0.25m
                                        }
                                    ]
                                },
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 3,
                                    SourceVoiceId = 11,
                                    Beats =
                                    [
                                        new BeatModel
                                        {
                                            Id = 101,
                                            Duration = 0.25m
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.BarsById[5].VoicesReferenceList.Should().Be("-1 10 -1 11");
    }

    [Fact]
    public async Task Sparse_bar_voice_slots_round_trip_through_json_and_write()
    {
        var gpif = """
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <Tracks><Track id="0"><Name>Guitar</Name></Track></Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>-1 10 -1 11</Voices></Bar></Bars>
          <Voices>
            <Voice id="10"><Beats>100</Beats></Voice>
            <Voice id="11"><Beats>101</Beats></Voice>
          </Voices>
          <Rhythms>
            <Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm>
          </Rhythms>
          <Beats>
            <Beat id="100"><Rhythm ref="1000" /></Beat>
            <Beat id="101"><Rhythm ref="1000" /></Beat>
          </Beats>
          <Notes />
        </GPIF>
        """;

        var roundTrip = await RoundTripThroughJsonAndWrite(gpif);
        roundTrip.Root!
            .Element("Bars")!
            .Element("Bar")!
            .Element("Voices")!
            .Value.Should().Be("-1 10 -1 11");
    }
}
