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
        typeof(IArchiveContributor).Should().NotBeNull();
        typeof(ArchiveEntry).Should().NotBeNull();
        typeof(IScoreReader).Should().NotBeNull();
        typeof(IScoreWriter).Should().NotBeNull();
        typeof(IFormatHandler).Should().NotBeNull();
        typeof(MotifArchiveContributorAttribute).Should().NotBeNull();
        typeof(MotifFormatHandlerAttribute).Should().NotBeNull();
        typeof(MotifScore).Should().NotBeNull();
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

        typeof(IScoreReader).GetMethod(nameof(IScoreReader.ReadAsync), [typeof(Stream), typeof(CancellationToken)])
            .Should().NotBeNull();
        typeof(IScoreReader).GetMethod(nameof(IScoreReader.ReadAsync), [typeof(string), typeof(CancellationToken)])
            .Should().NotBeNull();
        typeof(IScoreWriter).GetMethod(nameof(IScoreWriter.WriteAsync), [typeof(Score), typeof(Stream), typeof(CancellationToken)])
            .Should().NotBeNull();
        typeof(IScoreWriter).GetMethod(nameof(IScoreWriter.WriteAsync), [typeof(Score), typeof(string), typeof(CancellationToken)])
            .Should().NotBeNull();
        typeof(MotifScore).GetMethod(nameof(MotifScore.CreateReader), [typeof(string)])
            .Should().NotBeNull();
        typeof(MotifScore).GetMethod(nameof(MotifScore.CreateWriter), [typeof(string)])
            .Should().NotBeNull();
        typeof(MotifScore).GetMethod(nameof(MotifScore.OpenAsync), [typeof(string), typeof(string), typeof(CancellationToken)])
            .Should().NotBeNull();
        typeof(MotifScore).GetMethod(nameof(MotifScore.RegisterArchiveContributor), [typeof(IArchiveContributor)])
            .Should().NotBeNull();
    }
}
