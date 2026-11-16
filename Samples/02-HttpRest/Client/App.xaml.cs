using PhialeGrid.Contracts;
using System.Windows;
using PhialeGrid.Core;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Regions;
using PhialeTech.Grid.Samples.Wpf.HttpRest.Composition;
using PhialeTech.Grid.Samples.Wpf.HttpRest.ViewModels;
using PhialeTech.Grid.Samples.Wpf.HttpRest.Views;

namespace PhialeTech.Grid.Samples.Wpf.HttpRest;

public partial class App : Application
{
    private StockGridComposition? _data;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _data = new StockGridComposition();
        var api = GridApiFactory.CreateRuntime();
        try {
            foreach (var region in new[] { GridRegionKind.TopCommandRegion, GridRegionKind.GroupingRegion, GridRegionKind.SummaryBottomRegion })
                api.Workspace.Execute(new GridWorkspaceRequest(GridRegionCommandKind.Close, region));
            MainWindow = new MainWindow(api, new StocksViewModel(_data.CreateBinding, api));
            MainWindow.Show();
        } catch { api.Dispose(); _data.Dispose(); throw; }
    }
    protected override void OnExit(ExitEventArgs e) { _data?.Dispose(); base.OnExit(e); }
}
