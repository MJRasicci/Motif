namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models.Raw;

public class NavigationResolverTests
{
    [Fact]
    public void Resolver_handles_simple_repeat_with_two_endings()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0, RepeatStart = true },
            new GpifMasterBar { Index = 1, RepeatEnd = true, RepeatCount = 2 },
            new GpifMasterBar { Index = 2, AlternateEndings = "1" },
            new GpifMasterBar { Index = 3, AlternateEndings = "2" },
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 0, 1, 2, 3);
    }

    [Fact]
    public void Resolver_stops_infinite_loops_with_guard_limit()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0, RepeatStart = true, RepeatEnd = true, RepeatCount = 100000 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Count.Should().BeLessThan(20000);
        seq.Should().NotBeEmpty();
    }

    [Fact]
    public void Resolver_handles_da_capo_al_fine()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0 },
            new GpifMasterBar { Index = 1, Jump = "DaCapoAlFine" },
            new GpifMasterBar { Index = 2, Target = "Fine" },
            new GpifMasterBar { Index = 3 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 0, 1, 2);
    }

    [Fact]
    public void Resolver_handles_da_capo_al_coda_with_conditional_da_coda_jump()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0 },
            new GpifMasterBar { Index = 1, Jump = "DaCapoAlCoda" },
            new GpifMasterBar { Index = 2, Jump = "DaCoda" },
            new GpifMasterBar { Index = 3, Target = "Coda" },
            new GpifMasterBar { Index = 4 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 0, 1, 2, 3, 4);
    }

    [Fact]
    public void Resolver_handles_da_segno_segno_al_double_coda()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0 },
            new GpifMasterBar { Index = 1, Target = "SegnoSegno" },
            new GpifMasterBar { Index = 2, Jump = "DaSegnoSegnoAlDoubleCoda" },
            new GpifMasterBar { Index = 3, Jump = "DaDoubleCoda" },
            new GpifMasterBar { Index = 4, Target = "DoubleCoda" },
            new GpifMasterBar { Index = 5 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 2, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void Resolver_ignores_da_coda_without_pending_al_coda_route()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0 },
            new GpifMasterBar { Index = 1, Jump = "DaCoda" },
            new GpifMasterBar { Index = 2, Target = "Coda" }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Resolver_uses_last_matching_direction_marker()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0, Jump = "DaCapoAlCoda" },
            new GpifMasterBar { Index = 1, Target = "Coda" },
            new GpifMasterBar { Index = 2 },
            new GpifMasterBar { Index = 3, Jump = "DaCoda" },
            new GpifMasterBar { Index = 4, Target = "Coda" },
            new GpifMasterBar { Index = 5 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 0, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void Resolver_honors_extended_alternate_endings_inside_repeat_loop()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0, RepeatStart = true },
            new GpifMasterBar { Index = 1, AlternateEndings = "1" },
            new GpifMasterBar { Index = 2, AlternateEndings = "2" },
            new GpifMasterBar { Index = 3, RepeatEnd = true, RepeatCount = 2 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 0, 2, 3);
    }

    [Fact]
    public void Resolver_enables_da_segno_segno_after_da_segno_is_consumed()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0, Target = "SegnoSegno" },
            new GpifMasterBar { Index = 1, Jump = "DaSegnoSegno" },
            new GpifMasterBar { Index = 2, Jump = "DaSegno" },
            new GpifMasterBar { Index = 3, Target = "Segno" },
            new GpifMasterBar { Index = 4 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public void Resolver_can_read_direction_tokens_from_direction_properties()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0 },
            new GpifMasterBar
            {
                Index = 1,
                DirectionProperties = new Dictionary<string, string>
                {
                    ["DaCapoAlFine"] = "1"
                }
            },
            new GpifMasterBar
            {
                Index = 2,
                DirectionProperties = new Dictionary<string, string>
                {
                    ["Fine"] = "1"
                }
            },
            new GpifMasterBar { Index = 3 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 0, 1, 2);
    }

    [Fact]
    public void Resolver_prevents_da_double_coda_until_da_coda_allows_it_when_da_coda_exists()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0, Jump = "DaCapoAlDoubleCoda" },
            new GpifMasterBar { Index = 1, Jump = "DaDoubleCoda" },
            new GpifMasterBar { Index = 2 },
            new GpifMasterBar { Index = 3, Target = "DoubleCoda" },
            new GpifMasterBar { Index = 4, Jump = "DaCoda" },
            new GpifMasterBar { Index = 5 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 0, 1, 2, 3, 4, 5);
    }

    [Fact]
    public void Resolver_anchors_implicit_repeat_to_bar_zero_when_anacrusis_is_disabled()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0 },
            new GpifMasterBar { Index = 1 },
            new GpifMasterBar { Index = 2, RepeatEnd = true, RepeatCount = 2 },
            new GpifMasterBar { Index = 3 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars);

        seq.Should().Equal(0, 1, 2, 0, 1, 2, 3);
    }

    [Fact]
    public void Resolver_anchors_implicit_repeat_to_first_full_bar_when_anacrusis_is_enabled()
    {
        var bars = new[]
        {
            new GpifMasterBar { Index = 0 },
            new GpifMasterBar { Index = 1 },
            new GpifMasterBar { Index = 2, RepeatEnd = true, RepeatCount = 2 },
            new GpifMasterBar { Index = 3 }
        };

        var resolver = new DefaultNavigationResolver();
        var seq = resolver.BuildPlaybackSequence(bars, anacrusis: true);

        seq.Should().Equal(0, 1, 2, 1, 2, 3);
    }
}
