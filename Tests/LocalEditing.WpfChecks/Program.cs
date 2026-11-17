using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PhialeGrid.Core;
using PhialeGrid.Core.Regions;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Composition;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.ViewModels;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Views;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

internal static class Program
{
 [STAThread]
 static int Main()
 {
    var file = Path.Combine(Path.GetTempPath(), "Phiale-local-wpf-" + Guid.NewGuid().ToString("N") + ".sqlite");
    File.Copy(Path.Combine(AppContext.BaseDirectory, "Data", "local-editing.sqlite"), file);
    using var data = AssetGridComposition.CreateAsync(file).GetAwaiter().GetResult();
    var app = new PhialeTech.Grid.Samples.Wpf.LocalEditing.App(); app.InitializeComponent(); app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    using var api = GridApiFactory.CreateRuntime();
    var model = new AssetsViewModel(PhialeGrid.Core.GridApiFactory.CreateContext<PhialeTech.Grid.Samples.Wpf.LocalEditing.Models.AssetRow>(api, data.Binding), file);
    var window = new MainWindow(api, model) { Opacity = 0, ShowActivated = false, ShowInTaskbar = false };
    var frame = new DispatcherFrame(); var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    Exception? failure = null; var start = DateTime.UtcNow;
    void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
    async Task Flush() { window.UpdateLayout(); await Dispatcher.Yield(DispatcherPriority.ApplicationIdle); window.UpdateLayout(); }
    IEnumerable<T> Children<T>(DependencyObject parent) where T : DependencyObject {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
            var child = VisualTreeHelper.GetChild(parent, i); if (child is T item) yield return item;
            foreach (var nested in Children<T>(child)) yield return nested;
        }
    }
    timer.Tick += async (_, _) => {
        try {
            if (DateTime.UtcNow - start > TimeSpan.FromSeconds(30)) throw new TimeoutException("WPF snapshot load");
            if (api.Data.Current.IsBusy || !api.Data.Current.HasConfirmedQuery) return;
            timer.Stop(); await Flush();
            var grid = (PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid)window.FindName("AssetsGrid");

            Check(api.Editing.Records.Count == 24 && api.Workspace.IsRegionOpenAndActive(GridRegionKind.ChangePanelRegion), "actual WPF loads 24 assets and opens Changes panel");
            Check(!grid.IsPagingEnabled && !api.Projection.IsPagingEnabled, "local example has no paging");
            Check(api.Data.Current.VisibleRecords.Count == 24, "all 24 loaded records are available together");
            foreach (var (field, type) in new[] { ("Name", typeof(TextBox)), ("Category", typeof(ComboBox)),
                ("Quantity", typeof(TextBox)), ("UnitPrice", typeof(TextBox)), ("InService", typeof(CheckBox)),
                ("NextInspection", typeof(DatePicker)), ("Location", typeof(ComboBox)) }) {
                Check(grid.ScrollCellIntoView("EQ-001", field, GridScrollAlignment.Start, setCurrentCell: true), "scroll to " + field);
                Check(grid.BeginEditCurrentCell(), "begin native editor " + field); await Flush();
                var presenter = Children<GridCellPresenter>(grid).Single(p => p.CellData?.ColumnKey == field && p.CellData.IsEditing);
                Check(Children<FrameworkElement>(presenter).Any(c => type.IsInstanceOfType(c)), "native " + type.Name + " for " + field);
                grid.CancelCurrentEdit(); await Flush();
            }
            grid.ScrollCellIntoView("EQ-001", "Name", GridScrollAlignment.Start, setCurrentCell: true); grid.BeginEditCurrentCell(); await Flush();
            var active = Children<GridCellPresenter>(grid).Single(p => p.CellData?.ColumnKey == "Name" && p.CellData.IsEditing);
            Children<TextBox>(active).Single().Text = "WPF — zmiana w buforze"; await Flush();
            Check(grid.PostCurrentEdit(), "posting native text editor changes only buffer"); await Flush();
            Console.WriteLine($"LABELS save={grid.CommitEditsText}; discard={grid.CancelEditsText}; cancelCell={grid.CancelEditText}");
            var repo = new AssetRepository(file);
            Check(api.Editing.HasPendingEdits && repo.ReadAll()[0].Name != "WPF — zmiana w buforze", "SQLite unchanged after native cell post");
            Check(grid.ChangePanelItems.Any(item => item.DetailsText.Contains("Laptop serwisowy") && item.DetailsText.Contains("WPF — zmiana w buforze")), "Changes panel contains original and current field values");
            Check(Children<PhialeChangePanel>(grid).Any(p => p.IsVisible) && model.Status.Contains("Oczekujące"), "Changes panel and application status expose pending edits");
            var cancel = Children<Button>(grid).Single(b => AutomationProperties.GetAutomationId(b) == "grid.cancel-all");
            Check(cancel.IsEnabled && cancel.Command.CanExecute(cancel), "Discard toolbar command available"); cancel.Command.Execute(cancel); await Flush();
            Check(!api.Editing.HasPendingEdits && repo.ReadAll()[0].Name != "WPF — zmiana w buforze", "native Discard leaves database unchanged");
            api.Editing.TrySetFieldValue("EQ-001", "Name", "WPF — zapis do SQLite"); await Flush();
            var save = Children<Button>(grid).Single(b => AutomationProperties.GetAutomationId(b) == "grid.commit");
            Check(save.IsEnabled && save.Command.CanExecute(save), "Save toolbar command available"); save.Command.Execute(save);
            var wait = DateTime.UtcNow;
            while (api.Editing.HasPendingEdits || api.Editing.IsCommitting) { if (DateTime.UtcNow - wait > TimeSpan.FromSeconds(15)) throw new TimeoutException("Native toolbar Save"); await Task.Delay(40); }
            Check(repo.ReadAll()[0].Name == "WPF — zapis do SQLite", "native Save command persists buffer after Discard");
            while (!grid.CanCloseRuntime) { if (DateTime.UtcNow - wait > TimeSpan.FromSeconds(15)) throw new TimeoutException("WPF operation cleanup"); await Task.Delay(40); }
            Check(grid.CanCloseRuntime, "window can close after the save pipeline finishes");
            frame.Continue = false;
        } catch (Exception error) { failure = error; timer.Stop(); frame.Continue = false; }
    };
    try { window.Show(); timer.Start(); Dispatcher.PushFrame(frame); }
    finally { timer.Stop(); api.Editing.CancelAllEdits(); window.Close(); app.Shutdown(); data.Dispose(); File.Delete(file); }
    if (failure != null) { Console.Error.WriteLine(failure); return 1; }
    return 0;
 }
}
