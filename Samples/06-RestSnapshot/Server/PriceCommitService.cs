using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
namespace PhialeTech.Grid.Samples.PriceReview.Server;

// All checks and writes occur in one transaction. No Grid assembly is used by this server.
public static class PriceCommitService
{
    public static async Task<PriceCommitResponse> SaveAsync(PriceDatabase db, PriceCommit request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 100 || request.Changes == null || request.Changes.Length is < 1 or > 10000 || request.Changes.Any(c => c == null || string.IsNullOrWhiteSpace(c.Id) || c.Fields == null) ||
            request.Changes.Select(c => c.Id).Distinct().Count() != request.Changes.Length)
            throw new ArgumentException("Nieprawidłowy pakiet zmian.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var receipt = await db.Receipts.FindAsync([request.RequestId], token);
        if (receipt != null)
        {
            if (receipt.BodyHash != hash) throw new ArgumentException("Ten identyfikator zapisu ma już inną treść.");
            return JsonSerializer.Deserialize<PriceCommitResponse>(receipt.Response)!;
        }
        var policy = await db.Policies.SingleAsync(token);
        var results = new List<PriceOutcome>();
        var updates = new List<PriceEntity>();
        foreach (var change in request.Changes)
        {
            var current = await PriceQueries.InScope(db, request.Scope).SingleOrDefaultAsync(p => p.Id == change.Id, token);
            if (current == null) { results.Add(new(change.Id, "Rejected", "Pozycja nie istnieje w wybranym zakresie.")); continue; }
            if (current.Version != change.ExpectedVersion)
            {
                results.Add(new(change.Id, "Conflict", "Inny operator zmienił pozycję. Porównaj swoją propozycję z aktualną wersją serwera.", current.ToRow())); continue;
            }
            try
            {
                Apply(current, change);
                Validate(current, policy.MinimumMargin);
                updates.Add(current);
                results.Add(new(change.Id, "Saved", "Zapisano.", current.ToRow()));
            }
            catch (ArgumentException error) { results.Add(new(change.Id, "Rejected", error.Message)); }
        }
        if (results.Any(r => r.Status != "Saved"))
        {
            // Disposing the uncommitted transaction rolls back everything; keep every local edit.
            return new(results.Select(r => r.Status == "Saved" ? r with { Status = "Aborted", Current = null, Message = "Nie zapisano całego pakietu. Popraw konflikty i błędy, następnie ponów zapis." } : r).ToArray());
        }
        foreach (var row in updates) row.Version++;
        policy.Revision++;
        await db.SaveChangesAsync(token); // EF adds Version to each UPDATE predicate.
        var response = new PriceCommitResponse(updates.Select(p => new PriceOutcome(p.Id, "Saved", "Zapisano.", p.ToRow())).ToArray());
        db.Receipts.Add(new() { Id = request.RequestId, BodyHash = hash, Response = JsonSerializer.Serialize(response) });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return response;
    }
    private static void Apply(PriceEntity row, PriceChange change)
    {
        if (change.Fields.Count == 0) throw new ArgumentException("Brak zmian.");
        try
        {
            foreach (var field in change.Fields) switch (field.Key)
                {
                    case "ProposedSalePrice":
                        var price = field.Value.GetDecimal();
                        if (price is < 0 or > 1000000 || decimal.Round(price, 2) != price) throw new ArgumentException("Cena: 0–1 000 000, maksymalnie dwa miejsca po przecinku.");
                        row.SaleCents = checked((long)(price * 100)); break;
                    case "ValidFrom": row.ValidFrom = field.Value.GetDateTime(); break;
                    case "Decision": row.Decision = field.Value.GetString() ?? ""; break;
                    case "Comment": row.Comment = field.Value.GetString() ?? ""; break;
                    default: throw new ArgumentException("Pole nie podlega edycji: " + field.Key);
                }
        }
        catch (Exception error) when (error is FormatException or InvalidOperationException or OverflowException)
        {
            throw new ArgumentException("Nieprawidłowy typ wartości pola.", error);
        }
    }
    private static void Validate(PriceEntity row, int minimumMargin)
    {
        if (row.Decision is not ("Oczekuje" or "Zatwierdź" or "Odrzuć")) throw new ArgumentException("Wybierz decyzję z listy.");
        if (row.Comment.Length > 200 || row.Decision == "Odrzuć" && string.IsNullOrWhiteSpace(row.Comment)) throw new ArgumentException("Odrzucenie wymaga komentarza; maksymalnie 200 znaków.");
        if (row.ValidFrom.TimeOfDay != TimeSpan.Zero || row.ValidFrom.Date < DateTime.Today || row.ValidFrom.Date > DateTime.Today.AddYears(1)) throw new ArgumentException("Data obowiązywania: od dzisiaj do roku naprzód.");
        if (row.Decision != "Zatwierdź") return;
        if (!row.Active) throw new ArgumentException("Produkt został wycofany. Nie można zatwierdzić jego ceny.");
        // Decimal arithmetic, not the rounded percentage displayed by the client.
        if (row.SaleCents <= 0 || (row.SaleCents - row.PurchaseCents) * 100m < minimumMargin * row.SaleCents)
            throw new ArgumentException($"Aktualna polityka serwera wymaga marży co najmniej {minimumMargin}%. Zwiększ cenę lub zmień decyzję.");
    }
}
