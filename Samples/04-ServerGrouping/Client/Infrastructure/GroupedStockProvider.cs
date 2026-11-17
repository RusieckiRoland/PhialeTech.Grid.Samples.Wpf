using System.Net.Http;
using System.Text.Json;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Query;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.ServerGrouping.Infrastructure;

// Connect the application's group-location endpoint to Core navigation.
public sealed class GroupedStockProvider(HttpClient http, string address) :
    IGridQueryDataProvider<StockRow>, IGridGroupedQueryDataProvider<StockRow>, IGridGroupLocationProvider
{
    private readonly HttpGridDataAdapter<StockRow> _adapter = new(http, GroupedStocksEndpoint.Options(address),
        (_, value) => JsonSerializer.Deserialize<object>(value.GetRawText())!);
    public Task<GridQueryResult<StockRow>> QueryAsync(GridQueryRequest query, CancellationToken token) =>
        _adapter.QueryAsync(query, token);
    public Task<GridGroupedQueryResult<StockRow>> QueryGroupedAsync(GridGroupedQueryRequest query, CancellationToken token) =>
        _adapter.QueryGroupedAsync(query, token);
    public Task<GridRecordLocation> LocateGroupEndAsync(string groupId, GridGroupedQueryRequest query, CancellationToken token) =>
        _adapter.LocateRecordAsync("groupend:" + groupId, query, token);
}
