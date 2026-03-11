namespace Motif.Core.UnitTests;

using FluentAssertions;
using Motif.Models;

public class ModelExtensionTests
{
    [Fact]
    public void Typed_extensions_can_be_attached_retrieved_and_removed()
    {
        var score = new Score();
        var extension = new TestScoreExtension("primary");

        score.TryGetExtension<TestScoreExtension>(out _).Should().BeFalse();
        score.GetExtensions().Should().BeEmpty();

        score.SetExtension(extension);

        score.TryGetExtension<TestScoreExtension>(out var attached).Should().BeTrue();
        attached.Should().BeSameAs(extension);
        score.GetExtension<TestScoreExtension>().Should().BeSameAs(extension);
        score.GetRequiredExtension<TestScoreExtension>().Should().BeSameAs(extension);
        score.GetExtensions().Should().ContainSingle().Which.Should().BeSameAs(extension);

        score.RemoveExtension<TestScoreExtension>().Should().BeTrue();
        score.TryGetExtension<TestScoreExtension>(out _).Should().BeFalse();
        score.GetExtensions().Should().BeEmpty();
    }

    [Fact]
    public void Extensions_are_isolated_per_model_instance()
    {
        var lead = new Track { Name = "Lead" };
        var rhythm = new Track { Name = "Rhythm" };

        lead.SetExtension(new TestTrackExtension(1));

        lead.GetRequiredExtension<TestTrackExtension>().Value.Should().Be(1);
        rhythm.TryGetExtension<TestTrackExtension>(out _).Should().BeFalse();
    }

    [Fact]
    public void Setting_same_extension_type_replaces_existing_instance()
    {
        var beat = new Beat();

        beat.SetExtension(new TestBeatExtension("first"));
        beat.SetExtension(new TestBeatExtension("second"));

        beat.GetExtensions().Should().ContainSingle();
        beat.GetRequiredExtension<TestBeatExtension>().Name.Should().Be("second");
    }

    [Fact]
    public void Missing_required_extension_throws_clear_error()
    {
        var note = new Note();

        var act = () => note.GetRequiredExtension<TestBeatExtension>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestBeatExtension*");
    }

    private sealed record TestScoreExtension(string Name) : IModelExtension;

    private sealed record TestTrackExtension(int Value) : IModelExtension;

    private sealed record TestBeatExtension(string Name) : IModelExtension;

}
