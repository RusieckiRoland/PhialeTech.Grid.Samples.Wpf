namespace PhialeTech.Grid.Samples.HttpRest.Server;

internal sealed class StockReadRow
{
    public string Id { get; init; } = "";
    public string ProductId { get; init; } = "";
    public string WarehouseId { get; init; } = "";
    public string ProductCode { get; init; } = "";
    public string ProductName { get; init; } = "";
    public string Category { get; init; } = "";
    public string UnitCode { get; init; } = "";
    public string WarehouseName { get; init; } = "";
    public string OnHandText { get; init; } = "";
    public string ReservedText { get; init; } = "";
    public double OnHand { get; init; }
    public double Reserved { get; init; }
    public double Available { get; init; }
}
