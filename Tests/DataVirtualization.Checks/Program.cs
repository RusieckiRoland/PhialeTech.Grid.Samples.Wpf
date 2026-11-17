using PhialeGrid.Core.Data;
using PhialeGrid.Core.Query;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.ViewModels;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var database = Path.Combine(root, "Data/warehouse-1000000.sqlite");
if (!File.Exists(database)) throw new FileNotFoundException(database);
var before = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(database));
var repository = new StockRepository(database);
var source = new StockQuerySource(repository, 10000);
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }
var first = await source.QueryAsync(new GridQueryRequest(0, 100, null, null, null, null), default);
Check(first.TotalCount == 10000 && first.Items.Count == 100, "only first 100 records, total 10000");
var next = await source.QueryAsync(new GridQueryRequest(100, 100, null, null, null, null), default);
Check(!first.Items.Select(row => row.Id).Intersect(next.Items.Select(row => row.Id)).Any(), "adjacent windows do not overlap");
var last = await source.QueryAsync(new GridQueryRequest(9995, 100, null, null, null, null), default);
Check(last.Items.Count == 5 && last.TotalCount == 10000, "last partial window preserves total");
var again = await source.QueryAsync(new GridQueryRequest(0, 100, null, null, null, null), default);
Check(first.Items.Select(row => row.Id).SequenceEqual(again.Items.Select(row => row.Id)), "stable ordering after returning to start");
var large = await source.QueryAsync(new GridQueryRequest(0, 250, null, null, null, null), default);
Check(large.Items.Count == 250, "local window size is not limited by REST endpoint cap");
var filter = new GridFilterGroup([new("Category", GridFilterOperator.Equals, first.Items[0].Category)], GridLogicalOperator.And);
var filtered = await source.QueryAsync(new GridQueryRequest(0, 100, null, filter, null, null), default);
Check(filtered.Items.All(row => row.Category == first.Items[0].Category), "source-side filter applies before selecting a window");
var sorted = await source.QueryAsync(new GridQueryRequest(0, 100, [new("OnHand", GridSortDirection.Descending)], null, null, null), default);
Check(sorted.Items.Zip(sorted.Items.Skip(1)).All(pair => pair.First.OnHand >= pair.Second.OnHand), "numeric sorting is executed by SQLite");
var groupRequest = new GridGroupedQueryRequest(0, 100, null, null, [new("WarehouseName"),new("Category")], null, new(), true);
var grouped = await source.QueryGroupedAsync(groupRequest, default);
Check(grouped.TotalItemCount == 10000 && grouped.Rows.Count <= 100 && grouped.TopLevelGroupCount == 4, "group window counts the scoped whole result without reading all records");
var groupId = grouped.Rows.First(row => row.Kind == GridGroupFlatRowKind.GroupHeader).GroupId;
var location = await source.LocateGroupEndAsync(groupId, groupRequest, default);
Check(location.Index.HasValue && location.Index.Value > 0, "group-end location is computed in the local source");
var baseline = await new PhialeTech.Grid.Samples.Wpf.SqliteBinding.Infrastructure.SqliteStockRepository(database).ReadAllAsync(default, 10000);
var completeTestScope = await source.QueryAsync(new GridQueryRequest(0, 10000, null, null, null, null), default);
Check(baseline.Select(row => row.Id).ToHashSet().SetEquals(completeTestScope.Items.Select(row => row.Id)), "02 selects exactly the same 10000-record dataset as 01");
Check(grouped.GroupIds.Count == 20, "scoped set contains all four categories and all four warehouses");
var columns = new StocksViewModel((_,_) => throw new Exception("Unexpected eager creation")).Columns;
using var session = GridDataBuilder.For<StockRow>().Columns(columns).Key(row => row.Id).Clone(row => row with { }).Query(source).Build();
await session.QueryAsync(new GridQueryRequest(500, 100, null, null, null, null), default);
Check(session.TotalCount == 10000 && session.VisibleRecords.Count == 100, "Query builder and protected NuGet session return one window");
Check(!session.EditContext.CanEdit, "no commit port means read-only");
using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
try { await source.QueryAsync(new GridQueryRequest(0, 100, null, null, null, null), cancellation.Token); throw new Exception("Cancellation ignored"); }
catch (OperationCanceledException) { checks++; Console.WriteLine("PASS canceled request"); }
Check(before.SequenceEqual(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(database))), "source database unchanged");
Console.WriteLine($"{checks} checks passed.");
