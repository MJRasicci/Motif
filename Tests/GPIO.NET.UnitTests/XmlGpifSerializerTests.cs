namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models;
using System.Text;

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
}
