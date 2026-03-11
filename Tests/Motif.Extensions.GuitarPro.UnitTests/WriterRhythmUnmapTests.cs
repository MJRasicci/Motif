namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using System.Text;
using System.Xml.Linq;

public class WriterRhythmUnmapTests
{
    [Fact]
    public async Task Unmapper_preserves_dotted_and_tuplet_rhythm_shapes_when_recognized()
    {
        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat { Id = 1, Duration = 0.375m },
                            new Beat { Id = 2, Duration = 1m / 12m }
                        ]
                    })
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
        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat { Id = 1, Duration = 0.25m },
                            new Beat { Id = 2, Duration = 0.25m }
                        ]
                    })
            ]
        };
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetOrCreateGuitarPro().Metadata.SourceRhythmId = 3;
        score.Tracks[0].PrimaryMeasure(0).Beats[1].GetOrCreateGuitarPro().Metadata.SourceRhythmId = 7;

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
        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat
                            {
                                Id = 1,
                                Duration = 1m / 48m
                            }
                        ]
                    })
            ]
        };
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetOrCreateGuitarPro().Metadata.SourceRhythmId = 10;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetRequiredGuitarPro().Metadata.SourceRhythm = new GpRhythmShapeMetadata
        {
            NoteValue = "32nd",
            PrimaryTuplet = new TupletRatio
            {
                Numerator = 3,
                Denominator = 2
            }
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
        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat
                            {
                                Id = 1,
                                Duration = 0.375m
                            }
                        ]
                    })
            ]
        };
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetOrCreateGuitarPro().Metadata.SourceRhythmId = 4;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetRequiredGuitarPro().Metadata.SourceRhythm = new GpRhythmShapeMetadata
        {
            NoteValue = "Quarter",
            AugmentationDots = 1,
            AugmentationDotUsesCountAttribute = true
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
        var score = new Score
        {
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    0,
                    "Guitar",
                    new StaffMeasure
                    {
                        Index = 0,
                        StaffIndex = 0,
                        Beats =
                        [
                            new Beat
                            {
                                Id = 1,
                                Duration = 0.4375m
                            }
                        ]
                    })
            ]
        };
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetOrCreateGuitarPro().Metadata.SourceRhythmId = 9;
        score.Tracks[0].PrimaryMeasure(0).Beats[0].GetRequiredGuitarPro().Metadata.SourceRhythm = new GpRhythmShapeMetadata
        {
            NoteValue = "Quarter",
            AugmentationDots = 2,
            AugmentationDotUsesCountAttribute = true,
            AugmentationDotCounts = [2]
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
