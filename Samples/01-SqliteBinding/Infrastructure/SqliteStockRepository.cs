using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding.Infrastructure;

/// <summary>Read-only SQLite access. This sample never creates or changes the database.</summary>
public sealed class SqliteStockRepository(string databasePath) : IStockRepository
{
    private readonly string _databasePath = Path.GetFullPath(databasePath);

    static SqliteStockRepository() => SQLitePCL.Batteries_V2.Init();

    // Microsoft.Data.Sqlite performs synchronous I/O, so run the read away from the UI thread.
    public Task<IReadOnlyList<StockRow>> ReadAllAsync(CancellationToken cancellationToken, int limit = 100000) =>
        Task.Run(() => ReadAll(cancellationToken, limit), cancellationToken);

    private IReadOnlyList<StockRow> ReadAll(CancellationToken cancellationToken, int limit)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (limit is not (10000 or 100000 or 1000000)) throw new ArgumentOutOfRangeException(nameof(limit));
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT b.warehouse_id || ':' || b.product_id AS Id,
                   p.code AS ProductCode, p.name AS ProductName, p.unit AS UnitCode,
                   w.name AS WarehouseName, b.on_hand AS OnHand, b.reserved AS Reserved, p.category AS Category
            FROM stock_balances AS b
            JOIN products AS p ON p.id = b.product_id
            JOIN warehouses AS w ON w.id = b.warehouse_id
            ORDER BY CAST(substr(p.code, instr(p.code, '-') + 1) AS INTEGER), p.category, p.code, w.id
            LIMIT $limit
            """;

        command.Parameters.AddWithValue("$limit", limit);
        var rows = new List<StockRow>(limit);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(new StockRow(
                Id: reader.GetString(0),
                ProductCode: reader.GetString(1),
                ProductName: reader.GetString(2),
                UnitCode: reader.GetString(3),
                WarehouseName: reader.GetString(4),
                OnHand: decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                Reserved: decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                Category: reader.GetString(7)));
        }

        return rows.AsReadOnly();
    }
}
