namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

// Mutable working copies belong to the Grid session; SQLite remains unchanged until Save.
public sealed record AssetRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool InService { get; set; }
    public DateTime? NextInspection { get; set; }
    public string Location { get; set; } = "";
    public string Notes { get; set; } = "";
    public int Version { get; set; }
}
