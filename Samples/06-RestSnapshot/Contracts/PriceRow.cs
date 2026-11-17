namespace PhialeTech.Grid.Samples.PriceReview.Contracts;

// A detached working copy. Version is the database version read with this copy.
public sealed record PriceRow
{
    public string Id { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal OldPurchasePrice { get; set; }
    public decimal NewPurchasePrice { get; set; }
    public decimal ProposedSalePrice { get; set; }
    public DateTime ValidFrom { get; set; }
    public string Decision { get; set; } = "Oczekuje";
    public string Comment { get; set; } = "";
    public int Version { get; set; }
    public decimal ChangePercent => OldPurchasePrice == 0 ? 0 : decimal.Round((NewPurchasePrice / OldPurchasePrice - 1) * 100, 2);
    public decimal MarginPercent => ProposedSalePrice <= 0 ? 0 : decimal.Round((1 - NewPurchasePrice / ProposedSalePrice) * 100, 2);
}
