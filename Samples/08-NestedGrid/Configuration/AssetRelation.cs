using PhialeGrid.Core.Interaction;
using PhialeGrid.Contracts;
using PhialeGrid.Core.Details;
using PhialeGrid.Core.Regions;
using PhialeGrid.Core.Validation;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Composition;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;

namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Configuration;

public static class AssetRelation
{
    public static GridDetailRelationDefinition<LocationRow, AssetRow> Create(
        LocationGridComposition source, Action<Action> postToUi) =>
        GridDetailRelation.For<LocationRow, AssetRow>("location-assets")
            .ParentKey(location => location.Id)
            .ChildKey(asset => asset.Id)
            // Each parent receives its own editable session and its own change buffer.
            .Source((parent, token) => Task.FromResult(source.OpenAssets(parent, token)))
            .Columns(columns => columns
                .Add(asset => asset.Id, "Kod", 105) // Keys and parent membership stay read-only.
                .Add(asset => asset.Name, "Wyposażenie", 240, editable: true,
                    validation: new TextValidationConstraints(required: true, maxLength: 80))
                .Add(asset => asset.Category, "Kategoria", 140)
                .Add(asset => asset.Quantity, "Ilość", 90, editable: true,
                    validation: new IntegerValidationConstraints(minValue: 0, maxValue: 500))
                .Add(asset => asset.UnitPrice, "Cena jedn.", 115, editable: true,
                    validation: new DecimalValidationConstraints(minValue: 0, maxValue: 100000, scale: 2)))
            .Configure(api => {
                // Child chrome starts minimal. Explicitly show its own standard edit/save bar.
                api.Workspace.Execute(new(GridRegionCommandKind.Open, GridRegionKind.TopCommandRegion));
            })
            .Dispatcher(postToUi)
            .Height(340)
            .Build();
}
