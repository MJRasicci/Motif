namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using GPIO.NET.Models.Raw;
using System.Text;
using System.Xml.Linq;

public class XmlGpifSerializerTests
{
    [Fact]
    public async Task Serializer_emits_section_text_as_cdata()
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
                            SectionText = "Intro"
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var writeResult = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        var serializer = new XmlGpifSerializer();
        await using var buffer = new MemoryStream();
        await serializer.SerializeAsync(writeResult.RawDocument, buffer, TestContext.Current.CancellationToken);

        var xml = Encoding.UTF8.GetString(buffer.ToArray());
        xml.Should().Contain("<Section>");
        xml.Should().Contain("<Letter><![CDATA[]]></Letter>");
        xml.Should().Contain("<Text><![CDATA[Intro]]></Text>");
    }

    [Fact]
    public async Task Serializer_emits_explicit_empty_optional_score_nodes()
    {
        var score = new GuitarProScore
        {
            Metadata = new ScoreMetadata
            {
                ExplicitEmptyOptionalElements = ["WordsAndMusic", "PageHeader"]
            }
        };

        var unmapper = new DefaultScoreUnmapper();
        var writeResult = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        var serializer = new XmlGpifSerializer();
        await using var buffer = new MemoryStream();
        await serializer.SerializeAsync(writeResult.RawDocument, buffer, TestContext.Current.CancellationToken);

        var xml = Encoding.UTF8.GetString(buffer.ToArray());
        xml.Should().Contain("<WordsAndMusic");
        xml.Should().Contain("<PageHeader");
    }

    [Fact]
    public async Task Serializer_emits_score_metadata_display_fields_as_cdata()
    {
        var score = new GuitarProScore
        {
            Title = "Song & Title",
            Artist = string.Empty,
            Album = "Record",
            Metadata = new ScoreMetadata
            {
                SubTitle = "Subtitle",
                Words = "Lyrics",
                WordsAndMusic = string.Empty,
                Copyright = "(c) test",
                PageFooter = "<html>Page %page%/%pages%</html>",
                ScoreSystemsDefaultLayout = "4",
                ScoreZoom = "1.5",
                ExplicitEmptyOptionalElements = ["WordsAndMusic"]
            }
        };

        var unmapper = new DefaultScoreUnmapper();
        var writeResult = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        var serializer = new XmlGpifSerializer();
        await using var buffer = new MemoryStream();
        await serializer.SerializeAsync(writeResult.RawDocument, buffer, TestContext.Current.CancellationToken);

        var document = XDocument.Parse(Encoding.UTF8.GetString(buffer.ToArray()));
        var scoreElement = document.Root!.Element("Score")!;

        scoreElement.Element("Title")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Song & Title");
        scoreElement.Element("Artist")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle(string.Empty);
        scoreElement.Element("Album")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Record");
        scoreElement.Element("SubTitle")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Subtitle");
        scoreElement.Element("Words")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("Lyrics");
        scoreElement.Element("WordsAndMusic")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle(string.Empty);
        scoreElement.Element("Copyright")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("(c) test");
        scoreElement.Element("PageFooter")!.Nodes().OfType<XCData>().Select(node => node.Value).Should().ContainSingle("<html>Page %page%/%pages%</html>");
        scoreElement.Element("ScoreSystemsDefaultLayout")!.Nodes().OfType<XCData>().Should().BeEmpty();
        scoreElement.Element("ScoreZoom")!.Nodes().OfType<XCData>().Should().BeEmpty();
    }

    [Fact]
    public async Task Serializer_emits_lf_delimited_xml_without_leading_indentation()
    {
        var score = new GuitarProScore
        {
            Title = "Formatting Test",
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
                            Voices =
                            [
                                new MeasureVoiceModel
                                {
                                    VoiceIndex = 0,
                                    Beats =
                                    [
                                        new BeatModel
                                        {
                                            Id = 1,
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

        var unmapper = new DefaultScoreUnmapper();
        var writeResult = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        var serializer = new XmlGpifSerializer();
        await using var buffer = new MemoryStream();
        await serializer.SerializeAsync(writeResult.RawDocument, buffer, TestContext.Current.CancellationToken);

        var xml = Encoding.UTF8.GetString(buffer.ToArray());
        xml.Should().NotContain("\r");
        xml.Should().Contain("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<GPIF>\n");

        var lines = xml.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().OnlyContain(line => line.Length == 0 || line[0] == '<');
    }

    [Fact]
    public async Task Serializer_does_not_reuse_stale_raw_node_xml_when_values_change()
    {
        var document = new GpifDocument
        {
            Score = new ScoreInfo
            {
                Title = "T",
                Artist = "A",
                Album = "B"
            },
            MasterTrack = new GpifMasterTrack
            {
                TrackIds = [0]
            },
            Tracks =
            [
                new GpifTrack
                {
                    Id = 0,
                    Name = "Track"
                }
            ],
            MasterBars =
            [
                new GpifMasterBar
                {
                    Index = 0,
                    Time = "4/4",
                    BarsReferenceList = "1"
                }
            ],
            BarsById = new Dictionary<int, GpifBar>
            {
                [1] = new()
                {
                    Id = 1,
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
                    Xml = "<Beat id=\"100\"><Rhythm ref=\"1000\" /><FreeText><![CDATA[Dist.]]></FreeText></Beat>",
                    Id = 100,
                    RhythmRef = 1000,
                    FreeText = "Clean"
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

        await using var buffer = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(document, buffer, TestContext.Current.CancellationToken);

        var xml = Encoding.UTF8.GetString(buffer.ToArray());
        xml.Should().Contain("<FreeText>Clean</FreeText>");
        xml.Should().NotContain("Dist.");
    }

    [Fact]
    public async Task Serializer_does_not_reuse_stale_container_raw_xml_when_values_change()
    {
        var document = new GpifDocument
        {
            Score = new ScoreInfo
            {
                Xml = "<Score><Title><![CDATA[Old]]></Title><Artist><![CDATA[A]]></Artist><Album><![CDATA[B]]></Album></Score>",
                Title = "New",
                Artist = "A",
                Album = "B"
            },
            MasterTrack = new GpifMasterTrack
            {
                Xml = "<MasterTrack><Tracks>99</Tracks></MasterTrack>",
                TrackIds = [0]
            },
            Tracks =
            [
                new GpifTrack
                {
                    Xml = "<Track id=\"0\"><Name>Old Track</Name></Track>",
                    Id = 0,
                    Name = "New Track"
                }
            ],
            MasterBars =
            [
                new GpifMasterBar
                {
                    Index = 0,
                    Time = "4/4",
                    BarsReferenceList = "1"
                }
            ],
            BarsById = new Dictionary<int, GpifBar>
            {
                [1] = new()
                {
                    Id = 1,
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
                    RhythmRef = 1000
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

        await using var buffer = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(document, buffer, TestContext.Current.CancellationToken);

        var xml = Encoding.UTF8.GetString(buffer.ToArray());
        xml.Should().Contain("<Score><Title><![CDATA[New]]></Title><Artist><![CDATA[A]]></Artist><Album><![CDATA[B]]></Album></Score>");
        xml.Should().Contain("<MasterTrack><Tracks>0</Tracks></MasterTrack>");
        xml.Should().Contain("<Track id=\"0\"><Name>New Track</Name></Track>");
        xml.Should().NotContain("Old Track");
        xml.Should().NotContain("<Tracks>99</Tracks>");
    }
}
