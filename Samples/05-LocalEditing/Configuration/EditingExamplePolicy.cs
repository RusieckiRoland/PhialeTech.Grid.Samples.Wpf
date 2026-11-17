using PhialeGrid.Core.Capabilities;
using PhialeGrid.Core.Regions;
namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;

public sealed class EditingExamplePolicy : IGridCapabilityPolicy
{
    public bool CanViewRegion(GridRegionKind region) => region is GridRegionKind.CoreGridSurface
        or GridRegionKind.TopCommandRegion or GridRegionKind.SideToolRegion
        or GridRegionKind.ChangePanelRegion or GridRegionKind.ValidationPanelRegion;
    public bool CanExecuteCommand(string key) => key is not ("grid.row.create" or "grid.row.delete" or "grid.row.undoDelete")
        && !key.StartsWith("grid.group", StringComparison.Ordinal)
        && !key.StartsWith("grid.summary", StringComparison.Ordinal)
        && !key.StartsWith("grid.export", StringComparison.Ordinal);
}
