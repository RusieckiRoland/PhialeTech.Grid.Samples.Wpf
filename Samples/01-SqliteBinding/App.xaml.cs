using System.IO;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Composition;
using System.Windows;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Interaction;
using PhialeGrid.Core.Regions;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.ViewModels;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Views;

namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding;

public partial class App : Application
{
    private readonly StockGridComposition _gridData = new();
    protected override void OnExit(ExitEventArgs e)
    {
        _gridData.Dispose();
        base.OnExit(e);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Application composition: only this entry point creates the Grid Core.
        var databasePath = Path.Combine(AppContext.BaseDirectory, "Data", "warehouse-100000.sqlite");
        var repository = new SqliteStockRepository(databasePath);
        var viewModel = new StocksViewModel(repository, _gridData.CreateBinding);
        var gridApi = GridApiFactory.CreateRuntime();

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
