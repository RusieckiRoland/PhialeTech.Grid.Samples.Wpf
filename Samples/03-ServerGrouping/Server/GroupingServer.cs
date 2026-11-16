namespace PhialeTech.Grid.Samples.HttpRest.Server;

public static class GroupingServer
{
    public const string Address = "http://127.0.0.1:5188";
    public static string FindDatabase()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent) {
                var file = Path.Combine(directory.FullName, "Data", "warehouse-1000000.sqlite");
                if (File.Exists(file)) return file;
            }
        throw new FileNotFoundException("Run git lfs pull in PhialeTech.Grid.Samples.Wpf.");
    }

    public static WebApplication Create(string address = Address, string? database = null, Action<string>? sqlLog = null)
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseUrls(address);
        var app = builder.Build();
        database ??= FindDatabase();
        var repository = new GroupedStockRepository(database, sqlLog ?? (sql => app.Logger.LogInformation("{Sql}", sql)));
        app.Logger.LogInformation("SQLite read-only: {Database}", Path.GetFullPath(database));
        app.MapPost("/api/stocks/query", async (GroupRequest body, HttpContext context) => {
            try { return Results.Ok(await repository.ReadAsync(body, context.RequestAborted)); }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
        });
        app.MapPost("/api/stocks/locate", async (LocateRequest body, HttpContext context) => {
            try { return Results.Ok(await repository.LocateAsync(body, context.RequestAborted)); }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
        });
        return app;
    }
}
