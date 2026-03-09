namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;
using System.Text;
using System.Xml.Linq;

public class BeatEffectMappingTests
{
    private static string BuildGpif(string beatBody, string noteBody = "", string noteXProperties = "")
    {
        var notesRef = string.IsNullOrEmpty(noteBody) ? "" : "<Notes>200</Notes>";
        var noteXml = string.IsNullOrEmpty(noteBody)
            ? ""
            : $"""
            <Notes>
              <Note id="200">
                {noteBody}
                {(string.IsNullOrEmpty(noteXProperties) ? "" : $"<XProperties>{noteXProperties}</XProperties>")}
              </Note>
            </Notes>
            """;

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
              {notesRef}
              {beatBody}
            </Beat>
          </Beats>
          {noteXml}
        </GPIF>
        """;
    }

    private static async Task<GuitarProScore> DeserializeAndMap(string gpif)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpif));
        var raw = await new XmlGpifDeserializer().DeserializeAsync(stream, TestContext.Current.CancellationToken);
        return await new DefaultScoreMapper().MapAsync(raw, TestContext.Current.CancellationToken);
    }

    private static async Task<XDocument> RoundTripThroughWrite(string gpif)
    {
        var score = await DeserializeAndMap(gpif);
        var result = await new DefaultScoreUnmapper().UnmapAsync(score, TestContext.Current.CancellationToken);
        await using var stream = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(result.RawDocument, stream, TestContext.Current.CancellationToken);
        return XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
    }

    // ── Whammy bar ──────────────────────────────────────────────────────

    [Fact]
    public async Task WhammyBar_deserialization_normalizes_values()
    {
        var gpif = BuildGpif("""
            <Properties>
              <Property name="WhammyBar"><Enable /></Property>
              <Property name="WhammyBarOriginValue"><Float>0</Float></Property>
              <Property name="WhammyBarMiddleValue"><Float>50</Float></Property>
              <Property name="WhammyBarDestinationValue"><Float>100</Float></Property>
              <Property name="WhammyBarOriginOffset"><Float>0</Float></Property>
              <Property name="WhammyBarMiddleOffset1"><Float>50</Float></Property>
              <Property name="WhammyBarMiddleOffset2"><Float>50</Float></Property>
              <Property name="WhammyBarDestinationOffset"><Float>100</Float></Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.WhammyBar.Should().NotBeNull();
        beat.WhammyBar!.Enabled.Should().BeTrue();
        beat.WhammyBar.OriginValue.Should().Be(0m);
        beat.WhammyBar.MiddleValue.Should().Be(1m);        // 50/50
        beat.WhammyBar.DestinationValue.Should().Be(2m);    // 100/50
        beat.WhammyBar.OriginOffset.Should().Be(0m);
        beat.WhammyBar.MiddleOffset1.Should().Be(0.5m);     // 50/100
        beat.WhammyBar.MiddleOffset2.Should().Be(0.5m);
        beat.WhammyBar.DestinationOffset.Should().Be(1m);   // 100/100
    }

    [Fact]
    public async Task WhammyBar_extended_flag_is_captured()
    {
        var gpif = BuildGpif("""
            <Properties>
              <Property name="WhammyBar"><Enable /></Property>
              <Property name="WhammyBarExtend"><Enable /></Property>
              <Property name="WhammyBarOriginValue"><Float>25</Float></Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.WhammyBar.Should().NotBeNull();
        beat.WhammyBar!.Extended.Should().BeTrue();
        beat.WhammyBar.OriginValue.Should().Be(0.5m); // 25/50
    }

    [Fact]
    public async Task Unmodeled_beat_properties_round_trip_through_write()
    {
        var gpif = BuildGpif("""
            <Properties>
              <Property name="PrimaryPickupVolume"><Float>0.500000</Float></Property>
              <Property name="PrimaryPickupTone"><Float>0.500000</Float></Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];
        beat.Properties.Should().ContainKey("PrimaryPickupVolume");
        beat.Properties["PrimaryPickupVolume"].Should().Be("0.500000");
        beat.Properties.Should().ContainKey("PrimaryPickupTone");
        beat.Properties["PrimaryPickupTone"].Should().Be("0.500000");

        var roundTrip = await RoundTripThroughWrite(gpif);
        var outputBeat = roundTrip.Root!.Element("Beats")!.Element("Beat")!;
        var outputProperties = outputBeat.Element("Properties")!.Elements("Property")
            .ToDictionary(p => (string)p.Attribute("name")!, p => p);

        outputProperties["PrimaryPickupVolume"].Element("Float")!.Value.Should().Be("0.500000");
        outputProperties["PrimaryPickupTone"].Element("Float")!.Value.Should().Be("0.500000");
    }

