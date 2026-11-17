using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.Infrastructure;

// The same small seed as lesson 04, opened read-only. No working database is modified.
public static class AssetCatalog
{
    public static IReadOnlyList<AssetRow> Read(string? file = null)
    {
        file ??= Path.Combine(AppContext.BaseDirectory, "Data", "local-editing.sqlite");
        SQLitePCL.Batteries_V2.Init();
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = file, Mode = SqliteOpenMode.ReadOnly, Pooling = false
        }.ToString());
        db.Open(); using var command = db.CreateCommand();
        command.CommandText = "SELECT id,name,category,quantity,price_cents,in_service,next_inspection,location,notes,version FROM assets ORDER BY id";
        using var reader = command.ExecuteReader(); var rows = new List<AssetRow>();
        while (reader.Read()) rows.Add(new AssetRow {
            Id = reader.GetString(0), Name = reader.GetString(1), Category = reader.GetString(2), Quantity = reader.GetInt32(3),
            UnitPrice = reader.GetInt64(4) / 100m, InService = reader.GetInt32(5) != 0,
            NextInspection = reader.IsDBNull(6) ? null : DateTime.ParseExact(reader.GetString(6), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Location = reader.GetString(7), Notes = reader.GetString(8), Version = reader.GetInt32(9)
        });
        return rows;
    }
}
