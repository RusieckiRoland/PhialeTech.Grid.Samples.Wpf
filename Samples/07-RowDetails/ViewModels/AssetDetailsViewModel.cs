using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.ViewModels;

// Values belong to the expanded row, not to the globally selected/current row.
public sealed class AssetDetailsViewModel(AssetRow asset)
{
    public string Id => asset.Id;
    public string Name => asset.Name;
    public string UnitPrice => asset.UnitPrice.ToString("N2") + " zł";
    public string Usage => asset.InService ? "W użyciu" : "Poza użyciem";
    public string Inspection => asset.NextInspection?.ToString("yyyy-MM-dd") ?? "Nie zaplanowano";
    public string Notes => string.IsNullOrWhiteSpace(asset.Notes) ? "Brak uwag" : asset.Notes;
}
