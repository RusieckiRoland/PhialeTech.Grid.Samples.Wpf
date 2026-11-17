using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;
namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels;
public sealed class LocationsViewModel(IGridContext<LocationRow> grid) : IDisposable
{
    public static IReadOnlyList<IGridColumnDescriptor> ColumnDefinitions { get; } = new GridColumnsBuilder<LocationRow>()
        .Add(location => location.Id, "Lokalizacja", 750).Add(location => location.AssetCount, "Liczba pozycji", 150).Build();
    public IGridContext<LocationRow> Grid { get; } = grid;
    public IReadOnlyList<IGridColumnDescriptor> Columns => ColumnDefinitions;
    public void Dispose() => Grid.Dispose();
}
