namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Models;

public class ScoreNavigationTests
{
    [Fact]
    public void BuildPlaybackSequence_handles_simple_repeat_with_two_endings()
    {
        var measures = new[]
        {
            Measure(0, repeatStart: true),
            Measure(1, repeatEnd: true, repeatCount: 2),
            Measure(2, alternateEndings: "1"),
            Measure(3, alternateEndings: "2")
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 0, 1, 2, 3);
    }

    [Fact]
    public void BuildPlaybackSequence_stops_infinite_loops_with_guard_limit()
    {
        var measures = new[]
        {
            Measure(0, repeatStart: true, repeatEnd: true, repeatCount: 100000)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Count.Should().BeLessThan(20000);
        sequence.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildPlaybackSequence_handles_da_capo_al_fine()
    {
        var measures = new[]
        {
            Measure(0),
            Measure(1, jump: "DaCapoAlFine"),
            Measure(2, target: "Fine"),
            Measure(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 0, 1, 2);
    }

    [Fact]
    public void BuildPlaybackSequence_handles_da_capo_al_coda_with_conditional_da_coda_jump()
    {
        var measures = new[]
        {
            Measure(0),
            Measure(1, jump: "DaCapoAlCoda"),
            Measure(2, jump: "DaCoda"),
            Measure(3, target: "Coda"),
            Measure(4)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 0, 1, 2, 3, 4);
    }

    [Fact]
    public void BuildPlaybackSequence_handles_da_segno_segno_al_double_coda()
    {
        var measures = new[]
        {
            Measure(0),
            Measure(1, target: "SegnoSegno"),
            Measure(2, jump: "DaSegnoSegnoAlDoubleCoda"),
            Measure(3, jump: "DaDoubleCoda"),
            Measure(4, target: "DoubleCoda"),
            Measure(5)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 2, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void BuildPlaybackSequence_ignores_da_coda_without_pending_al_coda_route()
    {
        var measures = new[]
        {
            Measure(0),
            Measure(1, jump: "DaCoda"),
            Measure(2, target: "Coda")
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void BuildPlaybackSequence_uses_last_matching_direction_marker()
    {
        var measures = new[]
        {
            Measure(0, jump: "DaCapoAlCoda"),
            Measure(1, target: "Coda"),
            Measure(2),
            Measure(3, jump: "DaCoda"),
            Measure(4, target: "Coda"),
            Measure(5)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 0, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void BuildPlaybackSequence_honors_extended_alternate_endings_inside_repeat_loop()
    {
        var measures = new[]
        {
            Measure(0, repeatStart: true),
            Measure(1, alternateEndings: "1"),
            Measure(2, alternateEndings: "2"),
            Measure(3, repeatEnd: true, repeatCount: 2)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 0, 2, 3);
    }

    [Fact]
    public void BuildPlaybackSequence_enables_da_segno_segno_after_da_segno_is_consumed()
    {
        var measures = new[]
        {
            Measure(0, target: "SegnoSegno"),
            Measure(1, jump: "DaSegnoSegno"),
            Measure(2, jump: "DaSegno"),
            Measure(3, target: "Segno"),
            Measure(4)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public void BuildPlaybackSequence_can_read_direction_tokens_from_direction_properties()
    {
        var measures = new[]
        {
            Measure(0),
            Measure(1, directionProperties: new Dictionary<string, string> { ["DaCapoAlFine"] = "1" }),
            Measure(2, directionProperties: new Dictionary<string, string> { ["Fine"] = "1" }),
            Measure(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 0, 1, 2);
    }

    [Fact]
    public void BuildPlaybackSequence_prevents_da_double_coda_until_da_coda_allows_it_when_da_coda_exists()
    {
        var measures = new[]
        {
            Measure(0, jump: "DaCapoAlDoubleCoda"),
            Measure(1, jump: "DaDoubleCoda"),
            Measure(2),
            Measure(3, target: "DoubleCoda"),
            Measure(4, jump: "DaCoda"),
            Measure(5)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 0, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void BuildPlaybackSequence_anchors_implicit_repeat_to_bar_zero_when_anacrusis_is_disabled()
    {
        var measures = new[]
        {
            Measure(0),
            Measure(1),
            Measure(2, repeatEnd: true, repeatCount: 2),
            Measure(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures);

        sequence.Should().Equal(0, 1, 2, 0, 1, 2, 3);
    }

    [Fact]
    public void BuildPlaybackSequence_anchors_implicit_repeat_to_first_full_bar_when_anacrusis_is_enabled()
    {
        var measures = new[]
        {
            Measure(0),
            Measure(1),
            Measure(2, repeatEnd: true, repeatCount: 2),
            Measure(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(measures, anacrusis: true);

        sequence.Should().Equal(0, 1, 2, 1, 2, 3);
    }

    [Fact]
    public void RebuildPlaybackSequence_updates_score_from_first_populated_track()
    {
        var score = new Score
        {
            Anacrusis = true,
            Tracks =
            [
                new TrackModel(),
                new TrackModel
                {
                    Measures =
                    [
                        Measure(0),
                        Measure(1),
                        Measure(2, repeatEnd: true, repeatCount: 2),
                        Measure(3)
                    ]
                }
            ]
        };

        var sequence = ScoreNavigation.RebuildPlaybackSequence(score);

        sequence.Should().Equal(0, 1, 2, 1, 2, 3);
        score.PlaybackMasterBarSequence.Should().Equal(0, 1, 2, 1, 2, 3);
        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeTrue();
    }

    [Fact]
    public void RebuildPlaybackSequence_prefers_score_timeline_bars_when_present()
    {
        var score = new Score
        {
            Tracks =
            [
                new TrackModel
                {
                    Measures =
                    [
                        Measure(0),
                        Measure(1),
                        Measure(2)
                    ]
                }
            ],
            TimelineBars =
            [
                TimelineBar(0),
                TimelineBar(1, repeatEnd: true, repeatCount: 2),
                TimelineBar(2)
            ]
        };

        var sequence = ScoreNavigation.RebuildPlaybackSequence(score);

        sequence.Should().Equal(0, 1, 0, 1, 2);
        score.PlaybackMasterBarSequence.Should().Equal(0, 1, 0, 1, 2);
    }

    [Fact]
    public void InvalidatePlaybackSequence_clears_cached_sequence_and_marks_it_stale()
    {
        var score = new Score
        {
            Tracks =
            [
                new TrackModel
                {
                    Measures = [Measure(0)]
                }
            ]
        };

        ScoreNavigation.RebuildPlaybackSequence(score);

        ScoreNavigation.InvalidatePlaybackSequence(score);

        score.PlaybackMasterBarSequence.Should().BeEmpty();
        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeFalse();
    }

    [Fact]
    public void EnsurePlaybackSequence_rebuilds_only_when_the_cached_sequence_is_stale()
    {
        var score = new Score
        {
            Tracks =
            [
                new TrackModel
                {
                    Measures =
                    [
                        Measure(0),
                        Measure(1, repeatEnd: true, repeatCount: 2),
                        Measure(2)
                    ]
                }
            ]
        };

        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeFalse();

        var rebuilt = ScoreNavigation.EnsurePlaybackSequence(score);

        rebuilt.Should().Equal(0, 1, 0, 1, 2);
        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeTrue();

        score.Tracks[0].Measures = [Measure(0), Measure(1), Measure(2)];
        var stale = ScoreNavigation.EnsurePlaybackSequence(score);
        stale.Should().Equal(0, 1, 0, 1, 2);

        ScoreNavigation.InvalidatePlaybackSequence(score);
        var refreshed = ScoreNavigation.EnsurePlaybackSequence(score);

        refreshed.Should().Equal(0, 1, 2);
        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeTrue();
    }

    private static MeasureModel Measure(
        int index,
        bool repeatStart = false,
        bool repeatEnd = false,
        int repeatCount = 0,
        string alternateEndings = "",
        string jump = "",
        string target = "",
        IReadOnlyDictionary<string, string>? directionProperties = null)
        => new()
        {
            Index = index,
            RepeatStart = repeatStart,
            RepeatEnd = repeatEnd,
            RepeatCount = repeatCount,
            AlternateEndings = alternateEndings,
            Jump = jump,
            Target = target,
            DirectionProperties = directionProperties ?? new Dictionary<string, string>()
        };

    private static TimelineBarModel TimelineBar(
        int index,
        bool repeatStart = false,
        bool repeatEnd = false,
        int repeatCount = 0,
        string alternateEndings = "",
        string jump = "",
        string target = "",
        IReadOnlyDictionary<string, string>? directionProperties = null)
        => new()
        {
            Index = index,
            TimeSignature = "4/4",
            RepeatStart = repeatStart,
            RepeatEnd = repeatEnd,
            RepeatCount = repeatCount,
            AlternateEndings = alternateEndings,
            Jump = jump,
            Target = target,
            DirectionProperties = directionProperties ?? new Dictionary<string, string>()
        };
}
