using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Regions;
namespace PhialeTech.Grid.Samples.Wpf.ServerGrouping.Configuration;

public sealed class GroupingExamplePolicy : IGridCapabilityPolicy
{
    public bool CanViewRegion(GridRegionKind region) => region is GridRegionKind.CoreGridSurface
        or GridRegionKind.PagingRegion or GridRegionKind.GroupingRegion;
    public bool CanExecuteCommand(string key) => !key.StartsWith("grid.export", StringComparison.Ordinal)
        && !key.StartsWith("grid.summary", StringComparison.Ordinal);
}
