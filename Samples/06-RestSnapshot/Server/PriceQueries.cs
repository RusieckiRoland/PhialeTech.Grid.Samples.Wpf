using Microsoft.EntityFrameworkCore;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
namespace PhialeTech.Grid.Samples.PriceReview.Server;

public static class PriceQueries
{
    public static IQueryable<PriceEntity> InScope(PriceDatabase db, ReviewScope scope)
    {
        if (scope == null || scope.ImportedFrom.Year is < 2000 or > 2100 || scope.ImportedTo.Year is < 2000 or > 2100 || scope.Supplier is not ("Nord" or "Baltic") || scope.ImportedTo.Date < scope.ImportedFrom.Date)
            throw new ArgumentException("Wybierz dostawcę i prawidłowy zakres dat importu.");
        var from = scope.ImportedFrom.Date; var until = scope.ImportedTo.Date.AddDays(1);
        var query = db.Prices.Where(p => p.Supplier == scope.Supplier && p.Imported >= from && p.Imported < until);
        return string.IsNullOrWhiteSpace(scope.Category) ? query : query.Where(p => p.Category == scope.Category);
    }
    public static async Task<PricePage> SearchAsync(PriceDatabase db, PriceSearch search, CancellationToken token)
    {
        if (search.Skip < 0 || search.Take is < 1 or > 1000) throw new ArgumentException("Nieprawidłowe okno pobierania.");
        // Page, count and revision belong to one database read transaction.
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var query = InScope(db, search.Scope).AsNoTracking();
        var revision = await db.Policies.Select(p => p.Revision).SingleAsync(token);
        var count = await query.CountAsync(token);
        var rows = await query.OrderBy(p => p.Id).Skip(search.Skip).Take(search.Take).ToArrayAsync(token);
        await transaction.CommitAsync(token);
        return new(rows.Select(p => p.ToRow()).ToArray(), count, revision);
    }
}
