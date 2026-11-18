using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Regions;

namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Configuration;

// Keep this lesson focused on editing. The standard edit/save and change panels remain available.
public sealed class NestedExamplePolicy : IGridCapabilityPolicy
{
    public bool CanViewRegion(GridRegionKind region) => region is GridRegionKind.CoreGridSurface
        or GridRegionKind.TopCommandRegion or GridRegionKind.ChangePanelRegion or GridRegionKind.ValidationPanelRegion;

    public bool CanExecuteCommand(string key) => !key.StartsWith("grid.group", StringComparison.Ordinal)
        && !key.StartsWith("grid.summary", StringComparison.Ordinal) && !key.StartsWith("grid.export", StringComparison.Ordinal);
}
