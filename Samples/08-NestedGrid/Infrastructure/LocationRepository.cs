using System.IO;
using Microsoft.Data.Sqlite;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;

namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Infrastructure;

// Lesson 08 uses its own writable copy. The seed shipped with the repository stays unchanged.
// The existing assets table is reused; one small table stores editable location metadata.
public sealed class LocationRepository
{
    public AssetRepository Assets { get; }
    public string DatabasePath => Assets.DatabasePath;

    public LocationRepository(string database)
    {
        Assets = new AssetRepository(database);
        using var db = Assets.Open();
        using var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS locations (
                id TEXT PRIMARY KEY, contact TEXT NOT NULL, phone TEXT NOT NULL,
                notes TEXT NOT NULL);
            INSERT OR IGNORE INTO locations(id, contact, phone, notes)
                SELECT DISTINCT location, 'Opiekun lokalizacji', '', '' FROM assets;
            """;
        command.ExecuteNonQuery();
    }

    public static LocationRepository OpenWorkingCopy()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhialeTech", "GridSamples", "08-NestedGrid");
        Directory.CreateDirectory(folder);
        var database = Path.Combine(folder, "locations.sqlite");
        if (!File.Exists(database))
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Data", "local-editing.sqlite"), database);
        return new LocationRepository(database);
    }

    public IReadOnlyList<LocationRow> ReadLocations()
    {
        using var db = Assets.Open(); using var command = db.CreateCommand();
        command.CommandText = "SELECT id, contact, phone, notes FROM locations ORDER BY id";
        using var reader = command.ExecuteReader(); var rows = new List<LocationRow>();
        while (reader.Read()) rows.Add(new LocationRow {
            Id = reader.GetString(0), Contact = reader.GetString(1), Phone = reader.GetString(2),
            Notes = reader.GetString(3)
        });
        return rows;
    }
}
