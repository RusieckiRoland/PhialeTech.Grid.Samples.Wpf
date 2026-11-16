using System.Windows;
using System.Windows.Threading;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeTech.Grid.Samples.HttpRest.Server;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.Composition;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.ViewModels;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.Views;

internal static class Program
{
    [STAThread]
    static int Main()
    {
        using var server = GroupingServer.Create();
        server.StartAsync().GetAwaiter().GetResult();
        var app = new PhialeTech.Grid.Samples.Wpf.ServerGrouping.App();
        app.InitializeComponent(); app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        using var data = new StockGridComposition();
        using var api = GridApiFactory.CreateRuntime();
        var model = new StocksViewModel(data.CreateBinding, api);
        api.Projection.ConfigureColumns(model.Columns);
        var window = new MainWindow(api, model) { Opacity = 0, ShowActivated = false, ShowInTaskbar = false };
        var frame = new DispatcherFrame(); Exception? failure = null; var started = DateTime.UtcNow; bool expanding = false;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += async (_, _) => {
            try {
                if (DateTime.UtcNow - started > TimeSpan.FromSeconds(100)) throw new TimeoutException("WPF grouped startup/expansion");
                if (api.Data.Current.IsBusy || !api.Data.Current.HasConfirmedQuery) return;
                if (api.Projection.Groups.Count != 2) throw new Exception("Native binding lost initial grouping");
                var view = api.Projection.ProjectCurrent(); var rows = api.Projection.GetSurfaceRows(view);
                if (!expanding) {
                    if (rows.Count == 0 || rows[0].Group?.Kind != GridQueryRowKind.GroupHeader) throw new Exception("Expected grouped server rows");
                    Console.WriteLine("PASS actual WPF MainWindow loads server grouping");
                    expanding = true; timer.Stop();
                    var id = rows[0].Group!.GroupId;
                    if (!await api.Navigation.NavigateToGroupEndAsync("group:" + id)) throw new Exception("Native remote group navigation failed");
                    if (!api.Projection.GetSurfaceRows(api.Projection.ProjectCurrent()).Any(r => r.Group?.Kind == GridQueryRowKind.GroupSummary && r.Group.GroupId == id)) throw new Exception("Missing native group footer");
                    Console.WriteLine("PASS actual WPF group-end command reaches the full-group summary");
                    frame.Continue = false;
                }
            } catch (Exception error) { failure = error; timer.Stop(); frame.Continue = false; }
        };
        try { window.Show(); timer.Start(); Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); window.Close(); app.Shutdown(); server.StopAsync().GetAwaiter().GetResult(); }
        if (failure != null) { Console.Error.WriteLine(failure); return 1; }
        return 0;
    }
}