    [Fact]
    public async Task WhammyBar_absent_returns_null()
    {
        var gpif = BuildGpif("");
        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].WhammyBar.Should().BeNull();
    }

    [Fact]
    public async Task Midi_number_property_is_preferred_over_pitch_element_for_note_midi()
    {
        var gpif = BuildGpif(
            beatBody: "",
            noteBody: """
                <Properties>
                  <Property name="ConcertPitch">
                    <Pitch><Step>C</Step><Accidental></Accidental><Octave>-1</Octave></Pitch>
                  </Property>
                  <Property name="Midi"><Number>36</Number></Property>
                  <Property name="String"><String>0</String></Property>
                </Properties>
                """);

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].Notes[0].MidiPitch.Should().Be(36);
    }

    [Fact]
    public async Task AntiAccent_text_round_trips_through_write()
    {
        var gpif = BuildGpif(
            beatBody: "",
            noteBody: """
                <AntiAccent>Normal</AntiAccent>
                <Properties>
                  <Property name="ConcertPitch">
                    <Pitch><Step>C</Step><Accidental></Accidental><Octave>4</Octave></Pitch>
                  </Property>
                  <Property name="Midi"><Number>48</Number></Property>
                  <Property name="String"><String>0</String></Property>
                </Properties>
                """);

        var score = await DeserializeAndMap(gpif);
        var note = score.Tracks[0].Measures[0].Beats[0].Notes[0];
        note.Articulation.AntiAccent.Should().BeTrue();
        note.Articulation.AntiAccentValue.Should().Be("Normal");

        var roundTrip = await RoundTripThroughWrite(gpif);
        roundTrip.Root!
            .Element("Notes")!
            .Element("Note")!
            .Element("AntiAccent")!
            .Value.Should().Be("Normal");
    }

    [Fact]
    public async Task WhammyBar_encode_round_trips_through_write_and_read()
    {
        var score = new GuitarProScore
        {
            Tracks =
            [
                new TrackModel
                {
                    Id = 0, Name = "Guitar",
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 0, TimeSignature = "4/4",
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 1, Duration = 0.25m,
                                    WhammyBar = new WhammyBarModel
                                    {
                                        Enabled = true, Extended = true,
                                        OriginValue = 0m, MiddleValue = 1m, DestinationValue = 2m,
                                        OriginOffset = 0m, MiddleOffset1 = 0.5m, MiddleOffset2 = 0.5m, DestinationOffset = 1m
                                    },
                                    Notes = [new NoteModel { Id = 1, MidiPitch = 60 }]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-whammy-enc-{Guid.NewGuid():N}.gp");
        try
        {
            await new Motif.Extensions.GuitarPro.GuitarProWriter().WriteAsync(score, outFile, TestContext.Current.CancellationToken);
            var readBack = await new Motif.Extensions.GuitarPro.GuitarProReader().ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            var whammy = readBack.Tracks[0].Measures[0].Beats[0].WhammyBar;
            whammy.Should().NotBeNull();
            whammy!.Enabled.Should().BeTrue();
            whammy.Extended.Should().BeTrue();
            whammy.OriginValue.Should().Be(0m);
            whammy.MiddleValue.Should().Be(1m);
            whammy.DestinationValue.Should().Be(2m);
            whammy.OriginOffset.Should().Be(0m);
            whammy.MiddleOffset1.Should().Be(0.5m);
            whammy.MiddleOffset2.Should().Be(0.5m);
            whammy.DestinationOffset.Should().Be(1m);
        }
        finally
        {
            if (File.Exists(outFile)) File.Delete(outFile);
        }
    }

    // ── Rasgueado ───────────────────────────────────────────────────────

    [Fact]
    public async Task Rasgueado_property_is_captured()
    {
        var gpif = BuildGpif("""
            <Properties>
              <Property name="Rasgueado"><Enable /></Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].Rasgueado.Should().BeTrue();
    }

    [Fact]
    public async Task Rasgueado_absent_is_false()
    {
        var gpif = BuildGpif("");
        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].Rasgueado.Should().BeFalse();
    }

    [Fact]
    public async Task Beat_shape_effects_round_trip_through_write()
    {
        var gpif = BuildGpif("""
            <TransposedPitchStemOrientationUserDefined />
            <Wah>Open</Wah>
            <Golpe>Finger</Golpe>
            <Fadding>FadeIn</Fadding>
            <Slashed />
            <Properties>
              <Property name="BarreFret"><Fret>7</Fret></Property>
              <Property name="BarreString"><String>0</String></Property>
              <Property name="Rasgueado"><Rasgueado>mii_1</Rasgueado></Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.HasTransposedPitchStemOrientationUserDefinedElement.Should().BeTrue();
        beat.Wah.Should().Be("Open");
        beat.Golpe.Should().Be("Finger");
        beat.Fadding.Should().Be("FadeIn");
        beat.Slashed.Should().BeTrue();
        beat.Rasgueado.Should().BeTrue();
        beat.RasgueadoPattern.Should().Be("mii_1");
        beat.Properties["BarreFret"].Should().Be("7");
        beat.Properties["BarreString"].Should().Be("0");

        var roundTrip = await RoundTripThroughWrite(gpif);
        var outputBeat = roundTrip.Root!.Element("Beats")!.Element("Beat")!;
        var outputProperties = outputBeat.Element("Properties")!.Elements("Property")
            .ToDictionary(p => (string)p.Attribute("name")!, p => p);

        outputBeat.Element("TransposedPitchStemOrientationUserDefined").Should().NotBeNull();
        outputBeat.Element("Wah")!.Value.Should().Be("Open");
        outputBeat.Element("Golpe")!.Value.Should().Be("Finger");
        outputBeat.Element("Fadding")!.Value.Should().Be("FadeIn");
        outputBeat.Element("Slashed").Should().NotBeNull();
        outputProperties["BarreFret"].Element("Fret")!.Value.Should().Be("7");
        outputProperties["BarreString"].Element("String")!.Value.Should().Be("0");
        outputProperties["Rasgueado"].Element("Rasgueado")!.Value.Should().Be("mii_1");
    }

    // ── DeadSlapped ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeadSlapped_element_is_captured()
    {
        var gpif = BuildGpif("<DeadSlapped />");

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].DeadSlapped.Should().BeTrue();
    }

    [Fact]
    public async Task DeadSlapped_absent_is_false()
    {
        var gpif = BuildGpif("");
        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].DeadSlapped.Should().BeFalse();
    }

    // ── Arpeggio vs Brush ───────────────────────────────────────────────

    [Fact]
    public async Task Arpeggio_element_sets_arpeggio_and_brush()
    {
        var gpif = BuildGpif("<Arpeggio>Down</Arpeggio>");

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.Arpeggio.Should().BeTrue();
        beat.Brush.Should().BeTrue();
        beat.BrushIsUp.Should().BeFalse(); // Down → not up
    }

    [Fact]
    public async Task Arpeggio_up_sets_brush_is_up()
    {
        var gpif = BuildGpif("<Arpeggio>Up</Arpeggio>");

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.Arpeggio.Should().BeTrue();
        beat.Brush.Should().BeTrue();
        beat.BrushIsUp.Should().BeTrue();
    }

    [Fact]
    public async Task Brush_property_without_arpeggio_sets_brush_only()
    {
        var gpif = BuildGpif("""
            <Properties>
              <Property name="Brush"><Direction>Down</Direction></Property>
            </Properties>
            """);

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.Brush.Should().BeTrue();
        beat.Arpeggio.Should().BeFalse();
        beat.BrushIsUp.Should().BeFalse();
        beat.BrushDurationTicks.Should().Be(60);
    }

    [Fact]
    public async Task Brush_duration_from_XProperties()
    {
        var gpif = BuildGpif("""
            <Arpeggio>Down</Arpeggio>
            <XProperties>
              <XProperty id="687935489"><Int>120</Int></XProperty>
            </XProperties>
            """);

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].BrushDurationTicks.Should().Be(120);
    }

    [Fact]
    public async Task Brush_duration_fallback_XProperty()
    {
        var gpif = BuildGpif("""
            <Arpeggio>Down</Arpeggio>
            <XProperties>
              <XProperty id="687931393"><Int>60</Int></XProperty>
            </XProperties>
            """);

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].BrushDurationTicks.Should().Be(60);
    }

    // ── Trill speed ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(480, TrillSpeedKind.Sixteenth)]
    [InlineData(240, TrillSpeedKind.Sixteenth)]
    [InlineData(120, TrillSpeedKind.ThirtySecond)]
    [InlineData(60, TrillSpeedKind.SixtyFourth)]
    [InlineData(30, TrillSpeedKind.OneHundredTwentyEighth)]
    public async Task TrillSpeed_decode_thresholds(int rawValue, TrillSpeedKind expected)
    {
        var gpif = BuildGpif(
            "",
            noteBody: """
                <Properties>
                  <Property name="Fret"><Fret>5</Fret></Property>
                  <Property name="String"><String>3</String></Property>
                </Properties>
                """,
            noteXProperties: $"""<XProperty id="688062467"><Int>{rawValue}</Int></XProperty>""");

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].Notes[0].Articulation.TrillSpeed.Should().Be(expected);
    }

    [Fact]
    public async Task TrillSpeed_absent_XProperty_returns_none()
    {
        var gpif = BuildGpif(
            "",
            noteBody: """
                <Properties>
                  <Property name="Fret"><Fret>5</Fret></Property>
                  <Property name="String"><String>3</String></Property>
                </Properties>
                """);

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].Notes[0].Articulation.TrillSpeed.Should().Be(TrillSpeedKind.None);
    }

    [Theory]
    [InlineData(TrillSpeedKind.Sixteenth)]
    [InlineData(TrillSpeedKind.ThirtySecond)]
    [InlineData(TrillSpeedKind.SixtyFourth)]
    [InlineData(TrillSpeedKind.OneHundredTwentyEighth)]
    public async Task TrillSpeed_round_trips_through_write_and_read(TrillSpeedKind kind)
    {
        var score = new GuitarProScore
        {
            Tracks =
            [
                new TrackModel
                {
                    Id = 0, Name = "Guitar",
                    Measures =
                    [
                        new MeasureModel
                        {
                            Index = 0, TimeSignature = "4/4",
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 1, Duration = 0.25m,
                                    Notes =
                                    [
                                        new NoteModel
                                        {
                                            Id = 1, MidiPitch = 64,
                                            Articulation = new NoteArticulationModel
                                            {
                                                Trill = 7,
                                                TrillSpeed = kind
                                            }
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-trill-{Guid.NewGuid():N}.gp");
        try
        {
            await new Motif.Extensions.GuitarPro.GuitarProWriter().WriteAsync(score, outFile, TestContext.Current.CancellationToken);
            var readBack = await new Motif.Extensions.GuitarPro.GuitarProReader().ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);
            readBack.Tracks[0].Measures[0].Beats[0].Notes[0].Articulation.TrillSpeed.Should().Be(kind);
        }
        finally
        {
            if (File.Exists(outFile)) File.Delete(outFile);
        }
    }

    [Fact]
    public async Task TrillSpeed_deserialized_from_note_XProperty()
    {
        var gpif = BuildGpif(
            "",
            noteBody: """
                <Properties>
                  <Property name="Fret"><Fret>5</Fret></Property>
                  <Property name="String"><String>3</String></Property>
                </Properties>
                """,
            noteXProperties: """<XProperty id="688062467"><Int>480</Int></XProperty>""");

        var score = await DeserializeAndMap(gpif);
        var note = score.Tracks[0].Measures[0].Beats[0].Notes[0];
        note.Articulation.TrillSpeed.Should().Be(TrillSpeedKind.Sixteenth);
    }

    // ── Tremolo ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Tremolo_element_with_value()
    {
        var gpif = BuildGpif("<Tremolo>1/8</Tremolo>");

        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];

        beat.Tremolo.Should().BeTrue();
        beat.TremoloValue.Should().Be("1/8");
    }

    [Fact]
    public async Task Tremolo_absent_is_false()
    {
        var gpif = BuildGpif("");
        var score = await DeserializeAndMap(gpif);
        var beat = score.Tracks[0].Measures[0].Beats[0];
        beat.Tremolo.Should().BeFalse();
        beat.TremoloValue.Should().BeEmpty();
    }

    // ── Chord ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Chord_element_captures_id()
    {
        var gpif = BuildGpif("<Chord>Am7</Chord>");

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].ChordId.Should().Be("Am7");
    }

    // ── FreeText ────────────────────────────────────────────────────────

    [Fact]
    public async Task FreeText_element_captures_text()
    {
        var gpif = BuildGpif("<FreeText>let ring</FreeText>");

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].FreeText.Should().Be("let ring");
    }

    [Fact]
    public async Task Dynamic_element_captures_value()
    {
        var gpif = BuildGpif("<Dynamic>FF</Dynamic>");

        var score = await DeserializeAndMap(gpif);
        score.Tracks[0].Measures[0].Beats[0].Dynamic.Should().Be("FF");
    }

    [Fact]
    public async Task Dynamic_round_trips_through_write_and_read()
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
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 1,
                                    Duration = 0.25m,
                                    Dynamic = "PP",
                                    Notes =
                                    [
                                        new NoteModel { Id = 1, MidiPitch = 60 }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-dynamic-{Guid.NewGuid():N}.gp");
        try
        {
            await new Motif.Extensions.GuitarPro.GuitarProWriter().WriteAsync(score, outFile, TestContext.Current.CancellationToken);
            var readBack = await new Motif.Extensions.GuitarPro.GuitarProReader().ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            readBack.Tracks[0].Measures[0].Beats[0].Dynamic.Should().Be("PP");
        }
        finally
        {
            if (File.Exists(outFile)) File.Delete(outFile);
        }
    }

    // ── Round-trip parity ───────────────────────────────────────────────

    [Fact]
    public async Task RoundTrip_preserves_all_new_beat_effects()
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
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 1,
                                    Arpeggio = true,
                                    Brush = true,
                                    BrushIsUp = true,
                                    BrushDurationTicks = 120,
                                    Rasgueado = true,
                                    DeadSlapped = true,
                                    Tremolo = true,
                                    TremoloValue = "1/8",
                                    ChordId = "Dm",
                                    FreeText = "muted",
                                    WhammyBar = new WhammyBarModel
                                    {
                                        Enabled = true,
                                        Extended = true,
                                        OriginValue = 0m,
                                        MiddleValue = 1m,
                                        DestinationValue = 2m,
                                        OriginOffset = 0m,
                                        MiddleOffset1 = 0.5m,
                                        MiddleOffset2 = 0.5m,
                                        DestinationOffset = 1m
                                    },
                                    Duration = 0.25m,
                                    Notes =
                                    [
                                        new NoteModel
                                        {
                                            Id = 1,
                                            MidiPitch = 64,
                                            Articulation = new NoteArticulationModel
                                            {
                                                Trill = 7,
                                                TrillSpeed = TrillSpeedKind.ThirtySecond
                                            }
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-beatfx-{Guid.NewGuid():N}.gp");
        try
        {
            var writer = new Motif.Extensions.GuitarPro.GuitarProWriter();
            await writer.WriteAsync(score, outFile, TestContext.Current.CancellationToken);

            var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
            var readBack = await reader.ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            var beat = readBack.Tracks[0].Measures[0].Beats[0];
            beat.Arpeggio.Should().BeTrue();
            beat.Brush.Should().BeTrue();
            beat.BrushIsUp.Should().BeTrue();
            beat.BrushDurationTicks.Should().Be(120);
            beat.Rasgueado.Should().BeTrue();
            beat.DeadSlapped.Should().BeTrue();
            beat.Tremolo.Should().BeTrue();
            beat.TremoloValue.Should().Be("1/8");
            beat.ChordId.Should().Be("Dm");
            beat.FreeText.Should().Be("muted");

            beat.WhammyBar.Should().NotBeNull();
            beat.WhammyBar!.Enabled.Should().BeTrue();
            beat.WhammyBar.Extended.Should().BeTrue();
            beat.WhammyBar.OriginValue.Should().Be(0m);
            beat.WhammyBar.MiddleValue.Should().Be(1m);
            beat.WhammyBar.DestinationValue.Should().Be(2m);
            beat.WhammyBar.OriginOffset.Should().Be(0m);
            beat.WhammyBar.MiddleOffset1.Should().Be(0.5m);
            beat.WhammyBar.MiddleOffset2.Should().Be(0.5m);
            beat.WhammyBar.DestinationOffset.Should().Be(1m);

            var note = beat.Notes[0];
            note.Articulation.Trill.Should().Be(7);
            note.Articulation.TrillSpeed.Should().Be(TrillSpeedKind.ThirtySecond);
        }
        finally
        {
            if (File.Exists(outFile))
                File.Delete(outFile);
        }
    }

    [Fact]
    public async Task RoundTrip_whammy_bar_only()
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
                            Beats =
                            [
                                new BeatModel
                                {
                                    Id = 1,
                                    Duration = 0.25m,
                                    WhammyBar = new WhammyBarModel
                                    {
                                        Enabled = true,
                                        OriginValue = 0m,
                                        DestinationValue = -1m
                                    },
                                    Notes =
                                    [
                                        new NoteModel { Id = 1, MidiPitch = 60 }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var outFile = Path.Combine(Path.GetTempPath(), $"gpio-whammy-{Guid.NewGuid():N}.gp");
        try
        {
            await new Motif.Extensions.GuitarPro.GuitarProWriter().WriteAsync(score, outFile, TestContext.Current.CancellationToken);
            var readBack = await new Motif.Extensions.GuitarPro.GuitarProReader().ReadAsync(outFile, cancellationToken: TestContext.Current.CancellationToken);

            var beat = readBack.Tracks[0].Measures[0].Beats[0];
            beat.WhammyBar.Should().NotBeNull();
            beat.WhammyBar!.Enabled.Should().BeTrue();
            beat.WhammyBar.OriginValue.Should().Be(0m);
            beat.WhammyBar.DestinationValue.Should().Be(-1m);
        }
        finally
        {
            if (File.Exists(outFile))
                File.Delete(outFile);
        }
    }
}
