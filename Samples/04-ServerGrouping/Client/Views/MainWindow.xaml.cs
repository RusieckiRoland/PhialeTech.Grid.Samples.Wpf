using System.Windows;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.ServerGrouping.ViewModels;

namespace PhialeTech.Grid.Samples.Wpf.ServerGrouping.Views;

public partial class MainWindow : Window
{
    public MainWindow(IGridApi api, StocksViewModel model)
    {
        DataContext = model;
        InitializeComponent();
        // Configure groups before the first Loaded query. Runtime comes from the context binding.
        StocksGrid.SetGroups([new("WarehouseName"), new("Category")]);
        StocksGrid.Summaries = [new("ProductCode", GridSummaryType.Count),
            new("OnHand", GridSummaryType.Sum), new("Reserved", GridSummaryType.Sum),
            new("Available", GridSummaryType.Sum)];
        api.Projection.ShowGroupSummaries = true;
        // Context in XAML attaches the application-owned runtime.
        Closed += (_, _) => { model.Grid.Dispose(); StocksGrid.CloseRuntime(); };
    }
}
