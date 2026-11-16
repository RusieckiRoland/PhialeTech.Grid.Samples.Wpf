namespace PhialeTech.Grid.Samples.HttpRest.Server;

public static class StockServer
{
    public const string Address = "http://127.0.0.1:5187";

    // Find the existing repository Data folder from either the terminal or build output.
    public static string FindDatabase()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent) {
            var candidate = Path.Combine(directory.FullName, "Data", "warehouse-1000000.sqlite");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Data/warehouse-1000000.sqlite was not found. Run git lfs pull in the sample repository.");
    }

    public static WebApplication Create(string address = Address, string? databasePath = null, string provider = "ef", Action<string>? logSql = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(address);
        var app = builder.Build();
        databasePath ??= FindDatabase();
        Func<StockQuery, CancellationToken, Task<StockPage>> search = provider switch {
            "ef" => new EfStockRepository(databasePath, logSql ?? (sql => app.Logger.LogInformation("{Sql}", sql))).SearchAsync,
            "sql" => new StockRepository(databasePath).SearchAsync,
            _ => throw new ArgumentException("Provider must be ef or sql.")
        };
        app.Logger.LogInformation("Data provider: {Provider}", provider);
        app.Logger.LogInformation("SQLite database: {Database}", Path.GetFullPath(databasePath));

        app.MapPost("/api/stocks/search", async (StockQuery body, HttpContext context) => {
            try {
                var page = await search(body, context.RequestAborted);
                app.Logger.LogInformation("Stocks: skip={Skip}, take={Take}, total={Total}",
                    body.Skip, body.Take, page.Total);
                return Results.Ok(page);
            } catch (ArgumentException error) {
                return Results.BadRequest(new { error = error.Message });
            }
        });
        return app;
    }
}