using System.Globalization;
using Microsoft.Data.Sqlite;
using PhialeGrid.Core.Commit;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;

// Save receives a ChangeSet from Grid. Either every update is persisted, or none is.
public sealed class SqliteCommitPort : IGridCommitPort
{
    private readonly AssetRepository repository;
    private readonly Dictionary<string, int> _versions;
    public SqliteCommitPort(AssetRepository repository, IReadOnlyList<AssetRow> loadedRows)
    {
        this.repository = repository;
        _versions = loadedRows.ToDictionary(row => row.Id, row => row.Version);
    }
    public Task<GridCommitResult> CommitAsync(ChangeSet changes, CancellationToken token) =>
        Task.Run(() => Save(changes, token), token);

    private GridCommitResult Save(ChangeSet changes, CancellationToken token)
    {
        try {
            token.ThrowIfCancellationRequested();
            using var db = repository.Open(); using var transaction = db.BeginTransaction();
            var results = new List<GridRecordCommitResult>();
            foreach (var change in changes.Changes) {
                token.ThrowIfCancellationRequested();
                if (change.Operation != ChangeOperation.Update)
                    return Reject(changes, GridRecordCommitStatus.BusinessRejected, "Przykład obsługuje edycję istniejących rekordów.");
                var current = AssetRepository.Find(db, transaction, change.TargetId);
                if (current is null || !_versions.TryGetValue(change.TargetId, out var loadedVersion) || current.Version != loadedVersion)
                    return Reject(changes, GridRecordCommitStatus.VersionConflict,
                        "Baza została zmieniona w innym oknie. Nic nie zapisano. Odrzuć bufor i otwórz aplikację ponownie, aby odczytać aktualne dane.");
                var edited = Apply(current with { }, change);
                Validate(edited); // Protect persistence even if a caller bypasses Grid's editors.
                AssetRepository.Update(db, transaction, edited);
                results.Add(new(change.TargetId, GridRecordCommitStatus.Succeeded,
                    (edited.Version + 1).ToString(CultureInfo.InvariantCulture), operation: change.Operation));
            }
            token.ThrowIfCancellationRequested();
            using (var budget = db.CreateCommand()) {
                budget.Transaction = transaction;
                budget.CommandText = "SELECT COALESCE(SUM(quantity * price_cents), 0) FROM assets";
                if (Convert.ToDecimal(budget.ExecuteScalar(), CultureInfo.InvariantCulture) > 500_000_000m)
                    return Reject(changes, GridRecordCommitStatus.BusinessRejected, "Łączna wartość wyposażenia przekracza budżet 5 000 000 zł. Nic nie zapisano.");
            }
            transaction.Commit(); // The only point at which the changes become durable.
            foreach (var result in results) _versions[result.RecordId] = int.Parse(result.VersionToken, CultureInfo.InvariantCulture);
            return new(changes.ChangeSetId, results);
        } catch (ArgumentException error) {
            return Reject(changes, GridRecordCommitStatus.ValidationRejected, error.Message);
        } catch (SqliteException) {
            return Reject(changes, GridRecordCommitStatus.TechnicalFailed,
                "Nie udało się zapisać do SQLite. Nic nie zapisano; zmiany pozostają w buforze. Sprawdź dostęp do pliku i ponów zapis.");
        }
    }

    private static GridCommitResult Reject(ChangeSet changes, GridRecordCommitStatus status, string message) =>
        new(changes.ChangeSetId, changes.Changes.Select(change =>
            new GridRecordCommitResult(change.TargetId, status, message: message, operation: change.Operation)).ToArray());

    private static AssetRow Apply(AssetRow row, ChangeSetChange change)
    {
        foreach (var field in change.FieldChanges) {
            switch (field.FieldName) {
                case "Name": row.Name = (string)field.NewValue; break;
                case "Category": row.Category = (string)field.NewValue; break;
                case "Quantity": row.Quantity = (int)field.NewValue; break;
                case "UnitPrice": row.UnitPrice = (decimal)field.NewValue; break;
                case "InService": row.InService = (bool)field.NewValue; break;
                case "NextInspection": row.NextInspection = (DateTime?)field.NewValue; break;
                case "Location": row.Location = (string)field.NewValue; break;
                case "Notes": row.Notes = (string)field.NewValue; break;
                default: throw new ArgumentException("Nieobsługiwane pole: " + field.FieldName);
            }
        }
        return row;
    }

    private static void Validate(AssetRow row)
    {
        var businessErrors = new AssetBusinessRules().Validate(row);
        if (businessErrors.Count > 0) throw new ArgumentException(businessErrors[0].Message);
        if (string.IsNullOrWhiteSpace(row.Name) || row.Name.Length > 80) throw new ArgumentException("Nazwa jest wymagana (maks. 80 znaków).");
        if (!AssetColumns.Categories.Contains(row.Category)) throw new ArgumentException("Wybierz kategorię z listy.");
        if (row.Quantity is < 0 or > 500) throw new ArgumentException("Ilość: od 0 do 500.");
        if (row.UnitPrice is < 0 or > 100000 || decimal.Round(row.UnitPrice, 2) != row.UnitPrice) throw new ArgumentException("Cena: od 0 do 100000, maks. 2 miejsca po przecinku.");
        if (row.NextInspection.HasValue && (row.NextInspection.Value.Year is < 2000 or > 2100 || row.NextInspection.Value.TimeOfDay != TimeSpan.Zero)) throw new ArgumentException("Przegląd: sama data w latach 2000–2100 albo brak daty.");
        if (string.IsNullOrWhiteSpace(row.Location) || row.Location.Length > 60) throw new ArgumentException("Lokalizacja jest wymagana (maks. 60 znaków).");
        if (row.Notes is null || row.Notes.Length > 200) throw new ArgumentException("Uwagi: maks. 200 znaków.");
    }
}
