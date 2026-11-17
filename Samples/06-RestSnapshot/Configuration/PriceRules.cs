using PhialeGrid.Contracts;
using PhialeGrid.Core.Editing;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
namespace PhialeTech.Grid.Samples.Wpf.RestSnapshot.Configuration;

// Run these application rules for the entire buffer before any HTTP write.
// Current margin policy and product availability remain authoritative on the server.
public sealed class PriceRules : IGridRecordValidator<PriceRow>, IGridChangesValidator<PriceRow>
{
    public IReadOnlyList<GridValidationError> Validate(PriceRow row)
    {
        var errors = new List<GridValidationError>();
        if (row.Decision == "Odrzuć" && string.IsNullOrWhiteSpace(row.Comment)) errors.Add(new("Comment", "Odrzucenie wymaga uzasadnienia."));
        if (row.ValidFrom.Date < DateTime.Today || row.ValidFrom.Date > DateTime.Today.AddYears(1)) errors.Add(new("ValidFrom", "Wybierz datę od dzisiaj do roku naprzód."));
        return errors;
    }
    public Task<IReadOnlyList<GridApplicationValidationIssue>> ValidateAsync(IReadOnlyList<GridValidationRecord<PriceRow>> records, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        IReadOnlyList<GridApplicationValidationIssue> issues = records.SelectMany(r => Validate(r.ProposedRecord).Select(e => new GridApplicationValidationIssue(r.RecordId, e.ColumnId, e.Message))).ToArray();
        return Task.FromResult(issues);
    }
}
