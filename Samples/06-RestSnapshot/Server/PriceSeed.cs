using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace PhialeTech.Grid.Samples.PriceReview.Server;

public static class PriceSeed
{
    public static string FindCatalog()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            var path = Path.Combine(folder.FullName, "Data", "warehouse-1000000.sqlite");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Run git lfs pull: Data/warehouse-1000000.sqlite is required.");
    }
    public static async Task EnsureAsync(PriceDatabase db, string catalog)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Policies.AnyAsync()) return;
        // Reuse products from demo 01. The warehouse file is opened read-only and never changed.
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = catalog, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open(); using var command = connection.CreateCommand();
        // Spread the selection across the full catalog rather than selecting the first category only.
        command.CommandText = "SELECT code, name, category FROM products ORDER BY code";
        var products = new List<(string Code, string Name, string Category)>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) products.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        if (products.Count < 10000) throw new InvalidOperationException("The full warehouse catalog is required.");
        using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var supplier in new[] { "Nord", "Baltic" })
        {
            for (var i = 0; i < 10000; i++)
            {
                var product = products[(int)((long)i * products.Count / 10000)];
                var old = 1000 + (i * 137L) % 90000;
                var purchase = old * (95 + i % 31) / 100;
                db.Prices.Add(new PriceEntity
                {
                    Id = $"{supplier}-{i + 1:D5}",
                    Supplier = supplier,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    Category = product.Category,
                    Imported = DateTime.Today.AddDays(-(i % 7)),
                    OldCents = old,
                    PurchaseCents = purchase,
                    SaleCents = (long)decimal.Ceiling(purchase / 0.75m),
                    ValidFrom = DateTime.Today.AddDays(7),
                    Active = i % 997 != 996
                });
            }
        }
        db.Policies.Add(new()); await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
}
