using PhialeGrid.Core.Commit;

namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Infrastructure;

// This port persists ONLY master edits. A detail's Save uses SqliteCommitPort instead.
// One Save is transactional for this table; it is not a transaction over all open grids.
public sealed class LocationCommitPort(LocationRepository repository) : IGridCommitPort
{
    public Task<GridCommitResult> CommitAsync(ChangeSet changes, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var db = repository.Assets.Open(); using var transaction = db.BeginTransaction();
        foreach (var change in changes.Changes)
        {
            if (change.Operation != ChangeOperation.Update)
                return Task.FromResult(Reject(changes, "Ten przykład edytuje istniejące lokalizacje."));
            foreach (var field in change.FieldChanges)
            {
                // Only these three editable fields may become SQL column names.
                var column = field.FieldName switch {
                    "Contact" => "contact", "Phone" => "phone", "Notes" => "notes",
                    _ => throw new ArgumentException("Nieedytowalne pole: " + field.FieldName)
                };
                var value = field.NewValue as string ?? "";
                var limit = field.FieldName == "Contact" ? 80 : field.FieldName == "Phone" ? 40 : 200;
                if (value.Length > limit || field.FieldName == "Contact" && string.IsNullOrWhiteSpace(value))
                    return Task.FromResult(Reject(changes, "Sprawdź dane lokalizacji: kontakt jest wymagany, a pola mają ograniczoną długość."));
                token.ThrowIfCancellationRequested();
                using var command = db.CreateCommand(); command.Transaction = transaction;
                command.CommandText = $"UPDATE locations SET {column}=$value WHERE id=$id";
                command.Parameters.AddWithValue("$value", value); command.Parameters.AddWithValue("$id", change.TargetId);
                if (command.ExecuteNonQuery() != 1)
                    return Task.FromResult(Reject(changes, "Nie znaleziono lokalizacji. Nic nie zapisano."));
            }
        }
        token.ThrowIfCancellationRequested();
        transaction.Commit(); // Nothing reaches SQLite until the complete batch is valid.
        return Task.FromResult(new GridCommitResult(changes.ChangeSetId, changes.Changes.Select(change =>
            new GridRecordCommitResult(change.TargetId, GridRecordCommitStatus.Succeeded, operation: change.Operation)).ToArray()));
    }

    private static GridCommitResult Reject(ChangeSet changes, string message) => new(changes.ChangeSetId,
        changes.Changes.Select(change => new GridRecordCommitResult(change.TargetId,
            GridRecordCommitStatus.ValidationRejected, message: message, operation: change.Operation)).ToArray());
}
