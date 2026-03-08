namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
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
}
