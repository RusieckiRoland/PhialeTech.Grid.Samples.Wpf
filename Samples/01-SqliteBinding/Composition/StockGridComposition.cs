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
        if (_session != null) throw new InvalidOperationException("This example loads one snapshot per window.");
        var definitions = columns.Select(column => new GridColumnDefinition(
            column.Id, column.Header, column.Width, isEditable: false,
            valueType: column.ValueType)).ToArray();
        var source = new GridDataSource<StockRow>(definitions, row => row.Id, row => row with { });
        _session = source.CreateLocalSession(rows, readOnly: true);
        return GridApiFactory.CreateDataBinding(_session);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }
}
