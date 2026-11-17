using PhialeGrid.Core.Details;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;
namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Configuration;

// Describes the relationship. Core owns each child runtime; WPF only displays it.
public static class AssetRelation
{
    public static GridDetailRelationDefinition<LocationRow, AssetRow> Create(Action<Action> postToUi) =>
        GridDetailRelation.For<LocationRow, AssetRow>("location-assets")
            .ParentKey(location => location.Id)
            .ChildKey(asset => asset.Id)
            .Local(location => location.Assets)
            .Columns(columns => columns
                .Add(asset => asset.Id, "Kod", 120)
                .Add(asset => asset.Name, "Wyposażenie", 290)
                .Add(asset => asset.Category, "Kategoria", 140)
                .Add(asset => asset.Quantity, "Ilość", 85)
                .Add(asset => asset.UnitPrice, "Cena jedn.", 110))
            .Dispatcher(postToUi)
            .Height(300)
            .Build();
}
