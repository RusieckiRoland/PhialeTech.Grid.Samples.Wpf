namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

/// <summary>One product balance in one warehouse. No SQL, UI or Grid dependencies.</summary>
public sealed record StockRow(
    string Id,
    string ProductCode,
    string ProductName,
    string UnitCode,
    string WarehouseName,
    decimal OnHand,
    decimal Reserved)
{
    public decimal Available => OnHand - Reserved;
}
