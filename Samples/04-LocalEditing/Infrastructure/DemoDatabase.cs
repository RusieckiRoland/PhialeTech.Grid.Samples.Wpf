using System.IO;
namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;

public static class DemoDatabase
{
    // The seed is never edited. Reopening the app reuses this persistent working database.
    public static string OpenWorkingCopy()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhialeTech", "GridSamples", "04-LocalEditing");
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, "assets.sqlite");
        if (!File.Exists(target)) File.Copy(Path.Combine(AppContext.BaseDirectory, "Data", "local-editing.sqlite"), target, overwrite: false);
        return target;
    }
}
