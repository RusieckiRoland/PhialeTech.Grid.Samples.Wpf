using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Composition;
using PhialeGrid.Core.Query;
using System.Diagnostics;
using System.IO;
using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.ViewModels;

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine($"PASS: {name}");
}

var path = Path.Combine(AppContext.BaseDirectory, "Data", "warehouse-1000000.sqlite");
var repository = new CountingRepository(new SqliteStockRepository(path));
var timer = Stopwatch.StartNew();
var rows = await repository.ReadAllAsync(CancellationToken.None);
timer.Stop();
Console.WriteLine($"SQLite read: {timer.Elapsed.TotalMilliseconds:F2} ms; rows={rows.Count}");
Check(rows.Count == 100000, "all 100000 balances are read");
Check(rows.Select(row => row.Id).Distinct().Count() == rows.Count, "warehouse/product identities are unique");
Check(rows.Select(row => row.ProductCode).Distinct().Count() == 25000, "25000 distinct products");
Check(rows.Select(row => row.WarehouseName).Distinct().Count() == 4, "four warehouses");
Check(rows.All(row => row.Available == row.OnHand - row.Reserved), "available quantity is computed from decimal model values");
Check(rows.All(row => !string.IsNullOrWhiteSpace(row.ProductName)), "SQL maps product names");
using var composition = new StockGridComposition();
var model = new StocksViewModel(repository, composition.CreateBinding);
var changed = new List<string?>();
model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
await model.LoadAsync(CancellationToken.None);
Check(model.Rows.Count == 100000 && changed.Contains("Rows") && changed.Contains("Status"), "ViewModel publishes loaded data and status");
Check(model.Columns.Count == 8 && model.Columns.All(column => typeof(StockRow).GetProperty(column.Id) != null), "every builder column maps to an existing model property");
Check(model.Columns.Single(column => column.Id == "OnHand").ValueType == typeof(decimal), "numeric columns retain decimal type");
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
using var cancelledComposition = new StockGridComposition();
var cancelledModel = new StocksViewModel(repository, cancelledComposition.CreateBinding);
await cancelledModel.LoadAsync(cancelled.Token);
Check(cancelledModel.Rows.Count == 0, "cancelled load publishes no rows");
using var missingComposition = new StockGridComposition();
var missingModel = new StocksViewModel(new SqliteStockRepository(path + ".missing"), missingComposition.CreateBinding);
await missingModel.LoadAsync(CancellationToken.None);
Check(missingModel.Status.StartsWith("Could not load") && !File.Exists(path + ".missing"), "missing database is reported and never created");
try
{
    new GridColumnsBuilder<StockRow>().Add(row => row.ProductName.Length, "Invalid", 100);
    throw new Exception("Nested property should be rejected");
}
catch (ArgumentException) { Console.WriteLine("PASS: builder rejects nested property paths"); }
using (var api = global::PhialeGrid.Core.GridApiFactory.CreateRuntime())
{
    api.Workspace.SetCapabilityPolicy(model.Capabilities);
    var states = api.Workspace.ResolveCurrent();
    Check(states.Where(state => state.RegionKind is
        global::PhialeGrid.Core.Regions.GridRegionKind.TopCommandRegion or
        global::PhialeGrid.Core.Regions.GridRegionKind.ChangePanelRegion or
        global::PhialeGrid.Core.Regions.GridRegionKind.ValidationPanelRegion).All(state => !state.IsAvailable),
        "editing command strip, changes and validation are unavailable");
    Check(states.Where(state => state.RegionKind is
        global::PhialeGrid.Core.Regions.GridRegionKind.GroupingRegion or
        global::PhialeGrid.Core.Regions.GridRegionKind.PagingRegion or
        global::PhialeGrid.Core.Regions.GridRegionKind.SummaryBottomRegion or
        global::PhialeGrid.Core.Regions.GridRegionKind.SummaryDesignerRegion).All(state => state.IsAvailable),
        "paging, grouping and summaries remain available on the local snapshot");
    Check(Enum.GetValues<GridEditingCommand>().All(command => !api.Editing.CanExecute(command,
        new GridCommandContext { IsReadOnly = true })), "read-only context rejects all editing commands");
    Check(api.Data.Current?.CanRefresh != true, "no live data session or refresh source is connected");
}
using (var runtime = global::PhialeGrid.Core.GridApiFactory.CreateRuntime())
{
    runtime.Data.Bind(model.DataSession);
    Check(ReferenceEquals(runtime.Data.Binding, model.DataSession), "ViewModel publishes the Grid data binding");
    Check(runtime.Editing.Current?.CanEdit == false, "local session remains read-only");
    var readsAfterLoad = repository.ReadCount;
    runtime.Projection.IsPagingEnabled = true;
    await runtime.Data.ExecuteAsync(runtime.Projection.CreateDataRequest(GridDataRequestKind.Query, 0, 100), CancellationToken.None);
    Check(runtime.Data.Current.TotalCount == 100000 && runtime.Data.Current.VisibleRecords.Count == 100,
        "first page has 100 rows out of 100000");
    var firstId = ((StockRow)runtime.Data.Current.VisibleRecords[0]).Id;
    await runtime.Data.ExecuteAsync(runtime.Projection.CreateDataRequest(GridDataRequestKind.Page, 0, 100, parameter: 1), CancellationToken.None);
    Check(runtime.Data.Current.Paging.Offset == 100 && ((StockRow)runtime.Data.Current.VisibleRecords[0]).Id != firstId,
        "next page contains different records");
    await runtime.Data.ExecuteAsync(runtime.Projection.CreateDataRequest(GridDataRequestKind.PageSize, 0, 100, parameter: 25), CancellationToken.None);
    Check(runtime.Data.Current.VisibleRecords.Count == 25 && runtime.Data.Current.Paging.PageSize == 25,
        "page size can be changed to 25");
    await runtime.Data.ExecuteAsync(new GridDataRequest { Kind = GridDataRequestKind.ChangeQuery, Paging = true,
        Query = new GridQueryRequest(0, 25, null, new GridFilterGroup(new[] {
            new GridFilterDescriptor("ProductCode", GridFilterOperator.Contains, "KAR-00001")
        }, GridLogicalOperator.And), null, null) }, CancellationToken.None);
    Check(runtime.Data.Current.TotalCount == rows.Count(row => row.ProductCode.Contains("KAR-00001")) && runtime.Data.Current.VisibleRecords.Count == Math.Min(25, rows.Count(row => row.ProductCode.Contains("KAR-00001"))),
        "filter applies to the complete snapshot, not just the current page");
    runtime.Workspace.Execute(new global::PhialeGrid.Contracts.GridWorkspaceRequest(
        global::PhialeGrid.Core.Interaction.GridRegionCommandKind.Close,
        global::PhialeGrid.Core.Regions.GridRegionKind.PagingRegion));
    await runtime.Data.ExecuteAsync(runtime.Projection.CreateDataRequest(GridDataRequestKind.ChangeQuery, 25, 25), CancellationToken.None);
    Check(runtime.Data.Current.VisibleRecords.Count == 100000 && !runtime.Projection.IsPagingEnabled,
        "turning paging off exposes the complete 100000-row snapshot");
    runtime.Workspace.Execute(new global::PhialeGrid.Contracts.GridWorkspaceRequest(
        global::PhialeGrid.Core.Interaction.GridRegionCommandKind.Open,
        global::PhialeGrid.Core.Regions.GridRegionKind.PagingRegion));
    await runtime.Data.ExecuteAsync(runtime.Projection.CreateDataRequest(GridDataRequestKind.ChangeQuery, 0, runtime.Projection.PageSize), CancellationToken.None);
    Check(runtime.Data.Current.VisibleRecords.Count == 25 && runtime.Projection.IsPagingEnabled,
        "turning paging on restores the configured 25-row page");
    Check(repository.ReadCount == readsAfterLoad, "paging and filtering perform no additional SQLite reads");
    Check(runtime.Editing.Current?.CanEdit == false, "paging does not enable editing");
}
foreach (var size in new[] { 10000, 100000, 1000000 })
{
    var measured = Stopwatch.StartNew();
    var subset = await repository.ReadAllAsync(CancellationToken.None, size);
    Check(subset.Count == size, $"SQL limit {size}");
    Check(subset.Select(row => row.Category).Distinct().Count() == 4, $"all four categories in {size}");
    Check(subset.Select(row => row.WarehouseName).Distinct().Count() == 4, $"all four warehouses in {size}");
    foreach (var category in subset.GroupBy(row => row.Category)) Console.WriteLine($"  {category.Key}: {category.Count():N0}");
    Check(subset.All(row => row.Reserved >= 0 && row.OnHand >= row.Reserved && !string.IsNullOrWhiteSpace(row.Category)), "valid quantities and categories");
    Console.WriteLine($"Read {size}: {measured.Elapsed.TotalSeconds:F3} s");
}
model.SelectedSize = 10000;
await model.LoadAsync(CancellationToken.None);
Check(model.Rows.Count == 10000 && model.IsIdle && model.DataSession != null, "snapshot can be replaced");
Console.WriteLine("All checks passed.");

sealed class CountingRepository(IStockRepository inner) : IStockRepository
{
    public int ReadCount { get; private set; }
    public Task<IReadOnlyList<StockRow>> ReadAllAsync(CancellationToken token, int limit = 100000)
    {
        ReadCount++;
        return inner.ReadAllAsync(token, limit);
    }
}
