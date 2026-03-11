namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif.Extensions.GuitarPro;
using Motif.Models;

public class AdvancedArticulationFixtureTests
{
    private static int? SourceSlideFlagsOf(Note note)
        => note.GetRequiredGuitarPro().Metadata.SourceSlideFlags;

    [Fact]
    public async Task Schema_reference_fixture_exposes_slide_bend_and_harmonic_data()
    {
        var fixturePath = GuitarProFixture.PathFor("schema-reference.gp");
        File.Exists(fixturePath).Should().BeTrue();

        var reader = new Motif.Extensions.GuitarPro.GuitarProReader();
        var score = await reader.ReadAsync(fixturePath, cancellationToken: TestContext.Current.CancellationToken);

        var notes = score.Tracks
            .SelectMany(t => t.Staves[0].Measures)
            .SelectMany(m => m.Beats)
            .SelectMany(b => b.Notes)
            .ToArray();

        notes.Should().NotBeEmpty();
        notes.Any(n => n.Articulation.Slides.Count > 0).Should().BeTrue();
        notes.Any(n => n.Articulation.Bend is not null).Should().BeTrue();
        notes.Any(n => n.Articulation.Harmonic is not null).Should().BeTrue();
        score.Tracks
            .SelectMany(t => t.Staves[0].Measures)
            .SelectMany(m => m.Beats)
            .Any(b => b.PalmMuted)
            .Should().BeTrue();

        var seenSlideFlags = notes
            .Select(SourceSlideFlagsOf)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .OrderBy(v => v)
            .ToArray();

        seenSlideFlags.Should().Contain(new[] { 1, 2, 4, 8, 16, 32, 64, 128 });

        var byId = notes
            .Where(n => n.Id > 0)
            .GroupBy(n => n.Id)
            .ToDictionary(g => g.Key, g => g.First());

        byId[61].Articulation.Slides.Should().ContainSingle().Which.Should().Be(SlideType.Legato);
        byId[63].Articulation.Slides.Should().ContainSingle().Which.Should().Be(SlideType.Shift);
        byId[65].Articulation.Slides.Should().ContainSingle().Which.Should().Be(SlideType.IntoFromBelow);
        byId[66].Articulation.Slides.Should().ContainSingle().Which.Should().Be(SlideType.IntoFromAbove);
        byId[67].Articulation.Slides.Should().ContainSingle().Which.Should().Be(SlideType.OutDown);
        byId[68].Articulation.Slides.Should().ContainSingle().Which.Should().Be(SlideType.OutUp);

        byId[69].Articulation.HopoType.Should().Be(HopoTypeKind.HammerOn);
        byId[69].Articulation.HopoDestinationNoteId.Should().Be(70);
        byId[70].Articulation.HopoType.Should().Be(HopoTypeKind.HammerOn);
        byId[70].Articulation.HopoOriginNoteId.Should().Be(69);
        byId[71].Articulation.HopoType.Should().Be(HopoTypeKind.PullOff);
        byId[71].Articulation.HopoDestinationNoteId.Should().Be(72);
        byId[72].Articulation.HopoType.Should().Be(HopoTypeKind.PullOff);
        byId[72].Articulation.HopoOriginNoteId.Should().Be(71);
        byId[73].Articulation.HopoType.Should().Be(HopoTypeKind.PullOff);
        byId[73].Articulation.HopoOriginNoteId.Should().Be(69);
        byId[73].Articulation.HopoDestinationNoteId.Should().Be(72);

        byId[43].Articulation.Harmonic!.Kind.Should().Be(HarmonicTypeKind.Natural);
        byId[44].Articulation.Harmonic!.Kind.Should().Be(HarmonicTypeKind.Artificial);
        byId[45].Articulation.Harmonic!.Kind.Should().Be(HarmonicTypeKind.Pinch);
        byId[46].Articulation.Harmonic!.Kind.Should().Be(HarmonicTypeKind.Tap);
        byId[47].Articulation.Harmonic!.Kind.Should().Be(HarmonicTypeKind.Semi);
        byId[48].Articulation.Harmonic!.Kind.Should().Be(HarmonicTypeKind.Feedback);

        byId[80].Articulation.Bend!.Type.Should().Be(BendTypeKind.Bend);
        byId[81].Articulation.Bend!.Type.Should().Be(BendTypeKind.BendAndRelease);
        byId[82].Articulation.Bend!.Type.Should().Be(BendTypeKind.Prebend);
        byId[83].Articulation.Bend!.Type.Should().Be(BendTypeKind.PrebendAndBend);
        byId[84].Articulation.Bend!.Type.Should().Be(BendTypeKind.PrebendAndRelease);
        byId[80].Articulation.Bend!.DestinationValue.Should().Be(1m);
        byId[82].Articulation.Bend!.OriginValue.Should().Be(1m);
    }
}
