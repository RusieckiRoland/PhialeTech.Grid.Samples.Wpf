using PhialeGrid.Contracts;
using System.Windows;
using PhialeGrid.Core;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Regions;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.Composition;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.ViewModels;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.Views;

namespace PhialeTech.Grid.Samples.Wpf.ServerGrouping;

public partial class App : Application
{
    private StockGridComposition? _data;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _data = new StockGridComposition();
        var api = GridApiFactory.CreateRuntime();
        try {
            foreach (var region in new[] { GridRegionKind.TopCommandRegion, GridRegionKind.SummaryBottomRegion })
                api.Workspace.Execute(new GridWorkspaceRequest(GridRegionCommandKind.Close, region));
            var model = new StocksViewModel(_data.CreateBinding, api);
            MainWindow = new MainWindow(api, model);
            MainWindow.Show();
        } catch { api.Dispose(); _data.Dispose(); throw; }
    }
    protected override void OnExit(ExitEventArgs e) { _data?.Dispose(); base.OnExit(e); }
}
