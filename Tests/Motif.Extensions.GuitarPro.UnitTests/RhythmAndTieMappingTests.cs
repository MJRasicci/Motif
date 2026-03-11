namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.Text;

public class RhythmAndTieMappingTests
{
    private static GpRhythmShapeMetadata SourceRhythmOf(Beat beat)
        => beat.GetRequiredGuitarPro().Metadata.SourceRhythm!;

    [Fact]
    public async Task Mapper_applies_augmentation_dots_and_tuplets_to_duration()
    {
        const string gpif = """
<GPIF>
  <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
  <Tracks><Track id="0"><Name>Guitar</Name></Track></Tracks>
  <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
  <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
  <Voices><Voice id="10"><Beats>100 101</Beats></Voice></Voices>
  <Rhythms>
    <Rhythm id="1000">
      <NoteValue>Quarter</NoteValue>
      <AugmentationDot />
    </Rhythm>
    <Rhythm id="1001">
      <NoteValue>Eighth</NoteValue>
      <PrimaryTuplet><Num>3</Num><Den>2</Den></PrimaryTuplet>
    </Rhythm>
  </Rhythms>
  <Beats>
    <Beat id="100"><Rhythm ref="1000" /><Notes>200</Notes></Beat>
    <Beat id="101"><Rhythm ref="1001" /><Notes>201</Notes></Beat>
  </Beats>
  <Notes>
    <Note id="200"><Properties><Property name="Pitch"><Pitch><Step>C</Step><Octave>4</Octave></Pitch></Property></Properties></Note>
    <Note id="201"><Properties><Property name="Pitch"><Pitch><Step>D</Step><Octave>4</Octave></Pitch></Property></Properties></Note>
  </Notes>
</GPIF>
""";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var deserializer = new XmlGpifDeserializer();
        var raw = await deserializer.DeserializeAsync(stream, TestContext.Current.CancellationToken);
        var mapper = new DefaultScoreMapper();
        var score = await mapper.MapAsync(raw, TestContext.Current.CancellationToken);

        var beats = score.Tracks[0].PrimaryMeasure(0).Beats;
        beats[0].Duration.Should().Be(0.375m); // dotted quarter
        beats[1].Duration.Should().BeApproximately(1m / 12m, 0.00001m); // triplet eighth
    }

    [Fact]
    public async Task Mapper_preserves_grouped_augmentation_dot_count_values()
    {
        const string gpif = """
<GPIF>
  <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
  <Tracks><Track id="0"><Name>Guitar</Name></Track></Tracks>
  <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
  <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
  <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
  <Rhythms>
    <Rhythm id="1000">
      <NoteValue>Quarter</NoteValue>
      <AugmentationDot count="2" />
    </Rhythm>
  </Rhythms>
  <Beats>
    <Beat id="100"><Rhythm ref="1000" /><Notes>200</Notes></Beat>
  </Beats>
  <Notes>
    <Note id="200"><Properties><Property name="Midi"><Number>60</Number></Property></Properties></Note>
  </Notes>
</GPIF>
""";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var deserializer = new XmlGpifDeserializer();
        var raw = await deserializer.DeserializeAsync(stream, TestContext.Current.CancellationToken);
        var mapper = new DefaultScoreMapper();
        var score = await mapper.MapAsync(raw, TestContext.Current.CancellationToken);

        raw.RhythmsById[1000].AugmentationDots.Should().Be(2);
        raw.RhythmsById[1000].AugmentationDotCounts.Should().Equal(2);
        SourceRhythmOf(score.Tracks[0].PrimaryMeasure(0).Beats[0]).AugmentationDots.Should().Be(2);
        SourceRhythmOf(score.Tracks[0].PrimaryMeasure(0).Beats[0]).AugmentationDotCounts.Should().Equal(2);
        score.Tracks[0].PrimaryMeasure(0).Beats[0].Duration.Should().Be(0.4375m);
    }

    [Fact]
    public async Task Mapper_stitches_tied_note_duration_across_beats()
    {
        const string gpif = """
<GPIF>
  <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
  <Tracks><Track id="0"><Name>Guitar</Name></Track></Tracks>
  <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
  <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
  <Voices><Voice id="10"><Beats>100 101</Beats></Voice></Voices>
  <Rhythms>
    <Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm>
  </Rhythms>
  <Beats>
    <Beat id="100"><Rhythm ref="1000" /><Notes>200</Notes></Beat>
    <Beat id="101"><Rhythm ref="1000" /><Notes>201</Notes></Beat>
  </Beats>
  <Notes>
    <Note id="200">
      <Tie origin="true" destination="false" />
      <Properties><Property name="Pitch"><Pitch><Step>C</Step><Octave>4</Octave></Pitch></Property></Properties>
    </Note>
    <Note id="201">
      <Tie origin="false" destination="true" />
      <Properties><Property name="Pitch"><Pitch><Step>C</Step><Octave>4</Octave></Pitch></Property></Properties>
    </Note>
  </Notes>
</GPIF>
""";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var deserializer = new XmlGpifDeserializer();
        var raw = await deserializer.DeserializeAsync(stream, TestContext.Current.CancellationToken);
        var mapper = new DefaultScoreMapper();
        var score = await mapper.MapAsync(raw, TestContext.Current.CancellationToken);

        var notes = score.Tracks[0].PrimaryMeasure(0).Beats.SelectMany(b => b.Notes).ToArray();
        notes[0].Duration.Should().Be(0.5m);
        notes[1].TieExtendedFromPrevious.Should().BeTrue();
    }
}
