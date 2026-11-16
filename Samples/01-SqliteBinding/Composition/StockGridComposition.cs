using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding.Composition;

/// <summary>Application-owned Grid session. All paging/query rules remain in Grid Core.</summary>
public sealed class StockGridComposition : IDisposable
{
    private GridDataSession<StockRow>? _session;

    public GridDataBinding CreateBinding(IReadOnlyList<StockRow> rows,
        IReadOnlyList<IGridColumnDescriptor> columns)
    {
        _session?.Dispose();
        _session = null;
        // SQLite was already read by the repository. This session queries the supplied rows in memory.
        var source = new GridDataSource<StockRow>(columns, row => row.Id, row => row with { });
        _session = source.CreateLocalSession(rows, readOnly: true);
        return GridApiFactory.CreateDataBinding(_session);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }
}
