using System.Text.Json;
using PhialeGrid.Contracts;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.ServerGrouping.Infrastructure;

public static class GroupedStocksEndpoint
{
    public static GridHttpDataOptions<StockRow> Options(string address) => new(address, "api/stocks/query") {
        GroupedQueryPath = "api/stocks/query", RecordLocationPath = "api/stocks/locate",
        MapQueryRequest = request => Request(request.Offset, request.Size, request.Sorts, request.FilterGroup, [], [], false),
        MapGroupedQueryRequest = request => Request(request.Offset, request.Size, request.Sorts, request.FilterGroup,
            request.Groups, request.ExpansionState.CollapsedGroupIds, request.ShowGroupSummaries),
        MapRecordLocationRequest = (target, request) => new { target, request = Request(0, request.Size,
            request.Sorts, request.FilterGroup, request.Groups, request.ExpansionState.CollapsedGroupIds, request.ShowGroupSummaries) },
        ReadRecordLocationResponse = (json, options) => new GridRecordLocation(JsonSerializer.Deserialize<Location>(json, options)!.Index),
        ReadQueryResponse = (json, options) => {
            var page = Read(json, options);
            return new GridQueryResult<StockRow>(page.Rows.Select(row => row.Item!).ToArray(), page.TotalItems, null, Summary(page.Totals));
        },
        ReadGroupedQueryResponse = (json, options) => {
            var page = Read(json, options);
            var rows = page.Rows.Select(row => row.Kind switch {
                "group" => GridGroupFlatRow<StockRow>.CreateGroupHeader(row.Id!, Column(row.Field!), row.Key!, row.Count, row.Level, row.Expanded),
                "record" => GridGroupFlatRow<StockRow>.CreateDataRow(row.Item!, row.Level, row.GlobalNumber, row.GroupNumber),
                "summary" => GridGroupFlatRow<StockRow>.CreateSummary(row.Id!, row.Level, 0, Cells(row.Totals!), 1,
                    row.Path!.Select(key => new GridGroupPathSegment(Column(key.Field), key.Value)).ToArray()),
                _ => throw new JsonException("Unknown group row kind.")
            }).ToArray();
            return new GridGroupedQueryResult<StockRow>(rows, page.VisibleCount, page.TotalItems, page.RootCount, page.GroupIds, Summary(page.Totals));
        }
    };

    private static object Request(int skip, int take, IReadOnlyList<GridSortDescriptor> sorts, GridFilterGroup filter,
        IReadOnlyList<GridGroupDescriptor> groups, IEnumerable<string> collapsed, bool showSummaries) => new {
        query = new { skip, take, orderBy = sorts.Select(sort => new { field = Field(sort.ColumnId), direction = Direction(sort.Direction) }).ToArray(),
            match = filter.LogicalOperator == GridLogicalOperator.And ? "all" : "any",
            conditions = filter.Filters.Select(condition => new { field = Field(condition.ColumnId), comparison = Comparison(condition.Operator),
                value = condition.Value, upper = condition.SecondValue }).ToArray() },
        groupBy = groups.Select(group => new { field = Field(group.ColumnId), direction = Direction(group.Direction) }).ToArray(),
        collapsed = collapsed.ToArray(), showSummaries
    };
    private static string Direction(GridSortDirection value) => value == GridSortDirection.Descending ? "desc" : "asc";
    private static string Field(string column) => column switch {
        "Id" => "id", "ProductCode" => "productCode", "ProductName" => "productName", "Category" => "category",
        "WarehouseName" => "warehouseName", "UnitCode" => "unitCode", "OnHand" => "onHand", "Reserved" => "reserved", "Available" => "available",
        _ => throw new ArgumentException("Unknown column: " + column)
    };
    private static string Column(string field) => field switch {
        "warehouseName" => "WarehouseName", "category" => "Category", _ => throw new JsonException("Unsupported grouping field.")
    };
    private static string Comparison(GridFilterOperator op) => op switch {
        GridFilterOperator.Equals => "eq", GridFilterOperator.Contains => "contains", GridFilterOperator.StartsWith => "startsWith",
        GridFilterOperator.GreaterThan => "gt", GridFilterOperator.LessThan => "lt", GridFilterOperator.Between => "between",
        _ => throw new ArgumentException("Unsupported filter.")
    };
    private static Page Read(string json, JsonSerializerOptions options) => JsonSerializer.Deserialize<Page>(json, options)
        ?? throw new JsonException("Missing grouped page.");
    private static GridGroupSummaryCell[] Cells(Totals total) => [new("ProductCode", GridSummaryType.Count, total.Count),
        new("OnHand", GridSummaryType.Sum, total.OnHand), new("Reserved", GridSummaryType.Sum, total.Reserved),
        new("Available", GridSummaryType.Sum, total.Available)];
    private static GridSummarySet Summary(Totals total) => new(Cells(total).ToDictionary(c => c.ColumnId + ":" + c.Type, c => c.Value));

    public sealed record Totals(int Count, decimal OnHand, decimal Reserved, decimal Available);
    public sealed record Key(string Field, string Value);
    public sealed record Row(string Kind, int Level, string? Id, string? Field, string? Key, int Count, bool Expanded,
        StockRow? Item, Totals? Totals, Key[]? Path, int GlobalNumber, int GroupNumber);
    public sealed record Page(Row[] Rows, int VisibleCount, int TotalItems, int RootCount, string[] GroupIds, Totals Totals);
    private sealed record Location(int? Index);
}
