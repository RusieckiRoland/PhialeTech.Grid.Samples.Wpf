using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.Configuration;
public static class AssetColumns
{
    public static IReadOnlyList<IGridColumnDescriptor> All { get; } = new GridColumnsBuilder<AssetRow>()
        .Add(row => row.Id, "Kod", 120).Add(row => row.Name, "Wyposażenie", 320)
        .Add(row => row.Category, "Kategoria", 160).Add(row => row.Location, "Lokalizacja", 160).Build();
}
