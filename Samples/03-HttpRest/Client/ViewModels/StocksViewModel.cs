using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.HttpRest.ViewModels;

// No HTTP calls in the ViewModel: Grid drives remote queries through its DataSession.
public sealed class StocksViewModel
{
    public IReadOnlyList<IGridColumnDescriptor> Columns { get; } = new GridColumnsBuilder<StockRow>()
        .Add(p => p.ProductCode, "Symbol", 150)
        .Add(p => p.ProductName, "Product", 310)
        .Add(p => p.Category, "Category", 180)
        .Add(p => p.WarehouseName, "Warehouse", 220)
        .Add(p => p.UnitCode, "Unit", 65)
        .Add(p => p.OnHand, "On hand", 120)
        .Add(p => p.Reserved, "Reserved", 120)
        .Add(p => p.Available, "Available", 120)
        .Build();
    public GridDataBinding DataSession { get; }
    public GridContext<StockRow> Grid { get; }
    public StocksViewModel(Func<IReadOnlyList<IGridColumnDescriptor>, GridDataBinding> createBinding, IGridApi runtime)
    {
        DataSession = createBinding(Columns);
        Grid = GridApiFactory.CreateContext<StockRow>(runtime, DataSession);
    }
}
