using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Details;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels;

namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Composition;

// The application owns all sessions until the window closes, including collapsed details.
// Keeping a session per stable parent key preserves its buffer across collapse/reopen.
public sealed class LocationGridComposition : IDisposable
{
    private readonly LocationRepository _repository;
    private readonly GridDataSession<LocationRow> _master;
    private readonly Dictionary<string, GridDataSession<AssetRow>> _details = new();

    public LocationGridComposition(LocationRepository repository)
    {
        _repository = repository;
        _master = GridDataBuilder.For<LocationRow>()
            .Columns(LocationsViewModel.ColumnDefinitions)
            .Key(row => row.Id)
            .Clone(row => row with { }) // LocationRow is a record of scalar values.
            .Records(repository.ReadLocations())
            .CommitUsing(new LocationCommitPort(repository)) // Makes master edits persistable.
            .Build();
        Binding = GridApiFactory.CreateDataBinding(_master);
    }

    public GridDataBinding Binding { get; }
    public string DatabasePath => _repository.DatabasePath;
    public bool HasPendingChanges => _master.EditContext.HasPendingEdits
        || _details.Values.Any(session => session.EditContext.HasPendingEdits);

    public bool HasActiveOperations => _master.EditContext.IsInEditMode || _master.EditContext.IsCommitting
        || _details.Values.Any(session => session.IsLoading || session.EditContext.IsInEditMode || session.EditContext.IsCommitting);

    public GridDetailGridSource<AssetRow> OpenAssets(LocationRow parent, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!_details.TryGetValue(parent.Id, out var session))
        {
            // Small local data set: load this parent's records once, then edit working copies.
            var rows = _repository.Assets.ReadAll().Where(asset => asset.Location == parent.Id).ToArray();
            session = GridDataBuilder.For<AssetRow>()
                // Reuse lesson 05's validation for the five fields shown in this detail.
                .Columns(AssetColumns.All.Where(column => column.Id is
                    "Id" or "Name" or "Category" or "Quantity" or "UnitPrice").ToArray())
                .Key(row => row.Id)
                .Clone(row => row with { })
                .Records(rows)
                .CommitUsing(new SqliteCommitPort(_repository.Assets, rows))
                .Build();
            _details.Add(parent.Id, session);
        }
        // The source borrows the session. Composition, not a disappearing child view, owns it.
        return GridDetailGridSource<AssetRow>.Session(GridApiFactory.CreateDataBinding(session));
    }

    public void Dispose()
    {
        foreach (var session in _details.Values) session.Dispose();
        _master.Dispose();
    }
}
