using System.Text.Json;
using System.Text.Json.Serialization;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.HttpRest.Infrastructure;

// The server has its own API. These two functions translate at the client boundary.
public static class StocksEndpoint
{
    public static GridHttpDataOptions<StockRow> Options(string address) => new(address, "api/stocks/search")
    {
        MapQueryRequest = CreateServerRequest,
        ReadQueryResponse = ReadServerResponse
    };

    private static StockSearchRequest CreateServerRequest(GridQueryRequest query) => new()
    {
        // The server calls paging "skip/take"; Grid calls it "Offset/Size".
        Skip = query.Offset,
        Take = query.Size,
        // Column ids and operators also have to use the server's vocabulary.
        OrderBy = query.Sorts.Select(sort => new StockSearchOrder(
            Field(sort.ColumnId),
            sort.Direction == GridSortDirection.Descending ? "desc" : "asc")).ToArray(),
        Match = query.FilterGroup.LogicalOperator == GridLogicalOperator.And ? "all" : "any",
        Conditions = query.FilterGroup.Filters.Select(filter => new StockSearchCondition(
            Field(filter.ColumnId), Comparison(filter.Operator), filter.Value, filter.SecondValue)).ToArray()
    };

    private static GridQueryResult<StockRow> ReadServerResponse(string json, JsonSerializerOptions options)
    {
        var page = JsonSerializer.Deserialize<StocksPage>(json, options)
            ?? throw new JsonException("Missing stocks page.");

        // Stocks contains only this page. Total counts ALL matching records.
        return new GridQueryResult<StockRow>(page.Stocks, page.Total, null, GridSummarySet.Empty);
    }

    private static string Field(string column) => column switch {
        nameof(StockRow.Id) => "id", nameof(StockRow.ProductCode) => "productCode",
        nameof(StockRow.ProductName) => "productName", nameof(StockRow.Category) => "category",
        nameof(StockRow.WarehouseName) => "warehouseName", nameof(StockRow.UnitCode) => "unitCode",
        nameof(StockRow.OnHand) => "onHand", nameof(StockRow.Reserved) => "reserved",
        nameof(StockRow.Available) => "available",
        _ => throw new ArgumentException("Unsupported column: " + column)
    };

    private static string Comparison(GridFilterOperator op) => op switch {
        GridFilterOperator.Equals => "eq", GridFilterOperator.Contains => "contains",
        GridFilterOperator.StartsWith => "startsWith", GridFilterOperator.GreaterThan => "gt",
        GridFilterOperator.LessThan => "lt", GridFilterOperator.Between => "between",
        _ => throw new ArgumentException("Unsupported stock filter: " + op)
    };
}

// These client DTOs describe JSON accepted by this API, not a universal REST format.
// Explicit JSON names keep the wire contract independent of C# naming conventions.
public sealed class StockSearchRequest
{
    [JsonPropertyName("skip")] public int Skip { get; init; }
    [JsonPropertyName("take")] public int Take { get; init; }
    [JsonPropertyName("orderBy")] public StockSearchOrder[] OrderBy { get; init; } = [];
    [JsonPropertyName("match")] public string Match { get; init; } = "all";
    [JsonPropertyName("conditions")] public StockSearchCondition[] Conditions { get; init; } = [];
}

public sealed record StockSearchOrder(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("direction")] string Direction);

public sealed record StockSearchCondition(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("comparison")] string Comparison,
    [property: JsonPropertyName("value")] object? Value,
    [property: JsonPropertyName("upper")] object? Upper);

public sealed record StocksPage(StockRow[] Stocks, int Total);
