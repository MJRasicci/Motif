namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;
using System.Text;

public class HopoSemanticsMappingTests
{
    [Fact]
    public async Task Mapper_infers_hammer_on_and_pull_off_links_from_adjacent_beats()
    {
        const string gpif = """
<GPIF>
  <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
  <Tracks>
    <Track id="0">
      <Name>Guitar</Name>
      <Properties>
        <Property name="Tuning"><Pitches>64 59 55 50 45 40</Pitches></Property>
      </Properties>
    </Track>
  </Tracks>
  <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
  <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
  <Voices><Voice id="10"><Beats>100 101 102 103</Beats></Voice></Voices>
  <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
  <Beats>
    <Beat id="100"><Rhythm ref="1000" /><Notes>200</Notes></Beat>
    <Beat id="101"><Rhythm ref="1000" /><Notes>201</Notes></Beat>
    <Beat id="102"><Rhythm ref="1000" /><Notes>202</Notes></Beat>
    <Beat id="103"><Rhythm ref="1000" /><Notes>203</Notes></Beat>
  </Beats>
  <Notes>
    <Note id="200">
      <Properties>
        <Property name="Pitch"><Pitch><Step>E</Step><Octave>4</Octave></Pitch></Property>
        <Property name="String"><String>2</String></Property>
        <Property name="HopoOrigin"><Enable /></Property>
      </Properties>
    </Note>
    <Note id="201">
      <Properties>
        <Property name="Pitch"><Pitch><Step>G</Step><Octave>4</Octave></Pitch></Property>
        <Property name="String"><String>2</String></Property>
        <Property name="HopoDestination"><Enable /></Property>
      </Properties>
    </Note>
    <Note id="202">
      <Properties>
        <Property name="Pitch"><Pitch><Step>G</Step><Octave>4</Octave></Pitch></Property>
        <Property name="String"><String>2</String></Property>
        <Property name="HopoOrigin"><Enable /></Property>
      </Properties>
    </Note>
    <Note id="203">
      <Properties>
        <Property name="Pitch"><Pitch><Step>F</Step><Octave>4</Octave></Pitch></Property>
        <Property name="String"><String>2</String></Property>
        <Property name="HopoDestination"><Enable /></Property>
      </Properties>
    </Note>
  </Notes>
</GPIF>
""";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var deserializer = new XmlGpifDeserializer();
        var raw = await deserializer.DeserializeAsync(stream, TestContext.Current.CancellationToken);
        var mapper = new DefaultScoreMapper();
        var score = await mapper.MapAsync(raw, TestContext.Current.CancellationToken);

        var notes = score.Tracks[0]
            .PrimaryMeasure(0)
            .Voices[0]
            .Beats
            .SelectMany(b => b.Notes)
            .ToDictionary(n => n.Id);

        notes[200].StringNumber.Should().Be(2);
        notes[201].StringNumber.Should().Be(2);

        notes[200].Articulation.HopoDestinationNoteId.Should().Be(201);
        notes[200].Articulation.HopoType.Should().Be(HopoTypeKind.HammerOn);

        notes[201].Articulation.HopoOriginNoteId.Should().Be(200);
        notes[201].Articulation.HopoType.Should().Be(HopoTypeKind.HammerOn);

        notes[202].Articulation.HopoDestinationNoteId.Should().Be(203);
        notes[202].Articulation.HopoType.Should().Be(HopoTypeKind.PullOff);

        notes[203].Articulation.HopoOriginNoteId.Should().Be(202);
        notes[203].Articulation.HopoType.Should().Be(HopoTypeKind.PullOff);
    }
}
