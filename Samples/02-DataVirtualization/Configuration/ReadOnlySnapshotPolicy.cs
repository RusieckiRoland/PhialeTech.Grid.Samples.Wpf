using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Regions;

namespace PhialeTech.Grid.Samples.Wpf.DataVirtualization.Configuration;

/// <summary>Read-only local query example. The data source reads requested windows.</summary>
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
