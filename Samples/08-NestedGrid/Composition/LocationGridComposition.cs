using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Data;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels;
namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Composition;
public sealed class LocationGridComposition : IDisposable
{
    private readonly GridDataSession<LocationRow> _session;
    public LocationGridComposition(IReadOnlyList<AssetRow> assets)
    {
        // Group once. Each parent owns a bounded list; opening a row does not scan the database.
        var locations = assets.GroupBy(asset => asset.Location).OrderBy(group => group.Key)
            .Select(group => new LocationRow(group.Key, group.ToArray())).ToArray();
        _session = GridDataBuilder.For<LocationRow>()
            .Columns(LocationsViewModel.ColumnDefinitions).Key(row => row.Id).Clone(row => row with { })
            .Records(locations).Build();
        Binding = GridApiFactory.CreateDataBinding(_session);
    }
    public GridDataBinding Binding { get; }
    public void Dispose() => _session.Dispose();
}
