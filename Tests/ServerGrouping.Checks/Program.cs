using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.HttpRest.Server;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

var database = GroupingServer.FindDatabase();
byte[] Hash() { using var file = File.OpenRead(database); return SHA256.HashData(file); }
var before = Hash();
var commands = new ConcurrentQueue<string>();
await using var app = GroupingServer.Create("http://127.0.0.1:0", database, commands.Enqueue);
await app.StartAsync();
try {
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
    int checks = 0;
    void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    StockOrder[] Groups() => [new() { Field = "warehouseName" }, new() { Field = "category" }];
    GroupRequest Request(int skip = 0, int take = 5, string[]? collapsed = null) =>
        new() { Query = new() { Skip = skip, Take = take }, GroupBy = Groups(), Collapsed = collapsed ?? [] };
    async Task<GroupPage> Read(GroupRequest request) {
        using var response = await http.PostAsJsonAsync(address + "/api/stocks/query", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroupPage>())!;
    }
    async Task<int> Locate(string id, GroupRequest request) {
        using var response = await http.PostAsJsonAsync(address + "/api/stocks/locate", new LocateRequest("groupend:" + id, request));
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<LocatedRow>())!.Index!.Value;
    }
    Totals SqlTotals(GroupKey[]? path = null) {
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Mode = SqliteOpenMode.ReadOnly }.ToString());
        db.Open(); using var command = db.CreateCommand();
        command.CommandText = "SELECT COUNT(*), SUM(CAST(ROUND(CAST(b.on_hand AS REAL)*100) AS INTEGER)), SUM(CAST(ROUND(CAST(b.reserved AS REAL)*100) AS INTEGER)) FROM stock_balances b JOIN products p ON p.id=b.product_id JOIN warehouses w ON w.id=b.warehouse_id";
        var predicates = new List<string>();
        foreach (var key in path ?? []) {
            var parameter = "$p" + predicates.Count;
            predicates.Add((key.Field == "warehouseName" ? "w.name" : "p.category") + "=" + parameter);
            command.Parameters.AddWithValue(parameter, key.Value);
        }
        if (predicates.Count != 0) command.CommandText += " WHERE " + string.Join(" AND ", predicates);
        using var row = command.ExecuteReader(); row.Read();
        return new(row.GetInt32(0), row.IsDBNull(1) ? 0 : row.GetInt64(1), row.IsDBNull(2) ? 0 : row.GetInt64(2));
    }

    var initial = await Read(Request());
    Check(initial.TotalItems == 1_000_000 && initial.RootCount == 4, "million-row result and four warehouse roots");
    Check(initial.Rows.Length == 5 && initial.Rows[0].Kind == "group" && initial.Rows[1].Field == "category", "first page contains hierarchy and only requested records");
    Check(initial.Totals == SqlTotals(), "full-result count and sums match independent SQL exactly");
    Check(commands.Any(c => c.Contains("GROUP BY")) && commands.Any(c => c.Contains("LIMIT @") && c.Contains("OFFSET @")), "SQLite executes grouping, aggregates and paged data queries");
    var allIds = initial.GroupIds;
    commands.Clear();
    var collapsed = await Read(Request(collapsed: allIds));
    Check(collapsed.VisibleCount == 4 && collapsed.Rows.All(r => r.Kind == "group" && !r.Expanded), "collapsed groups return only four headers");
    Check(commands.Count == 1 && commands.All(c => c.Contains("GROUP BY")), "collapsed request fetches aggregate metadata, no data records");
    Check(collapsed.Rows.Sum(r => r.Count) == 1_000_000, "collapsed group counts still cover every record");
    var rootIds = collapsed.Rows.Select(r => r.Id!).ToArray();
    var openFirst = Request(collapsed: rootIds.Skip(1).ToArray());
    var first = await Read(openFirst);
    Check(first.VisibleCount > first.Rows.Length && first.Rows.Count(r => r.Kind == "record") == 3, "expanding a root fetches only the visible page");
    var child = first.Rows[1];
    var footerIndex = await Locate(child.Id!, openFirst);
    var footerRequest = Request(footerIndex, collapsed: openFirst.Collapsed);
    var footer = (await Read(footerRequest)).Rows[0];
    Check(footer.Kind == "summary" && footer.Path!.Length == 2, "group footer is reachable beyond the loaded page and carries its complete path");
    Check(footer.Totals == SqlTotals(child.Path), "group footer sums every member, not just page records");
    Check(footer.Totals!.Count > 100, "summary covers more than one page");
    var boundary = await Read(Request(footerIndex - 1, 4, openFirst.Collapsed));
    Check(boundary.Rows[0].Kind == "record" && boundary.Rows[1].Kind == "summary" && boundary.Rows[2].Kind == "group", "paging correctly crosses data, footer and next header");
    var rootFooterIndex = await Locate(first.Rows[0].Id!, openFirst);
    var rootFooter = (await Read(Request(rootFooterIndex, 1, openFirst.Collapsed))).Rows.Single();
    Check(rootFooter.Totals == SqlTotals(first.Rows[0].Path), "parent summary covers the complete warehouse");

    var lastRootRequest = Request(5, 1, rootIds.Take(3).ToArray());
    var lastRootRow = (await Read(lastRootRequest)).Rows.Single();
    Check(lastRootRow.Kind == "record" && lastRootRow.GlobalNumber == collapsed.Rows.Take(3).Sum(r => r.Count) + 1 && lastRootRow.GroupNumber == 1,
        "global numbering includes collapsed records while group numbering restarts");
    var reverse = Request(); reverse.GroupBy = [new() { Field = "category", Direction = "desc" }, new() { Field = "warehouseName" }];
    var reversed = await Read(reverse);
    Check(reversed.Rows[0].Field == "category" && reversed.Rows[1].Field == "warehouseName", "reversed grouping order and direction");
    var filtered = Request();
    filtered.Query.Conditions = [new() { Field = "category", Comparison = "eq", Value = System.Text.Json.JsonSerializer.SerializeToElement("Packaging") }];
    var filteredPage = await Read(filtered);
    Check(filteredPage.Totals == SqlTotals([new("category", "Packaging")]), "filters apply before grouping and totals");
    var empty = Request(); empty.Query.Conditions = [new() { Field = "category", Comparison = "eq", Value = System.Text.Json.JsonSerializer.SerializeToElement("missing") }];
    var emptyPage = await Read(empty);
    Check(emptyPage.TotalItems == 0 && emptyPage.Rows.Length == 0 && emptyPage.Totals.OnHand == 0, "empty filtered result");
    var flat = Request(); flat.GroupBy = [];
    var flatPage = await Read(flat);
    Check(flatPage.Rows.Length == 5 && flatPage.Rows.All(r => r.Kind == "record") && flatPage.VisibleCount == 1_000_000, "removing grouping retains server paging");
    var noSummary = Request(collapsed: openFirst.Collapsed); noSummary.ShowSummaries = false;
    var lastData = await Locate(child.Id!, noSummary); noSummary.Query.Skip = lastData; noSummary.Query.Take = 1;
    Check((await Read(noSummary)).Rows.Single().Kind == "record", "group-end navigation without summaries resolves the last record");
    var bad = Request(); bad.GroupBy = [new() { Field = "productCode" }];
    using var invalid = await http.PostAsJsonAsync(address + "/api/stocks/query", bad);
    Check(invalid.StatusCode == HttpStatusCode.BadRequest, "unsupported high-cardinality grouping is explicitly rejected");

    var adapter = new HttpGridDataAdapter<StockRow>(http, GroupedStocksEndpoint.Options(address), (_, value) => value.ToString());
    var expansion = new GridGroupExpansionState(); foreach (var id in rootIds.Skip(1)) expansion.SetExpanded(id, false);
    var descriptors = new[] { new GridGroupDescriptor("WarehouseName"), new GridGroupDescriptor("Category") };
    var summaries = new[] { new GridSummaryDescriptor("ProductCode", GridSummaryType.Count), new GridSummaryDescriptor("OnHand", GridSummaryType.Sum), new GridSummaryDescriptor("Reserved", GridSummaryType.Sum), new GridSummaryDescriptor("Available", GridSummaryType.Sum) };
    GridGroupedQueryRequest GridRequest(int offset) => new(offset, 5, [], GridFilterGroup.EmptyAnd(), descriptors, summaries, expansion, true);
    var mapped = await adapter.QueryGroupedAsync(GridRequest(footerIndex), default);
    Check(mapped.Rows[0].Kind == GridGroupFlatRowKind.GroupSummary && mapped.Rows[0].GroupPath.Count == 2 && mapped.Rows[0].SummaryCells.Count == 4,
        "published HTTP adapter maps group footer, path and four summary cells");
    Check(mapped.Summary.GetValue<int>("ProductCode:Count") == 1_000_000, "adapter maps total summaries");
    var located = await adapter.LocateRecordAsync("groupend:" + child.Id, GridRequest(0), default);
    Check(located.Index == footerIndex, "published adapter maps group-end location");
    Check(!typeof(GroupingServer).Assembly.GetReferencedAssemblies().Any(a => a.Name!.StartsWith("Phiale") || a.Name.StartsWith("UniversalInput")), "server has no Grid or other Phiale dependency");
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    try { await adapter.QueryGroupedAsync(GridRequest(0), cancellation.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { Check(true, "request cancellation"); }
    // Exercise the public Core runtime, not only transport DTOs.
    var columns = new[] { new GridColumnDefinition("ProductCode", "Symbol"), new GridColumnDefinition("WarehouseName", "Warehouse"),
        new GridColumnDefinition("Category", "Category"), new GridColumnDefinition("OnHand", "On hand", valueType: typeof(decimal)) };
    var source = new GridDataSource<StockRow>(columns, row => row.Id, row => row with { });
    using var session = source.CreateSession(new GroupedStockProvider(http, address));
    using var api = GridApiFactory.CreateRuntime();
    api.Projection.ConfigureColumns(columns); api.Data.Bind(GridApiFactory.CreateDataBinding(session));
    api.Projection.Groups = descriptors; api.Projection.Summaries = summaries;
    api.Projection.ShowGroupSummaries = true; api.Projection.PageSize = 5; api.Projection.IsPagingEnabled = true;
    foreach (var id in rootIds) api.Query.GroupExpansion.SetExpanded(id, false);
    void Paint() {
        var view = api.Projection.ProjectCurrent();
        var surface = api.Projection.ProjectSurface(api.Projection.GetSurfaceRows(view), 28, 100, true, view.LogicalPositions, view.VisibleCount, view.Summary);
        api.Configuration.Initialize(surface.Columns, surface.Rows);
    }
    api.Data.StateChanged += (_, _) => { if (!api.Data.Current.IsBusy && api.Data.Current.HasConfirmedQuery) Paint(); };
    await api.Data.ExecuteAsync(api.Projection.CreateDataRequest(GridDataRequestKind.Query, 0, 5), default); Paint();
    var contextKey = "group:" + rootIds[0];
    Check(api.Navigation.GetContextGroupId(contextKey) == rootIds[0], "Core recognizes the server's opaque group identity");
    Check(await api.Navigation.NavigateToGroupEndAsync(contextKey), "Core group-end command loads and scrolls to the remote footer");
    var projected = api.Projection.ProjectCurrent();
    Check(api.Projection.GetSurfaceRows(projected).Any(row => row.Group?.Kind == GridQueryRowKind.GroupSummary && row.Group.GroupId == rootIds[0]),
        "Core projection contains the requested warehouse footer");
    Check(before.SequenceEqual(Hash()), "existing database remains unchanged");
    Console.WriteLine($"PASS {checks} server-grouping integration checks");
} finally { await app.StopAsync(); }
