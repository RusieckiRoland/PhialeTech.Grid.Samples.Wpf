using Microsoft.EntityFrameworkCore;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
namespace PhialeTech.Grid.Samples.PriceReview.Server;

public sealed class PriceDatabase(DbContextOptions<PriceDatabase> options) : DbContext(options)
{
    public DbSet<PriceEntity> Prices => Set<PriceEntity>();
    public DbSet<PolicyEntity> Policies => Set<PolicyEntity>();
    public DbSet<ReceiptEntity> Receipts => Set<ReceiptEntity>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<PriceEntity>().HasKey(p => p.Id);
        model.Entity<PriceEntity>().Property(p => p.Version).IsConcurrencyToken();
        model.Entity<PriceEntity>().HasIndex(p => new { p.Supplier, p.Category, p.Imported });
        model.Entity<PolicyEntity>().HasKey(p => p.Id);
        model.Entity<ReceiptEntity>().HasKey(p => p.Id);
    }
}

// Store money as integer cents: exact values and predictable SQLite comparisons.
public sealed class PriceEntity
{
    public string Id { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime Imported { get; set; }
    public long OldCents { get; set; }
    public long PurchaseCents { get; set; }
    public long SaleCents { get; set; }
    public DateTime ValidFrom { get; set; }
    public string Decision { get; set; } = "Oczekuje";
    public string Comment { get; set; } = "";
    public bool Active { get; set; } = true;
    public int Version { get; set; } = 1;
    public PriceRow ToRow() => new()
    {
        Id = Id,
        Supplier = Supplier,
        ProductCode = ProductCode,
        ProductName = ProductName,
        Category = Category,
        OldPurchasePrice = OldCents / 100m,
        NewPurchasePrice = PurchaseCents / 100m,
        ProposedSalePrice = SaleCents / 100m,
        ValidFrom = ValidFrom,
        Decision = Decision,
        Comment = Comment,
        Version = Version
    };
}
public sealed class PolicyEntity
{
    public int Id { get; set; } = 1;
    public int MinimumMargin { get; set; } = 15;
    public long Revision { get; set; } = 1;
}
public sealed class ReceiptEntity
{
    public string Id { get; set; } = "";
    public string BodyHash { get; set; } = "";
    public string Response { get; set; } = "";
}
