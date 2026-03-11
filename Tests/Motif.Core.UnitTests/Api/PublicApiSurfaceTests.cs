namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Models;

public class PublicApiSurfaceTests
{
    [Fact]
    public void Core_domain_model_and_json_helpers_are_available()
    {
        typeof(IExtensibleModel).Should().NotBeNull();
        typeof(IModelExtension).Should().NotBeNull();
        typeof(IScoreReader).Should().NotBeNull();
        typeof(IScoreWriter).Should().NotBeNull();
        typeof(ScoreJson).Should().NotBeNull();
        typeof(ScoreNavigation).Should().NotBeNull();
        typeof(TimelineBar).Should().NotBeNull();
        typeof(Staff).Should().NotBeNull();
        typeof(StaffMeasure).Should().NotBeNull();

        typeof(TimelineBar).GetProperty("XProperties").Should().BeNull();
        typeof(StaffMeasure).GetProperty("BarProperties").Should().BeNull();
        typeof(StaffMeasure).GetProperty("BarXProperties").Should().BeNull();
        typeof(Beat).GetProperty("Wah").Should().BeNull();
        typeof(Beat).GetProperty("VibratoWithTremBarStrength").Should().BeNull();
        typeof(Beat).GetProperty("Properties").Should().BeNull();
        typeof(Beat).GetProperty("XProperties").Should().BeNull();
        typeof(Note).GetProperty("XProperties").Should().BeNull();
        typeof(NoteArticulation).GetProperty("AntiAccentValue").Should().BeNull();

        new Score().Tracks.Should().BeEmpty();
        new Score().TimelineBars.Should().BeEmpty();
        new Track().Staves.Should().BeEmpty();
        new Staff().Measures.Should().BeEmpty();
    }
}
