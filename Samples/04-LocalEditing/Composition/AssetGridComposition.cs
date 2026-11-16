using System.Globalization;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Composition;

public sealed class AssetGridComposition : IDisposable
{
    private readonly GridDataSession<AssetRow> _session;
    private AssetGridComposition(GridDataSession<AssetRow> session, string database)
        { _session = session; DatabasePath = database; Binding = GridApiFactory.CreateDataBinding(session); }
    public GridDataBinding Binding { get; }
    public string DatabasePath { get; }

    public static async Task<AssetGridComposition> CreateAsync(string database, CancellationToken token = default)
    {
        var repository = new AssetRepository(database);
        var rows = await Task.Run(repository.ReadAll, token);
        // Grid edits copies of these rows. Only SqliteCommitPort may persist the pending changes.
        var source = new GridDataSource<AssetRow>(AssetColumns.All, row => row.Id, row => row with { });
        var session = await source.CreateSnapshotSessionAsync(new SnapshotReader(rows),
            new GridSnapshotOptions { MaxRecords = 100 }, new SqliteCommitPort(repository, rows), token);
        return new(session, repository.DatabasePath);
    }

    // This provider is used only to load the initial local snapshot, not while editing.
    private sealed class SnapshotReader(IReadOnlyList<AssetRow> rows) : IGridQueryDataProvider<AssetRow>
    {
        public Task<GridQueryResult<AssetRow>> QueryAsync(GridQueryRequest request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new GridQueryResult<AssetRow>(rows.Skip(request.Offset).Take(request.Size).ToArray(), rows.Count, null, GridSummarySet.Empty));
        }
    }
    public void Dispose() => _session.Dispose();
}
