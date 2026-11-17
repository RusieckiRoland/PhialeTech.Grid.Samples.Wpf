// Why this composition uses a data builder:
// The example teaches four separate decisions: record structure, initial loading,
// persistence of pending edits, and session creation. Naming those steps makes
// the data flow easier to follow than a constructor with several callbacks.
// The builder receives all rows already read from SQLite; no paged reader is needed.
// It does not introduce another data engine or hide SQLite writes. Only the
// commit port persists edits. Build returns a data session, not a UI control,
// and this composition owns that session and disposes it when it is no longer used.

using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Data;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Composition;

public sealed class AssetGridComposition : IDisposable
{
    private readonly GridDataSession<AssetRow> _session;

    private AssetGridComposition(GridDataSession<AssetRow> session, string database)
    {
        _session = session;
        DatabasePath = database;
        Binding = GridApiFactory.CreateDataBinding(session);
    }

    public GridDataBinding Binding { get; }
    public string DatabasePath { get; }

    public static async Task<AssetGridComposition> CreateAsync(
        string database, CancellationToken cancellationToken = default)
    {
        var repository = new AssetRepository(database);
        var rows = await Task.Run(repository.ReadAll, cancellationToken);

        var session = GridDataBuilder
            .For<AssetRow>()

            // 1. Record structure, identity and independent working copies.
            // Columns: use AssetColumns' fields, headers, editors and validation rules.
            // This describes the table; it does not read SQLite.
            .Columns(AssetColumns.All)

            // Key: use the stable, unique Id to identify the same record after sorting
            // or filtering and to associate its pending edits. It is not a row number.
            .Key(row => row.Id)

            // Clone: tell Grid how to create an independent working copy for editing.
            // AssetRow is a record, so "with { }" creates a shallow copy here.
            // Clone itself does not require a record: for a regular class, supply
            // your own copying function. Copy nested mutable objects separately.
            .Clone(row => row with { })

            // 2. Use all rows already loaded from SQLite. No paging or further reads.
            .Records(rows)

            // 3. Only this port may persist pending changes in SQLite.
            .CommitUsing(new SqliteCommitPort(repository, rows))

            // 4. Create the data session. This does not create a UI control.
            .Build(cancellationToken);

        return new AssetGridComposition(session, repository.DatabasePath);
    }

    public void Dispose() => _session.Dispose();
}
