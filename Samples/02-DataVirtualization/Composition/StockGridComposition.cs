// Configure the same Grid with a query source instead of an already loaded list.
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Data;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Infrastructure;

namespace PhialeTech.Grid.Samples.Wpf.DataVirtualization.Composition;

public sealed class StockGridComposition(StockRepository repository) : IDisposable
{
    private GridDataSession<StockRow>? _session;
    public GridDataBinding CreateBinding(int recordLimit, IReadOnlyList<IGridColumnDescriptor> columns)
    {
        _session?.Dispose();
        _session = null;
        _session = GridDataBuilder.For<StockRow>()
            .Columns(columns) // Same columns as example 01.
            .Key(row => row.Id) // Stable identity across independently read windows.
            .Clone(row => row with { }) // StockRow is a record; make an independent copy.
            .Query(new StockQuerySource(repository, recordLimit)) // No ReadAll and no full list.
            .Build(); // Creates a session; the first query is issued by Grid.
        return GridApiFactory.CreateDataBinding(_session);
    }
    public void Dispose() { _session?.Dispose(); _session = null; }
}
