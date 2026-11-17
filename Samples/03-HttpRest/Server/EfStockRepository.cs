using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

public sealed class EfStockRepository
{
    private readonly DbContextOptions<WarehouseDbContext> _options;
    private readonly int? _recordLimit;

    public EfStockRepository(string databasePath, Action<string>? logSql = null, int? recordLimit = null)
    {
        _recordLimit = recordLimit;
        SQLitePCL.Batteries_V2.Init();
        if (!File.Exists(databasePath)) throw new FileNotFoundException("Missing warehouse database. Run git lfs pull.", databasePath);
        var connection = new SqliteConnectionStringBuilder {
            DataSource = Path.GetFullPath(databasePath), Mode = SqliteOpenMode.ReadOnly, Pooling = false
        };
        var builder = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseSqlite(connection.ToString()).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        if (logSql != null) builder.LogTo(logSql, [DbLoggerCategory.Database.Command.Name], LogLevel.Information);
        _options = builder.Options;
    }

    public async Task<StockPage> SearchAsync(StockQuery request, CancellationToken cancellationToken)
    {
        request.Validate(_recordLimit.HasValue ? int.MaxValue : 100);
        await using var db = new WarehouseDbContext(_options);
        await using var transaction = await db.BeginReadSnapshotAsync(cancellationToken);

        IQueryable<StockReadRow> query =
            from balance in db.Balances
            join product in db.Products on balance.ProductId equals product.Id
            join warehouse in db.Warehouses on balance.WarehouseId equals warehouse.Id
            select new StockReadRow {
                Id = warehouse.Id + ":" + product.Id,
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                Category = product.Category,
                UnitCode = product.Unit,
                WarehouseName = warehouse.Name,
                OnHandText = balance.OnHand,
                ReservedText = balance.Reserved,
                OnHand = WarehouseDbContext.Quantity(balance.OnHand),
                Reserved = WarehouseDbContext.Quantity(balance.Reserved),
                Available = Math.Round(WarehouseDbContext.Quantity(balance.OnHand) - WarehouseDbContext.Quantity(balance.Reserved), 2)
            };

        // 1. Apply the API filters. This is still IQueryable, not loaded data.
        if (_recordLimit.HasValue) query = query.OrderBy(row => WarehouseDbContext.Quantity(
                row.ProductCode.Substring(row.ProductCode.IndexOf("-") + 1)))
            .ThenBy(row => row.Category).ThenBy(row => row.ProductCode)
            .ThenBy(row => row.WarehouseId).Take(_recordLimit.Value);
        var filtered = StockFilters.Apply(query, request);

        // 2. Count the complete filtered result, before paging.
        var total = await filtered.CountAsync(cancellationToken);

        // 3. Sort, select a page, then execute the database query.
        var ordered = StockSorting.Apply(filtered, request.OrderBy);
        var rows = await ordered
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        // Only this already-paged result is mapped in memory. No client-side query evaluation.
        var items = rows.Select(row => new StockRow(
            Id: row.Id,
            ProductCode: row.ProductCode,
            ProductName: row.ProductName,
            UnitCode: row.UnitCode,
            WarehouseName: row.WarehouseName,
            OnHand: decimal.Parse(row.OnHandText, CultureInfo.InvariantCulture),
            Reserved: decimal.Parse(row.ReservedText, CultureInfo.InvariantCulture),
            Category: row.Category)).ToArray();

        transaction.Commit();
        return new StockPage(items, total);
    }
}
