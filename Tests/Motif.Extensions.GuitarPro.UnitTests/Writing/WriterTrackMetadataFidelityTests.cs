namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public class WriterTrackMetadataFidelityTests
{
    private static string BuildGpif(string trackBody)
    {
        return $"""
        <GPIF>
          <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
          <MasterTrack><Tracks>0</Tracks></MasterTrack>
          <Tracks>
            <Track id="0">
              <Name>Track</Name>
              {trackBody}
            </Track>
          </Tracks>
          <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
          <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
          <Voices><Voice id="10"><Beats></Beats></Voice></Voices>
          <Beats />
          <Notes />
          <Rhythms />
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
        return await RoundTripThroughWrite(fromJson!);
    }

    [Fact]
    public async Task Track_metadata_passthrough_round_trips_empty_systems_layout_notation_patch_and_staff_tuning_shape()
    {
        var gpif = BuildGpif("""
            <ShortName></ShortName>
            <SystemsLayout></SystemsLayout>
            <NotationPatch><Name>Drumkit-Standard</Name></NotationPatch>
            <Staves>
              <Staff>
                <Properties>
                  <Property name="Tuning">
                    <Pitches>0 0 0 0 0 0</Pitches>
                    <Flat />
                    <Instrument>Undefined</Instrument>
                    <Label />
                    <LabelVisible>true</LabelVisible>
                  </Property>
                </Properties>
              </Staff>
            </Staves>
            """);

        var score = await DeserializeAndMap(gpif);
        var track = score.Tracks[0];
        var trackMetadata = track.GetRequiredGuitarPro().Metadata;

        trackMetadata.ShortName.Should().BeEmpty();
        trackMetadata.HasExplicitEmptyShortName.Should().BeTrue();
        trackMetadata.SystemsLayout.Should().BeEmpty();
        trackMetadata.HasExplicitEmptySystemsLayout.Should().BeTrue();
        trackMetadata.NotationPatchXml.Should().Contain("<NotationPatch>");
        trackMetadata.HasTrackTuningProperty.Should().BeFalse();
        trackMetadata.TuningPitches.Should().Equal(0, 0, 0, 0, 0, 0);
        trackMetadata.TuningInstrument.Should().Be("Undefined");
        trackMetadata.TuningLabelVisible.Should().BeTrue();

        var roundTrip = await RoundTripThroughWrite(score);
        var outputTrack = roundTrip.Root!.Element("Tracks")!.Element("Track")!;

        outputTrack.Element("ShortName").Should().NotBeNull();
        outputTrack.Element("ShortName")!.Value.Should().BeEmpty();
        outputTrack.Element("SystemsLayout").Should().NotBeNull();
        outputTrack.Element("SystemsLayout")!.Value.Should().BeEmpty();
        outputTrack.Element("NotationPatch").Should().NotBeNull();
        outputTrack.Element("NotationPatch")!.Element("Name")!.Value.Should().Be("Drumkit-Standard");
        outputTrack.Element("Properties").Should().BeNull();

        var tuningProperty = outputTrack.Element("Staves")!
            .Element("Staff")!
            .Element("Properties")!
            .Elements("Property")
            .Single(p => (string?)p.Attribute("name") == "Tuning");

        tuningProperty.Element("Pitches")!.Value.Should().Be("0 0 0 0 0 0");
        tuningProperty.Element("Instrument")!.Value.Should().Be("Undefined");
        tuningProperty.Element("Label")!.Value.Should().BeEmpty();
        tuningProperty.Element("LabelVisible")!.Value.Should().Be("true");
    }

    [Fact]
    public async Task Core_json_round_trip_drops_track_guitar_pro_extension_metadata()
    {
        var gpif = BuildGpif("<ShortName>abbr</ShortName>");

        var score = await DeserializeAndMap(gpif);
        var json = score.ToJson(indented: false);
        var fromJson = JsonSerializer.Deserialize<Score>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        fromJson.Should().NotBeNull();
        score.Tracks[0].GetGuitarPro().Should().NotBeNull();
        fromJson!.Tracks[0].GetGuitarPro().Should().BeNull();
    }

    [Fact]
    public async Task Generated_track_without_raw_staves_emits_top_level_tuning_property_as_fallback()
    {
        var score = new Score
        {
            Tracks =
            [
                new Track
                {
                    Id = 0,
                    Name = "Guitar"
                }
            ]
        };
        score.Tracks[0].GetOrCreateGuitarPro().Metadata = new TrackMetadata
        {
            TuningPitches = [40, 45, 50, 55, 59, 64],
            TuningInstrument = "Guitar",
            TuningLabel = "Std",
            TuningLabelVisible = true
        };

        var roundTrip = await RoundTripThroughWrite(score);
        var tuningProperty = roundTrip.Root!
            .Element("Tracks")!
            .Element("Track")!
            .Element("Properties")!
            .Elements("Property")
            .Single(p => (string?)p.Attribute("name") == "Tuning");

        tuningProperty.Element("Pitches")!.Value.Should().Be("40 45 50 55 59 64");
        tuningProperty.Element("Instrument")!.Value.Should().Be("Guitar");
        tuningProperty.Element("Label")!.Value.Should().Be("Std");
        tuningProperty.Element("LabelVisible")!.Value.Should().Be("true");
    }
}
