using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;
public sealed record LocationRow(string Id, IReadOnlyList<AssetRow> Assets)
{
    public int AssetCount => Assets.Count;
}
