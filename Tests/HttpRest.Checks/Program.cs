using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.Wpf.HttpRest.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;
using PhialeTech.Grid.Samples.HttpRest.Server;

var database = StockServer.FindDatabase();
byte[] HashDatabase() { using var file = File.OpenRead(database); return SHA256.HashData(file); }
var originalHash = HashDatabase();
using var db = new SqliteConnection(new SqliteConnectionStringBuilder {
    DataSource = database, Mode = SqliteOpenMode.ReadOnly, Pooling = false
}.ToString());
db.Open();
long Count(string where = "") {
    using var command = db.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM stock_balances b JOIN products p ON p.id=b.product_id JOIN warehouses w ON w.id=b.warehouse_id " + where;
    return (long)command.ExecuteScalar()!;
}
var total = Count();
var sqlCommands = new System.Collections.Concurrent.ConcurrentQueue<string>();
await using var server = StockServer.Create("http://127.0.0.1:0", database, logSql: sqlCommands.Enqueue);
await server.StartAsync();
try {
    var url = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client = new HttpClient();
    var adapter = new HttpGridDataAdapter<StockRow>(client, StocksEndpoint.Options(url), (_, value) => value.ToString());
    var checks = 0;
    void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    GridQueryRequest Query(int offset = 0, GridSortDescriptor[]? sorts = null, GridFilterGroup? filters = null)
        => new(offset, 5, sorts ?? [], filters ?? GridFilterGroup.EmptyAnd(), [], Array.Empty<GridSummaryDescriptor>());
    Check(total == 1_000_000, "uses the existing million-row warehouse database from demo 01");
    var first = await adapter.QueryAsync(Query(), default);
    Check(first.TotalCount == total && first.Items.Count == 5, "first page and count from existing SQLite");
    var second = await adapter.QueryAsync(Query(5), default);
    Check(!first.Items.Select(r => r.Id).Intersect(second.Items.Select(r => r.Id)).Any(), "successive pages have no duplicate identities");
    var last = await adapter.QueryAsync(Query((int)total - 5), default);
    Check(last.Items.Count == 5 && last.TotalCount == total, "last page of the full million-row result");
    var sorted = await adapter.QueryAsync(Query(sorts: [new(nameof(StockRow.OnHand), GridSortDirection.Descending)]), default);
    using var maximum = db.CreateCommand();
    maximum.CommandText = "SELECT MAX(CAST(on_hand AS REAL)) FROM stock_balances";
    Check(sorted.Items[0].OnHand == Convert.ToDecimal(maximum.ExecuteScalar()), "numeric sorting agrees with direct SQL");
    var category = new GridFilterGroup([new(nameof(StockRow.Category), GridFilterOperator.Equals, "Cables")], GridLogicalOperator.And);
    var filtered = await adapter.QueryAsync(Query(filters: category), default);
    Check(filtered.TotalCount == Count("WHERE p.category = 'Cables'") && filtered.Items.All(r => r.Category == "Cables"), "category filter and count agree with database");
    var numeric = await adapter.QueryAsync(Query(filters: new GridFilterGroup(
        [new(nameof(StockRow.OnHand), GridFilterOperator.GreaterThan, 500m)], GridLogicalOperator.And)), default);
    Check(numeric.TotalCount == Count("WHERE CAST(b.on_hand AS REAL) > 500"), "quantity filter uses numeric SQL");
    Check(first.Items.All(r => r.Available == r.OnHand - r.Reserved && r.ProductCode.Length > 0 && r.WarehouseName.Length > 0),
        "shared StockRow model retains warehouse, product and computed availability");
    using var plain = await client.PostAsJsonAsync(url + "/api/stocks/search", new { skip = 0, take = 2 });
    var plainPage = await plain.Content.ReadFromJsonAsync<StockPage>();
    Check(plain.IsSuccessStatusCode && plainPage!.Stocks.Length == 2 && plainPage.Total == total, "ordinary HTTP request without Grid types");
    foreach (var literal in new[] { "' OR 1=1 --", "%" }) {
        using var response = await client.PostAsJsonAsync(url + "/api/stocks/search", new {
            conditions = new[] { new { field = "productName", comparison = "contains", value = literal } }
        });
        Check((await response.Content.ReadFromJsonAsync<StockPage>())!.Total == 0, "filter value is literal: " + literal);
    }
    using var invalid = await client.PostAsJsonAsync(url + "/api/stocks/search", new { take = 1000 });
    Check(invalid.StatusCode == HttpStatusCode.BadRequest, "invalid page size rejected");
    using var unknown = await client.PostAsJsonAsync(url + "/api/stocks/search", new { orderBy = new[] { new { field = "Secret", direction = "asc" } } });
    Check(unknown.StatusCode == HttpStatusCode.BadRequest, "unknown SQL identifiers rejected");
    Check(!typeof(StockServer).Assembly.GetReferencedAssemblies().Any(a => a.Name!.StartsWith("Phiale") || a.Name.StartsWith("UniversalInput")),
        "server has no Phiale package dependencies");
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    try { await adapter.QueryAsync(Query(), cancellation.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { Check(true, "request cancellation"); }
    Check(sqlCommands.Any(sql => sql.Contains("COUNT(*)")) && sqlCommands.Any(sql => sql.Contains("LIMIT @") && sql.Contains("OFFSET @")),
        "EF executes COUNT and parameterized paging in SQLite");
    await EfParityChecks.RunAsync(database, url);
    await server.StopAsync();
    try { await adapter.QueryAsync(Query(), default); throw new Exception("Stopped server answered"); }
    catch (GridDataReadException) { Check(true, "unavailable server produces a read error"); }
    Check(originalHash.SequenceEqual(HashDatabase()), "existing warehouse database is unchanged after all HTTP tests");
    var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".sqlite");
    try { _ = new StockRepository(missing); throw new Exception("Missing database accepted"); }
    catch (FileNotFoundException) { Check(!File.Exists(missing), "missing database fails clearly without creating a replacement"); }
    Console.WriteLine($"PASS {checks} warehouse REST integration checks");
} finally {
    await server.StopAsync();
}