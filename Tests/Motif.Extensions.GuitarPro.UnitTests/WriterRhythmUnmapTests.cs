namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Models;
using System.Text;
using System.Xml.Linq;

public class WriterRhythmUnmapTests
{
    [Fact]
    public async Task Unmapper_preserves_dotted_and_tuplet_rhythm_shapes_when_recognized()
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
                                new BeatModel { Id = 1, Duration = 0.375m },      // dotted quarter
                                new BeatModel { Id = 2, Duration = 1m/12m }       // eighth triplet
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.RhythmsById.Should().HaveCount(2);

        var rhythms = result.RawDocument.RhythmsById.Values.OrderBy(r => r.Id).ToArray();
        rhythms[0].NoteValue.Should().Be("Quarter");
        rhythms[0].AugmentationDots.Should().Be(1);

        rhythms[1].NoteValue.Should().Be("Eighth");
        rhythms[1].PrimaryTuplet.Should().NotBeNull();
        rhythms[1].PrimaryTuplet!.Numerator.Should().Be(3);
        rhythms[1].PrimaryTuplet!.Denominator.Should().Be(2);
    }

    [Fact]
    public async Task Unmapper_preserves_distinct_source_rhythm_ids_even_when_shapes_match()
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
                                new BeatModel { Id = 1, SourceRhythmId = 3, Duration = 0.25m },
                                new BeatModel { Id = 2, SourceRhythmId = 7, Duration = 0.25m }
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.RhythmsById.Keys.Should().BeEquivalentTo([3, 7]);
        result.RawDocument.BeatsById[1].RhythmRef.Should().Be(3);
        result.RawDocument.BeatsById[2].RhythmRef.Should().Be(7);
    }

    [Fact]
    public async Task Unmapper_preserves_source_tuplet_shape_when_duration_is_unchanged()
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
                                    SourceRhythmId = 10,
                                    SourceRhythm = new RhythmShapeModel
                                    {
                                        NoteValue = "32nd",
                                        PrimaryTuplet = new TupletRatioModel
                                        {
                                            Numerator = 3,
                                            Denominator = 2
                                        }
                                    },
                                    Duration = 1m / 48m
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.Diagnostics.Warnings.Should().BeEmpty();
        result.RawDocument.RhythmsById.Should().ContainKey(10);
        result.RawDocument.RhythmsById[10].NoteValue.Should().Be("32nd");
        result.RawDocument.RhythmsById[10].PrimaryTuplet.Should().NotBeNull();
        result.RawDocument.RhythmsById[10].PrimaryTuplet!.Numerator.Should().Be(3);
        result.RawDocument.RhythmsById[10].PrimaryTuplet!.Denominator.Should().Be(2);
    }

    [Fact]
    public async Task Unmapper_preserves_augmentation_dot_count_attribute_when_requested_by_source_shape()
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
                                    SourceRhythmId = 4,
                                    SourceRhythm = new RhythmShapeModel
                                    {
                                        NoteValue = "Quarter",
                                        AugmentationDots = 1,
                                        AugmentationDotUsesCountAttribute = true
                                    },
                                    Duration = 0.375m
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.RawDocument.RhythmsById[4].AugmentationDotUsesCountAttribute.Should().BeTrue();

        await using var stream = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(result.RawDocument, stream, TestContext.Current.CancellationToken);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
        xml.Root!
            .Element("Rhythms")!
            .Element("Rhythm")!
            .Element("AugmentationDot")!
            .Attribute("count")!
            .Value.Should().Be("1");
    }

    [Fact]
    public async Task Unmapper_preserves_grouped_augmentation_dot_count_shape_when_requested_by_source()
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
                                    SourceRhythmId = 9,
                                    SourceRhythm = new RhythmShapeModel
                                    {
                                        NoteValue = "Quarter",
                                        AugmentationDots = 2,
                                        AugmentationDotUsesCountAttribute = true,
                                        AugmentationDotCounts = [2]
                                    },
                                    Duration = 0.4375m
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var unmapper = new DefaultScoreUnmapper();
        var result = await unmapper.UnmapAsync(score, TestContext.Current.CancellationToken);

        result.RawDocument.RhythmsById[9].AugmentationDots.Should().Be(2);
        result.RawDocument.RhythmsById[9].AugmentationDotCounts.Should().Equal(2);

        await using var stream = new MemoryStream();
        await new XmlGpifSerializer().SerializeAsync(result.RawDocument, stream, TestContext.Current.CancellationToken);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
        xml.Root!
            .Element("Rhythms")!
            .Element("Rhythm")!
            .Element("AugmentationDot")!
            .Attribute("count")!
            .Value.Should().Be("2");
    }
}
