namespace Motif.Extensions.GuitarPro.UnitTests;

using Motif.Models;
using System.Linq;

internal static class HierarchyTestHelpers
{
    public static Track SingleStaffTrack(int id, string name, params StaffMeasure[] measures)
        => new()
        {
            Id = id,
            Name = name,
            Staves =
            [
                new Staff
                {
                    StaffIndex = 0,
                    Measures = measures
                }
            ]
        };

    public static StaffMeasure PrimaryMeasure(this Track track, int measureIndex = 0)
        => track.StaffMeasure(staffIndex: 0, measureIndex);

    public static StaffMeasure StaffMeasure(this Track track, int staffIndex, int measureIndex = 0)
        => track.Staves
            .Single(staff => staff.StaffIndex == staffIndex)
            .Measures[measureIndex];
}
