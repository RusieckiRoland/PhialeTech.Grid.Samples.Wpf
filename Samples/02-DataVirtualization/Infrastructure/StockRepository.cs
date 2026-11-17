// The existing EF queries are shared as source links with the REST examples.
// Here they run locally: no HTTP server, no full ReadAll, and no database writes.
using PhialeTech.Grid.Samples.HttpRest.Server;
namespace PhialeTech.Grid.Samples.Wpf.DataVirtualization.Infrastructure;

public sealed class StockRepository(string database)
{
    public Task<StockPage> ReadAsync(StockQuery query, int limit, CancellationToken token) =>
        Task.Run(() => new EfStockRepository(database, recordLimit: limit).SearchAsync(query, token), token);
    public Task<GroupPage> ReadGroupedAsync(GroupRequest query, int limit, CancellationToken token) =>
        Task.Run(() => new GroupedStockRepository(database, recordLimit: limit).ReadAsync(query, token), token);
    public Task<LocatedRow> LocateAsync(LocateRequest query, int limit, CancellationToken token) =>
        Task.Run(() => new GroupedStockRepository(database, recordLimit: limit).LocateAsync(query, token), token);
}
