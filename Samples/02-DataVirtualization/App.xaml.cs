using System.IO;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Composition;
using System.Windows;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Regions;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.ViewModels;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Views;

namespace PhialeTech.Grid.Samples.Wpf.DataVirtualization;

public partial class App : Application
{
    private StockGridComposition? _gridData;
    protected override void OnExit(ExitEventArgs e)
    {
        _gridData?.Dispose();
        base.OnExit(e);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Application composition: only this entry point creates the Grid Core.
        var databasePath = Path.Combine(AppContext.BaseDirectory, "Data", "warehouse-1000000.sqlite");
        var repository = new StockRepository(databasePath);
        var gridApi = GridApiFactory.CreateRuntime();
        _gridData = new StockGridComposition(repository);
        var viewModel = new StocksViewModel(_gridData.CreateBinding, gridApi);

        try
        {
            foreach (var region in new[] { GridRegionKind.TopCommandRegion,
                GridRegionKind.GroupingRegion, GridRegionKind.SummaryBottomRegion })
                gridApi.Workspace.Execute(new GridWorkspaceRequest(GridRegionCommandKind.Close, region));

            MainWindow = new MainWindow(gridApi, viewModel);
            MainWindow.Show();
        }
        catch
        {
            gridApi.Dispose();
            throw;
        }
    }
}
