using Motif;
using Motif.Extensions.GuitarPro;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Motif.Extensions.GuitarPro.UnitTests")]
[assembly: InternalsVisibleTo("motif-cli")]
[assembly: MotifArchiveContributor(typeof(Motif.Extensions.GuitarPro.Implementation.GuitarProArchiveContributor))]
[assembly: MotifFormatHandler(typeof(GpFormatHandler))]
[assembly: MotifFormatHandler(typeof(GpifFormatHandler))]
