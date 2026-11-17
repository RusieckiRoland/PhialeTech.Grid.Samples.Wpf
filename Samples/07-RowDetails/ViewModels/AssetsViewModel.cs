using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.RowDetails.Configuration;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.ViewModels;
public sealed class AssetsViewModel(IGridContext<AssetRow> grid) : IDisposable
{
    public IGridContext<AssetRow> Grid { get; } = grid;
    public IReadOnlyList<IGridColumnDescriptor> Columns => AssetColumns.All;
    public void Dispose() => Grid.Dispose();
}
