using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

public sealed class GroupedStockRepository
{
    private readonly DbContextOptions<WarehouseDbContext> _options;

    public GroupedStockRepository(string database, Action<string>? sqlLog = null)
    {
        SQLitePCL.Batteries_V2.Init();
        if (!File.Exists(database)) throw new FileNotFoundException("Run git lfs pull to fetch Data/warehouse-1000000.sqlite.", database);
        var connection = new SqliteConnectionStringBuilder { DataSource = database, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        var options = new DbContextOptionsBuilder<WarehouseDbContext>().UseSqlite(connection.ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        if (sqlLog != null) options.LogTo(sqlLog, [DbLoggerCategory.Database.Command.Name], LogLevel.Information);
        _options = options.Options;
    }

    public async Task<GroupPage> ReadAsync(GroupRequest request, CancellationToken token)
    {
        request.Validate();
        await using var db = new WarehouseDbContext(_options);
        await using var snapshot = await db.BeginReadSnapshotAsync(token);
        var query = StockFilters.Apply(Stocks(db), request.Query);

        // 1. SQLite groups and aggregates the complete filtered result.
        var aggregates = await AggregateAsync(query, token);
        var total = GroupCatalog.Total(aggregates);
        var roots = GroupCatalog.Build(aggregates, request.GroupBy);
        var layout = new GroupLayout(roots, total, request);

        // 2. Intersect the requested visible page with the small group layout.
        var rows = new List<GroupRow>();
        var end = Math.Min((long)request.Query.Skip + request.Query.Take, layout.VisibleCount);
        foreach (var block in layout.Blocks) {
            token.ThrowIfCancellationRequested();
            var start = Math.Max(request.Query.Skip, block.Start);
            var stop = Math.Min(end, (long)block.Start + block.Length);
            if (start >= stop) continue;
            var group = block.Group;
            if (block.Kind == "group") {
                var key = group!.Path[^1];
                rows.Add(new("group", group.Path.Length - 1, group.Id, key.Field, key.Value,
                    group.Totals.Count, !request.Collapsed.Contains(group.Id), Path: group.Path));
            } else if (block.Kind == "summary") {
                rows.Add(new("summary", group!.Path.Length, group.Id, Totals: group.Totals, Path: group.Path));
            } else {
                // 3. Only the intersecting records are fetched, with SQL LIMIT/OFFSET.
                var memberQuery = InGroup(query, group);
                var offset = start - block.Start;
                var records = await StockSorting.Apply(memberQuery, request.Query.OrderBy)
                    .Skip(offset).Take((int)(stop - start)).ToListAsync(token);
                for (var index = 0; index < records.Count; index++)
                    rows.Add(new("record", group?.Path.Length ?? 0, Item: Item(records[index]),
                        GlobalNumber: block.RecordBase + offset + index + 1, GroupNumber: offset + index + 1));
            }
        }
        snapshot.Commit();
        return new(rows.ToArray(), layout.VisibleCount, total.Count, roots.Count,
            layout.AllGroups.Select(g => g.Id).ToArray(), total);
    }

    public async Task<LocatedRow> LocateAsync(LocateRequest location, CancellationToken token)
    {
        if (location.Request is null || location.Target is null) throw new ArgumentException("target and request are required.");
        location.Request.Validate();
        if (!location.Target.StartsWith("groupend:", StringComparison.Ordinal)) return new(null);
        await using var db = new WarehouseDbContext(_options);
        await using var snapshot = await db.BeginReadSnapshotAsync(token);
        var aggregates = await AggregateAsync(StockFilters.Apply(Stocks(db), location.Request.Query), token);
        var roots = GroupCatalog.Build(aggregates, location.Request.GroupBy);
        var layout = new GroupLayout(roots, GroupCatalog.Total(aggregates), location.Request);
        var group = layout.AllGroups.SingleOrDefault(g => g.Id == location.Target[9..]);
        // Only groups visible in the current expansion state are navigation targets.
        var visible = group != null && layout.Blocks.Any(b => b.Kind == "group" && b.Group == group);
        snapshot.Commit();
        return new(visible ? group!.EndIndex : null);
    }

    private static Task<List<GroupAggregate>> AggregateAsync(IQueryable<StockReadRow> query, CancellationToken token) =>
        query.GroupBy(row => new { row.WarehouseName, row.Category })
            .Select(group => new GroupAggregate(group.Key.WarehouseName, group.Key.Category, group.Count(),
                group.Sum(row => (long)Math.Round(row.OnHand * 100)),
                group.Sum(row => (long)Math.Round(row.Reserved * 100))))
            .ToListAsync(token);

    private static IQueryable<StockReadRow> InGroup(IQueryable<StockReadRow> query, StockGroup? group)
    {
        foreach (var key in group?.Path ?? []) {
            var value = key.Value;
            query = key.Field == "warehouseName" ? query.Where(row => row.WarehouseName == value)
                : query.Where(row => row.Category == value);
        }
        return query;
    }

    private static StockRow Item(StockReadRow row) => new(row.Id, row.ProductCode, row.ProductName,
        row.UnitCode, row.WarehouseName, decimal.Parse(row.OnHandText, CultureInfo.InvariantCulture),
        decimal.Parse(row.ReservedText, CultureInfo.InvariantCulture), row.Category);

    private static IQueryable<StockReadRow> Stocks(WarehouseDbContext db) =>
        from balance in db.Balances
        join product in db.Products on balance.ProductId equals product.Id
        join warehouse in db.Warehouses on balance.WarehouseId equals warehouse.Id
        select new StockReadRow {
            Id = warehouse.Id + ":" + product.Id, ProductId = product.Id, WarehouseId = warehouse.Id,
            ProductCode = product.Code, ProductName = product.Name, Category = product.Category,
            UnitCode = product.Unit, WarehouseName = warehouse.Name,
            OnHandText = balance.OnHand, ReservedText = balance.Reserved,
            OnHand = WarehouseDbContext.Quantity(balance.OnHand), Reserved = WarehouseDbContext.Quantity(balance.Reserved),
            Available = Math.Round(WarehouseDbContext.Quantity(balance.OnHand) - WarehouseDbContext.Quantity(balance.Reserved), 2)
        };
}
