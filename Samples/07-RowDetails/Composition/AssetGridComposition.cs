using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Data;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.RowDetails.Configuration;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.Composition;
public sealed class AssetGridComposition : IDisposable
{
    private readonly GridDataSession<AssetRow> _session;
    public AssetGridComposition(IReadOnlyList<AssetRow> rows)
    {
        _session = GridDataBuilder.For<AssetRow>()
            .Columns(AssetColumns.All).Key(row => row.Id).Clone(row => row with { })
            .Records(rows).Build();
        Binding = GridApiFactory.CreateDataBinding(_session);
    }
    public GridDataBinding Binding { get; }
    public void Dispose() => _session.Dispose();
}
