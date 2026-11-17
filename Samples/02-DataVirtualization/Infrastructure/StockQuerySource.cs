// Grid calls QueryAsync for an ordinary window and QueryGroupedAsync for a grouped one.
// Translate requests explicitly; SQLite, rather than an in-memory list, evaluates them.
using System.Text.Json;
using PhialeGrid.Contracts;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeTech.Grid.Samples.HttpRest.Server;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;
namespace PhialeTech.Grid.Samples.Wpf.DataVirtualization.Infrastructure;

public sealed class StockQuerySource(StockRepository repository, int recordLimit) :
    IGridQueryDataProvider<StockRow>, IGridGroupedQueryDataProvider<StockRow>, IGridGroupLocationProvider
{
    public async Task<GridQueryResult<StockRow>> QueryAsync(GridQueryRequest request, CancellationToken token)
    {
        var query = Query(request.Offset, request.Size, request.Sorts, request.FilterGroup);
        var page = await repository.ReadAsync(query, recordLimit, token);
        return new GridQueryResult<StockRow>(page.Stocks, page.Total, null, GridSummarySet.Empty);
    }
    public async Task<GridGroupedQueryResult<StockRow>> QueryGroupedAsync(GridGroupedQueryRequest request, CancellationToken token)
    {
        var page = await repository.ReadGroupedAsync(GroupQuery(request), recordLimit, token);
        var rows = page.Rows.Select(row => row.Kind switch {
            "group" => GridGroupFlatRow<StockRow>.CreateGroupHeader(row.Id!, Column(row.Field!), row.Key!, row.Count, row.Level, row.Expanded),
            "record" => GridGroupFlatRow<StockRow>.CreateDataRow(row.Item!, row.Level, row.GlobalNumber, row.GroupNumber),
            "summary" => GridGroupFlatRow<StockRow>.CreateSummary(row.Id!, row.Level, 0, Cells(row.Totals!), 1,
                row.Path!.Select(key => new GridGroupPathSegment(Column(key.Field), key.Value)).ToArray()),
            _ => throw new InvalidOperationException("Unknown group row kind.")
        }).ToArray();
        var summary = new GridSummarySet(Cells(page.Totals).ToDictionary(cell => cell.ColumnId + ":" + cell.Type, cell => cell.Value));
        return new(rows, page.VisibleCount, page.TotalItems, page.RootCount, page.GroupIds, summary);
    }
    public async Task<GridRecordLocation> LocateGroupEndAsync(string groupId, GridGroupedQueryRequest request, CancellationToken token)
    {
        var location = await repository.LocateAsync(new LocateRequest("groupend:" + groupId, GroupQuery(request)), recordLimit, token);
        return new GridRecordLocation(location.Index);
    }
    private static GroupRequest GroupQuery(GridGroupedQueryRequest request) => new() {
        Query = Query(request.Offset, request.Size, request.Sorts, request.FilterGroup),
        GroupBy = request.Groups.Select(group => new StockOrder { Field = Field(group.ColumnId), Direction = Direction(group.Direction) }).ToArray(),
        Collapsed = request.ExpansionState.CollapsedGroupIds.ToArray(), ShowSummaries = request.ShowGroupSummaries
    };
    private static StockQuery Query(int offset, int size, IReadOnlyList<GridSortDescriptor> sorts, GridFilterGroup filters) => new() {
        Skip = offset, Take = size,
        OrderBy = sorts.Select(sort => new StockOrder { Field = Field(sort.ColumnId), Direction = Direction(sort.Direction) }).ToArray(),
        Match = filters.LogicalOperator == GridLogicalOperator.And ? "all" : "any",
        Conditions = filters.Filters.Select(filter => new StockCondition { Field = Field(filter.ColumnId), Comparison = Comparison(filter.Operator),
            Value = JsonSerializer.SerializeToElement(filter.Value), Upper = JsonSerializer.SerializeToElement(filter.SecondValue) }).ToArray()
    };
    private static string Direction(GridSortDirection direction) => direction == GridSortDirection.Descending ? "desc" : "asc";
    private static string Field(string column) => column switch {
        "Id" => "id", "ProductCode" => "productCode", "ProductName" => "productName", "Category" => "category",
        "WarehouseName" => "warehouseName", "UnitCode" => "unitCode", "OnHand" => "onHand", "Reserved" => "reserved", "Available" => "available",
        _ => throw new ArgumentException("Unknown column: " + column)
    };
    private static string Column(string field) => field switch { "warehouseName" => "WarehouseName", "category" => "Category",
        _ => throw new ArgumentException("Group by WarehouseName or Category in this example.") };
    private static string Comparison(GridFilterOperator op) => op switch {
        GridFilterOperator.Equals => "eq", GridFilterOperator.Contains => "contains", GridFilterOperator.StartsWith => "startsWith",
        GridFilterOperator.GreaterThan => "gt", GridFilterOperator.LessThan => "lt", GridFilterOperator.Between => "between",
        _ => throw new ArgumentException("Unsupported filter.")
    };
    private static GridGroupSummaryCell[] Cells(Totals total) => [new("ProductCode", GridSummaryType.Count, total.Count),
        new("OnHand", GridSummaryType.Sum, total.OnHand), new("Reserved", GridSummaryType.Sum, total.Reserved), new("Available", GridSummaryType.Sum, total.Available)];
}
