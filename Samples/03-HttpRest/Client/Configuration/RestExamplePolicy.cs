using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Regions;

namespace PhialeTech.Grid.Samples.Wpf.HttpRest.Configuration;

// Offer only the regions and commands supported by this read-only REST example.
public sealed class RestExamplePolicy : IGridCapabilityPolicy
{
    public bool CanViewRegion(GridRegionKind region) =>
        region is GridRegionKind.CoreGridSurface or GridRegionKind.PagingRegion;

    public bool CanExecuteCommand(string key) =>
        !key.StartsWith("grid.group", StringComparison.Ordinal)
        && !key.StartsWith("grid.summary", StringComparison.Ordinal)
        && !key.StartsWith("grid.export", StringComparison.Ordinal);
}