namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
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
        typeof(IGuitarProReader).Should().NotBeNull();
        typeof(IGpArchiveReader).Should().NotBeNull();
        typeof(IGpifDeserializer).Should().NotBeNull();
        typeof(IScoreMapper).Should().NotBeNull();
        typeof(GpScoreExtension).Should().NotBeNull();
        typeof(GpTrackExtension).Should().NotBeNull();

        new GpReadOptions().Should().NotBeNull();
        new GuitarProScore().Tracks.Should().BeEmpty();
        new GpifDocument().Tracks.Should().BeEmpty();
    }
}
