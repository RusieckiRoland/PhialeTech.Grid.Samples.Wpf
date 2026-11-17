using PhialeTech.Grid.Samples.Wpf.ServerGrouping.Infrastructure;
using System.Net.Http;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.ServerGrouping.Composition;

// The composition root owns the client and the remote Grid session.
public sealed class StockGridComposition : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private GridDataSession<StockRow>? _session;

    public GridDataBinding CreateBinding(IReadOnlyList<IGridColumnDescriptor> columns)
    {
        var provider = new GroupedStockProvider(_http, "http://127.0.0.1:5188");
        // No CommitPath: the session has no write endpoint.
        _session?.Dispose();
        _session = GridDataBuilder.For<StockRow>()
            .Columns(columns).Key(row => row.Id).Clone(row => row with { })
            .Query(provider).Build();
        return GridApiFactory.CreateDataBinding(_session);
    }

    public void Dispose() { _session?.Dispose(); _http.Dispose(); }
}
