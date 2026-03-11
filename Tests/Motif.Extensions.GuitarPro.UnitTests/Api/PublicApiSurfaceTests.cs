namespace Motif.Extensions.GuitarPro.UnitTests;

using FluentAssertions;
using Motif;
using Motif.Extensions.GuitarPro;
using Motif.Extensions.GuitarPro.Abstractions;
using Motif.Extensions.GuitarPro.Implementation;
using Motif.Extensions.GuitarPro.Models;
using Motif.Extensions.GuitarPro.Models.Write;
using Motif.Models;
using Motif.Extensions.GuitarPro.Models.Raw;

public class PublicApiSurfaceTests
{
    [Fact]
    public void Supported_public_entry_points_are_available_and_pipeline_internals_stay_hidden()
    {
        typeof(GuitarProReader).Should().NotBeNull();
        typeof(GuitarProWriter).Should().NotBeNull();
        typeof(GuitarProModelExtensions).Should().NotBeNull();
        typeof(IScoreReader).Should().NotBeNull();
        typeof(IScoreWriter).Should().NotBeNull();
        typeof(IGuitarProReader).Should().NotBeNull();
        typeof(IGuitarProWriter).Should().NotBeNull();
        typeof(GpScoreExtension).Should().NotBeNull();
        typeof(GpTrackExtension).Should().NotBeNull();
        typeof(GpExtensionReattachmentResult).Should().NotBeNull();
        typeof(WriteDiagnostics).Should().NotBeNull();
        typeof(WriteDiagnosticEntry).Should().NotBeNull();

        new GuitarProReader().Should().BeAssignableTo<IScoreReader>().And.BeAssignableTo<IGuitarProReader>();
        new GuitarProWriter().Should().BeAssignableTo<IScoreWriter>().And.BeAssignableTo<IGuitarProWriter>();
        new Score().Tracks.Should().BeEmpty();

        typeof(IGuitarProReader).GetMethod(nameof(IGuitarProReader.ReadAsync), [typeof(Stream), typeof(CancellationToken)])
            .Should().NotBeNull();
        typeof(IGuitarProReader).GetMethod(nameof(IGuitarProReader.ReadAsync), [typeof(string), typeof(CancellationToken)])
            .Should().NotBeNull();

        typeof(IGpArchiveReader).IsNotPublic.Should().BeTrue();
        typeof(IGpifDeserializer).IsNotPublic.Should().BeTrue();
        typeof(IScoreMapper).IsNotPublic.Should().BeTrue();
        typeof(DefaultScoreMapper).IsNotPublic.Should().BeTrue();
        typeof(DefaultScoreUnmapper).IsNotPublic.Should().BeTrue();
        typeof(XmlGpifDeserializer).IsNotPublic.Should().BeTrue();
        typeof(XmlGpifSerializer).IsNotPublic.Should().BeTrue();
        typeof(ZipGpArchiveReader).IsNotPublic.Should().BeTrue();
        typeof(ZipGpArchiveWriter).IsNotPublic.Should().BeTrue();
        typeof(GpifDocument).IsNotPublic.Should().BeTrue();
        typeof(WriteResult).IsNotPublic.Should().BeTrue();
        typeof(GpifWriteFidelityDiagnostics).IsNotPublic.Should().BeTrue();
    }
}
