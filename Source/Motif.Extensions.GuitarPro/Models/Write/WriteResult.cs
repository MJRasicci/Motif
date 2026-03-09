namespace Motif.Extensions.GuitarPro.Models.Write;

using Motif.Extensions.GuitarPro.Models.Raw;

public sealed class WriteResult
{
    public required GpifDocument RawDocument { get; init; }

    public required WriteDiagnostics Diagnostics { get; init; }
}
