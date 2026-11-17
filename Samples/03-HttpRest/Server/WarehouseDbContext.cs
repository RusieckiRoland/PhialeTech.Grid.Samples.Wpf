using Microsoft.Data.Sqlite;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

// Maps the existing database. No migrations, EnsureCreated or seed data.
public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<WarehouseProduct> Products => Set<WarehouseProduct>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseBalance> Balances => Set<WarehouseBalance>();

    // Infrastructure for a consistent read of COUNT and page from this read-only database.
    public async Task<SqliteTransaction> BeginReadSnapshotAsync(CancellationToken cancellationToken)
    {
        await Database.OpenConnectionAsync(cancellationToken);
        var connection = (SqliteConnection)Database.GetDbConnection();
        var transaction = connection.BeginTransaction(deferred: true);
        try {
            await Database.UseTransactionAsync(transaction, cancellationToken);
            return transaction;
        } catch {
            await transaction.DisposeAsync();
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<WarehouseProduct>(p => {
            p.ToTable("products");
            p.HasKey(x => x.Id);
            p.Property(x => x.Id).HasColumnName("id");
            p.Property(x => x.Code).HasColumnName("code");
            p.Property(x => x.Name).HasColumnName("name");
            p.Property(x => x.Unit).HasColumnName("unit");
            p.Property(x => x.Category).HasColumnName("category");
        });
        model.Entity<Warehouse>(w => {
            w.ToTable("warehouses");
            w.HasKey(x => x.Id);
            w.Property(x => x.Id).HasColumnName("id");
            w.Property(x => x.Name).HasColumnName("name");
        });
        model.Entity<WarehouseBalance>(b => {
            b.ToTable("stock_balances");
            b.HasKey(x => new { x.WarehouseId, x.ProductId });
            b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.Property(x => x.OnHand).HasColumnName("on_hand");
            b.Property(x => x.Reserved).HasColumnName("reserved");
        });
        // Keep case conversion in SQLite for both the column and the parameter.
        model.HasDbFunction(typeof(WarehouseDbContext).GetMethod(nameof(Lower))!)
            .HasName("lower").IsBuiltIn();
        // This existing database stores quantities as TEXT. Use CAST(... AS REAL)
        // for numeric filters and sorting; keep original text for exact decimal output.
        model.HasDbFunction(typeof(WarehouseDbContext).GetMethod(nameof(Quantity))!)
            .HasTranslation(args => new SqlUnaryExpression(ExpressionType.Convert,
                args[0], typeof(double), new DoubleTypeMapping("REAL")));
    }

    public static string Lower(string text) => throw new InvalidOperationException("SQL translation only.");

    public static double Quantity(string text) => throw new InvalidOperationException("SQL translation only.");
}

public sealed class WarehouseProduct
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Category { get; set; } = "";
}
public sealed class Warehouse
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
public sealed class WarehouseBalance
{
    public string WarehouseId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string OnHand { get; set; } = "";
    public string Reserved { get; set; } = "";
}
