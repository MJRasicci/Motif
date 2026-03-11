namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Models;
using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;

public class PublicApiSurfaceTests
{
    [Fact]
    public void Public_abstractions_and_models_are_available()
    {
        typeof(GuitarProReader).Should().NotBeNull();
        typeof(GuitarProWriter).Should().NotBeNull();
        typeof(GuitarProModelExtensions).Should().NotBeNull();
        typeof(IScoreReader).Should().NotBeNull();
        typeof(IScoreWriter).Should().NotBeNull();
        typeof(IGuitarProReader).Should().NotBeNull();
        typeof(IGuitarProWriter).Should().NotBeNull();
        typeof(IGpArchiveReader).Should().NotBeNull();
        typeof(IGpifDeserializer).Should().NotBeNull();
        typeof(IScoreMapper).Should().NotBeNull();
        typeof(GpScoreExtension).Should().NotBeNull();
        typeof(GpTrackExtension).Should().NotBeNull();

        new GuitarProReader().Should().BeAssignableTo<IScoreReader>();
        new GuitarProWriter().Should().BeAssignableTo<IScoreWriter>();
        new GpReadOptions().Should().NotBeNull();
        new Score().Tracks.Should().BeEmpty();
        new GpifDocument().Tracks.Should().BeEmpty();
    }
}
