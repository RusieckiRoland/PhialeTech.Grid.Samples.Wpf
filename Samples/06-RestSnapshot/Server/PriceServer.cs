using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
namespace PhialeTech.Grid.Samples.PriceReview.Server;

public static class PriceServer
{
    public const string Address = "http://127.0.0.1:5189";
    public static string DefaultDatabase => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhialeTech.Grid.Samples", "06-PriceReview", "pricing.sqlite");
    public static WebApplication Create(string address = Address, string? database = null)
    {
        database ??= DefaultDatabase;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(database))!);
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseUrls(address);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        builder.Services.AddDbContext<PriceDatabase>(options => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = database }.ToString()));
        var app = builder.Build();
        app.MapPost("/api/price-reviews/search", async (PriceSearch request, PriceDatabase db, CancellationToken token) =>
        {
            try { return Results.Ok(await PriceQueries.SearchAsync(db, request, token)); }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
        });
        app.MapPost("/api/price-reviews/commit", async (PriceCommit request, PriceDatabase db, CancellationToken token) =>
        {
            try { return Results.Ok(await PriceCommitService.SaveAsync(db, request, token)); }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict(new { error = "Równoległy zapis. Nic nie zapisano. Pobierz aktualne dane." }); }
        });
        app.MapGet("/api/policy", async (PriceDatabase db, CancellationToken token) => new MarginPolicy(await db.Policies.Select(p => (decimal)p.MinimumMargin).SingleAsync(token)));
        // Deliberately exposed only in this localhost educational server, never a production admin API.
        app.MapPut("/api/demo/policy", async (MarginPolicy body, PriceDatabase db, CancellationToken token) =>
        {
            if (body.MinimumMargin is < 0 or > 90 || decimal.Truncate(body.MinimumMargin) != body.MinimumMargin) return Results.BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            var policy = await db.Policies.SingleAsync(token); policy.MinimumMargin = (int)body.MinimumMargin; policy.Revision++;
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return Results.Ok(body);
        });
        app.Logger.LogInformation("Demo pricing database: {Database}", database);
        return app;
    }
    public static async Task InitializeAsync(WebApplication app, string? catalog = null)
    {
        using var scope = app.Services.CreateScope();
        await PriceSeed.EnsureAsync(scope.ServiceProvider.GetRequiredService<PriceDatabase>(), catalog ?? PriceSeed.FindCatalog());
    }
}
