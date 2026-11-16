using PhialeGrid.Contracts;
using PhialeGrid.Core.Commit;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;

// A real read of SQLite plus the proposed buffer; no write occurs during validation.
// The commit port remains the authoritative persistence boundary and must recheck transactional rules.
public sealed class AssetBudgetValidator(AssetRepository repository) : IGridChangesValidator<AssetRow>
{
    public Task<IReadOnlyList<GridApplicationValidationIssue>> ValidateAsync(
        IReadOnlyList<GridValidationRecord<AssetRow>> changes, CancellationToken token) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        var proposed = repository.ReadAll().ToDictionary(row => row.Id);
        foreach (var change in changes) {
            if (change.Operation == ChangeOperation.Remove) proposed.Remove(change.RecordId);
            else proposed[change.RecordId] = change.ProposedRecord;
        }
        token.ThrowIfCancellationRequested();
        IReadOnlyList<GridApplicationValidationIssue> errors = proposed.Values.Sum(row => row.Quantity * row.UnitPrice) > 5_000_000m
            ? [new(changes[0].RecordId, "", "Łączna wartość wyposażenia przekracza budżet 5 000 000 zł. Nic nie zapisano.")]
            : [];
        return errors;
    }, token);
}
