namespace GPIO.NET.UnitTests;

using FluentAssertions;
using GPIO.NET.Implementation;
using GPIO.NET.Models.Raw;

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

        seq.Should().Equal(0, 1, 0, 1, 2);
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
}
