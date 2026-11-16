using System.IO;
using System.Globalization;
using Microsoft.Data.Sqlite;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;

// Explicit SQLite mapping. No database access while the user types in a cell.
public sealed class AssetRepository(string database)
{
    public string DatabasePath { get; } = Path.GetFullPath(database);
    public SqliteConnection Open()
    {
        SQLitePCL.Batteries_V2.Init();
        var db = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = DatabasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 3
        }.ToString());
        db.Open(); return db;
    }

    public IReadOnlyList<AssetRow> ReadAll()
    {
        using var db = Open(); using var command = db.CreateCommand();
        command.CommandText = "SELECT id,name,category,quantity,price_cents,in_service,next_inspection,location,notes,version FROM assets ORDER BY id";
        using var reader = command.ExecuteReader(); var rows = new List<AssetRow>();
        while (reader.Read()) rows.Add(Read(reader));
        return rows;
    }

    public static AssetRow? Find(SqliteConnection db, SqliteTransaction transaction, string id)
    {
        using var command = db.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT id,name,category,quantity,price_cents,in_service,next_inspection,location,notes,version FROM assets WHERE id=$id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader(); return reader.Read() ? Read(reader) : null;
    }

    public static void Update(SqliteConnection db, SqliteTransaction transaction, AssetRow row)
    {
        using var command = db.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            UPDATE assets SET name=$name,category=$category,quantity=$quantity,price_cents=$price,
                in_service=$active,next_inspection=$date,location=$location,notes=$notes,version=version+1
            WHERE id=$id AND version=$version
            """;
        command.Parameters.AddWithValue("$id", row.Id);
        command.Parameters.AddWithValue("$version", row.Version);
        command.Parameters.AddWithValue("$name", row.Name);
        command.Parameters.AddWithValue("$category", row.Category);
        command.Parameters.AddWithValue("$quantity", row.Quantity);
        command.Parameters.AddWithValue("$price", checked((long)(row.UnitPrice * 100m)));
        command.Parameters.AddWithValue("$active", row.InService ? 1 : 0);
        command.Parameters.AddWithValue("$date", (object?)row.NextInspection?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$location", row.Location);
        command.Parameters.AddWithValue("$notes", row.Notes);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Rekord zmienił się podczas zapisu.");
    }

    private static AssetRow Read(SqliteDataReader row) => new() {
        Id = row.GetString(0), Name = row.GetString(1), Category = row.GetString(2), Quantity = row.GetInt32(3),
        UnitPrice = row.GetInt64(4) / 100m, InService = row.GetInt32(5) != 0,
        NextInspection = row.IsDBNull(6) ? null : DateTime.ParseExact(row.GetString(6), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Location = row.GetString(7), Notes = row.GetString(8), Version = row.GetInt32(9)
    };
}
