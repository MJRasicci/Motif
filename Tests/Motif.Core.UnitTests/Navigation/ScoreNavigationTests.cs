namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Models;

public class ScoreNavigationTests
{
    [Fact]
    public void BuildPlaybackSequence_handles_simple_repeat_with_two_endings()
    {
        var timelineBars = new[]
        {
            TimelineBar(0, repeatStart: true),
            TimelineBar(1, repeatEnd: true, repeatCount: 2),
            TimelineBar(2, alternateEndings: "1"),
            TimelineBar(3, alternateEndings: "2")
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 0, 1, 2, 3);
    }

    [Fact]
    public void BuildPlaybackSequence_stops_infinite_loops_with_guard_limit()
    {
        var timelineBars = new[]
        {
            TimelineBar(0, repeatStart: true, repeatEnd: true, repeatCount: 100000)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Count.Should().BeLessThan(20000);
        sequence.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildPlaybackSequence_handles_da_capo_al_fine()
    {
        var timelineBars = new[]
        {
            TimelineBar(0),
            TimelineBar(1, jump: "DaCapoAlFine"),
            TimelineBar(2, target: "Fine"),
            TimelineBar(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 0, 1, 2);
    }

    [Fact]
    public void BuildPlaybackSequence_handles_da_capo_al_coda_with_conditional_da_coda_jump()
    {
        var timelineBars = new[]
        {
            TimelineBar(0),
            TimelineBar(1, jump: "DaCapoAlCoda"),
            TimelineBar(2, jump: "DaCoda"),
            TimelineBar(3, target: "Coda"),
            TimelineBar(4)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 0, 1, 2, 3, 4);
    }

    [Fact]
    public void BuildPlaybackSequence_handles_da_segno_segno_al_double_coda()
    {
        var timelineBars = new[]
        {
            TimelineBar(0),
            TimelineBar(1, target: "SegnoSegno"),
            TimelineBar(2, jump: "DaSegnoSegnoAlDoubleCoda"),
            TimelineBar(3, jump: "DaDoubleCoda"),
            TimelineBar(4, target: "DoubleCoda"),
            TimelineBar(5)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 2, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void BuildPlaybackSequence_ignores_da_coda_without_pending_al_coda_route()
    {
        var timelineBars = new[]
        {
            TimelineBar(0),
            TimelineBar(1, jump: "DaCoda"),
            TimelineBar(2, target: "Coda")
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void BuildPlaybackSequence_uses_last_matching_direction_marker()
    {
        var timelineBars = new[]
        {
            TimelineBar(0, jump: "DaCapoAlCoda"),
            TimelineBar(1, target: "Coda"),
            TimelineBar(2),
            TimelineBar(3, jump: "DaCoda"),
            TimelineBar(4, target: "Coda"),
            TimelineBar(5)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 0, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void BuildPlaybackSequence_honors_extended_alternate_endings_inside_repeat_loop()
    {
        var timelineBars = new[]
        {
            TimelineBar(0, repeatStart: true),
            TimelineBar(1, alternateEndings: "1"),
            TimelineBar(2, alternateEndings: "2"),
            TimelineBar(3, repeatEnd: true, repeatCount: 2)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 0, 2, 3);
    }

    [Fact]
    public void BuildPlaybackSequence_enables_da_segno_segno_after_da_segno_is_consumed()
    {
        var timelineBars = new[]
        {
            TimelineBar(0, target: "SegnoSegno"),
            TimelineBar(1, jump: "DaSegnoSegno"),
            TimelineBar(2, jump: "DaSegno"),
            TimelineBar(3, target: "Segno"),
            TimelineBar(4)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public void BuildPlaybackSequence_can_read_direction_tokens_from_direction_properties()
    {
        var timelineBars = new[]
        {
            TimelineBar(0),
            TimelineBar(1, directionProperties: new Dictionary<string, string> { ["DaCapoAlFine"] = "1" }),
            TimelineBar(2, directionProperties: new Dictionary<string, string> { ["Fine"] = "1" }),
            TimelineBar(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 0, 1, 2);
    }

    [Fact]
    public void BuildPlaybackSequence_prevents_da_double_coda_until_da_coda_allows_it_when_da_coda_exists()
    {
        var timelineBars = new[]
        {
            TimelineBar(0, jump: "DaCapoAlDoubleCoda"),
            TimelineBar(1, jump: "DaDoubleCoda"),
            TimelineBar(2),
            TimelineBar(3, target: "DoubleCoda"),
            TimelineBar(4, jump: "DaCoda"),
            TimelineBar(5)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 0, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void BuildPlaybackSequence_anchors_implicit_repeat_to_bar_zero_when_anacrusis_is_disabled()
    {
        var timelineBars = new[]
        {
            TimelineBar(0),
            TimelineBar(1),
            TimelineBar(2, repeatEnd: true, repeatCount: 2),
            TimelineBar(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars);

        sequence.Should().Equal(0, 1, 2, 0, 1, 2, 3);
    }

    [Fact]
    public void BuildPlaybackSequence_anchors_implicit_repeat_to_first_full_bar_when_anacrusis_is_enabled()
    {
        var timelineBars = new[]
        {
            TimelineBar(0),
            TimelineBar(1),
            TimelineBar(2, repeatEnd: true, repeatCount: 2),
            TimelineBar(3)
        };

        var sequence = ScoreNavigation.BuildPlaybackSequence(timelineBars, anacrusis: true);

        sequence.Should().Equal(0, 1, 2, 1, 2, 3);
    }

    [Fact]
    public void RebuildPlaybackSequence_uses_score_timeline_bars()
    {
        var score = new Score
        {
            Anacrusis = true,
            TimelineBars =
            [
                TimelineBar(0),
                TimelineBar(1),
                TimelineBar(2, repeatEnd: true, repeatCount: 2),
                TimelineBar(3)
            ],
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    new StaffMeasure { Index = 0, StaffIndex = 0 },
                    new StaffMeasure { Index = 1, StaffIndex = 0 },
                    new StaffMeasure { Index = 2, StaffIndex = 0 },
                    new StaffMeasure { Index = 3, StaffIndex = 0 })
            ]
        };

        var sequence = ScoreNavigation.RebuildPlaybackSequence(score);

        sequence.Should().Equal(0, 1, 2, 1, 2, 3);
        score.PlaybackMasterBarSequence.Should().Equal(0, 1, 2, 1, 2, 3);
        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeTrue();
    }

    [Fact]
    public void RebuildPlaybackSequence_uses_score_timeline_for_staff_only_tracks()
    {
        var score = new Score
        {
            TimelineBars =
            [
                TimelineBar(0),
                TimelineBar(1, repeatEnd: true, repeatCount: 2),
                TimelineBar(2)
            ],
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    new StaffMeasure { Index = 0, StaffIndex = 0 },
                    new StaffMeasure { Index = 1, StaffIndex = 0 },
                    new StaffMeasure { Index = 2, StaffIndex = 0 })
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
            TimelineBars = [TimelineBar(0)],
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(new StaffMeasure { Index = 0, StaffIndex = 0 })
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
            TimelineBars =
            [
                TimelineBar(0),
                TimelineBar(1, repeatEnd: true, repeatCount: 2),
                TimelineBar(2)
            ],
            Tracks =
            [
                HierarchyTestHelpers.SingleStaffTrack(
                    new StaffMeasure { Index = 0, StaffIndex = 0 },
                    new StaffMeasure { Index = 1, StaffIndex = 0 },
                    new StaffMeasure { Index = 2, StaffIndex = 0 })
            ]
        };

        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeFalse();

        var rebuilt = ScoreNavigation.EnsurePlaybackSequence(score);

        rebuilt.Should().Equal(0, 1, 0, 1, 2);
        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeTrue();

        score.Tracks[0].PrimaryMeasure(1).Beats =
        [
            new Beat
            {
                Id = 10
            }
        ];

        var stale = ScoreNavigation.EnsurePlaybackSequence(score);
        stale.Should().Equal(0, 1, 0, 1, 2);

        score.TimelineBars =
        [
            TimelineBar(0),
            TimelineBar(1),
            TimelineBar(2)
        ];
        ScoreNavigation.InvalidatePlaybackSequence(score);
        var refreshed = ScoreNavigation.EnsurePlaybackSequence(score);

        refreshed.Should().Equal(0, 1, 2);
        ScoreNavigation.HasCurrentPlaybackSequence(score).Should().BeTrue();
    }

    private static TimelineBar TimelineBar(
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
