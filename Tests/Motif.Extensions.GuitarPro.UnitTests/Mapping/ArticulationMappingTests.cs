namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Implementation;
using System.Text;

public class ArticulationMappingTests
{
    [Fact]
    public async Task Mapper_captures_note_articulation_fields_from_gpif()
    {
        const string gpif = """
<GPIF>
  <Score>
    <Title>T</Title>
    <Artist>A</Artist>
    <Album>B</Album>
  </Score>
  <Tracks>
    <Track id="0"><Name>Guitar</Name></Track>
  </Tracks>
  <MasterBars>
    <MasterBar>
      <Time>4/4</Time>
      <Bars>1</Bars>
    </MasterBar>
  </MasterBars>
  <Bars>
    <Bar id="1"><Voices>10</Voices></Bar>
  </Bars>
  <Voices>
    <Voice id="10"><Beats>100</Beats></Voice>
  </Voices>
  <Rhythms>
    <Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm>
  </Rhythms>
  <Beats>
    <Beat id="100">
      <Rhythm ref="1000" />
      <Notes>10000</Notes>
    </Beat>
  </Beats>
  <Notes>
    <Note id="10000">
      <Accent>2</Accent>
      <InstrumentArticulation>5</InstrumentArticulation>
      <LetRing />
      <Tie origin="true" destination="false" />
      <Trill>7</Trill>
      <Vibrato>Wide</Vibrato>
      <Properties>
        <Property name="Pitch">
          <Pitch>
            <Step>E</Step>
            <Accidental>#</Accidental>
            <Octave>4</Octave>
          </Pitch>
        </Property>
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

        var note = score.Tracks[0].PrimaryMeasure(0).Beats[0].Notes[0];
        note.MidiPitch.Should().Be(53); // E#4
        note.Articulation.LetRing.Should().BeTrue();
        note.Articulation.Vibrato.Should().Be("Wide");
        note.Articulation.TieOrigin.Should().BeTrue();
        note.Articulation.TieDestination.Should().BeFalse();
        note.Articulation.Trill.Should().Be(7);
        note.Articulation.Accent.Should().Be(2);
        note.GetRequiredGuitarPro().Metadata.InstrumentArticulation.Should().Be(5);
    }
}
