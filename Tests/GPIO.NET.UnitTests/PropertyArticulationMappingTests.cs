namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using System.Text;

public class PropertyArticulationMappingTests
{
    [Fact]
    public async Task Mapper_captures_property_based_articulations()
    {
        const string gpif = """
<GPIF>
  <Score><Title>T</Title><Artist>A</Artist><Album>B</Album></Score>
  <Tracks><Track id="0"><Name>Guitar</Name></Track></Tracks>
  <MasterBars><MasterBar><Time>4/4</Time><Bars>1</Bars></MasterBar></MasterBars>
  <Bars><Bar id="1"><Voices>10</Voices></Bar></Bars>
  <Voices><Voice id="10"><Beats>100</Beats></Voice></Voices>
  <Rhythms><Rhythm id="1000"><NoteValue>Quarter</NoteValue></Rhythm></Rhythms>
  <Beats>
    <Beat id="100">
      <GraceNotes>BeforeBeat</GraceNotes>
      <Rhythm ref="1000" />
      <Notes>200</Notes>
      <Properties>
        <Property name="PickStroke"><Direction>Down</Direction></Property>
        <Property name="VibratoWTremBar"><Strength>Slight</Strength></Property>
        <Property name="Slapped"><Enable /></Property>
        <Property name="Popped"><Enable /></Property>
        <Property name="Brush"><Direction>Up</Direction></Property>
      </Properties>
    </Beat>
  </Beats>
  <Notes>
    <Note id="200">
      <LeftFingering>I</LeftFingering>
      <RightFingering>M</RightFingering>
      <Ornament>Turn</Ornament>
      <Properties>
        <Property name="PalmMuted"><Enable /></Property>
        <Property name="Muted"><Enable /></Property>
        <Property name="Tapped"><Enable /></Property>
        <Property name="LeftHandTapped"><Enable /></Property>
        <Property name="HopoOrigin"><Enable /></Property>
        <Property name="HopoDestination"><Enable /></Property>
        <Property name="Slide"><Flags>32</Flags></Property>
        <Property name="Harmonic"><Enable /></Property>
        <Property name="HarmonicType"><HType>Artificial</HType></Property>
        <Property name="HarmonicFret"><HFret>12</HFret></Property>
        <Property name="Bended"><Enable /></Property>
        <Property name="BendOriginOffset"><Float>0</Float></Property>
        <Property name="BendOriginValue"><Float>0</Float></Property>
        <Property name="BendMiddleOffset1"><Float>12</Float></Property>
        <Property name="BendMiddleOffset2"><Float>12</Float></Property>
        <Property name="BendMiddleValue"><Float>25</Float></Property>
        <Property name="BendDestinationOffset"><Float>25</Float></Property>
        <Property name="BendDestinationValue"><Float>50</Float></Property>
        <Property name="Pitch"><Pitch><Step>E</Step><Octave>4</Octave></Pitch></Property>
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

        var beat = score.Tracks[0].Measures[0].Beats[0];
        var articulation = beat.Notes[0].Articulation;

        beat.GraceType.Should().Be("BeforeBeat");
        beat.PickStrokeDirection.Should().Be("Down");
        beat.VibratoWithTremBarStrength.Should().Be("Slight");
        beat.Slapped.Should().BeTrue();
        beat.Popped.Should().BeTrue();
        beat.PalmMuted.Should().BeTrue();
        beat.Brush.Should().BeTrue();
        beat.BrushIsUp.Should().BeTrue();

        articulation.LeftFingering.Should().Be("I");
        articulation.RightFingering.Should().Be("M");
        articulation.Ornament.Should().Be("Turn");
        articulation.PalmMuted.Should().BeTrue();
        articulation.Muted.Should().BeTrue();
        articulation.Tapped.Should().BeTrue();
        articulation.LeftHandTapped.Should().BeTrue();
        articulation.HopoOrigin.Should().BeTrue();
        articulation.HopoDestination.Should().BeTrue();
        articulation.SlideFlags.Should().Be(32);
        articulation.Slides.Should().Contain(SlideType.IntoFromAbove);

        articulation.Harmonic.Should().NotBeNull();
        articulation.Harmonic!.Type.Should().Be(2);
        articulation.Harmonic.TypeName.Should().Be("Artificial");
        articulation.Harmonic.Kind.Should().Be(HarmonicTypeKind.Artificial);
        articulation.Harmonic.Fret.Should().Be(12m);

        articulation.Bend.Should().NotBeNull();
        articulation.Bend!.Type.Should().Be(BendTypeKind.Bend);
        articulation.Bend.OriginValue.Should().Be(0m);
        articulation.Bend.MiddleValue.Should().Be(0.5m);
        articulation.Bend.DestinationValue.Should().Be(1m);
        articulation.Bend.DestinationOffset.Should().Be(0.25m);
    }
}
