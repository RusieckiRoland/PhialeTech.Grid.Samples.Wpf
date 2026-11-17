using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Regions;

namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding.Configuration;

/// <summary>Application configuration for an already loaded, read-only snapshot.</summary>
public sealed class ReadOnlySnapshotPolicy : IGridCapabilityPolicy
{
    public bool CanViewRegion(GridRegionKind regionKind) => regionKind is
        GridRegionKind.CoreGridSurface or
        GridRegionKind.GroupingRegion or
        GridRegionKind.PagingRegion or
        GridRegionKind.SummaryBottomRegion or
        GridRegionKind.SummaryDesignerRegion or
        GridRegionKind.SideToolRegion;

    // Region availability is this policy's role. Editing is separately blocked by IsGridReadOnly.
    public bool CanExecuteCommand(string commandKey) => true;
}
