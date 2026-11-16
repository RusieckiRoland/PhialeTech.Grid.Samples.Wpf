using System.Windows;
using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.ViewModels;

namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding.Views;

public partial class MainWindow : Window
{
    private readonly StocksViewModel _viewModel;
    private readonly CancellationTokenSource _lifetime = new();

    public MainWindow(IGridApi gridApi, StocksViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        StockGrid.BindRuntime(gridApi);
        Loaded += LoadRows;
        Closed += ReleaseResources;
    }

    // View lifecycle bridge only: no SQL, column definitions or business rules in code-behind.
    private async void LoadRows(object sender, RoutedEventArgs e)
    {
        Loaded -= LoadRows;
        await _viewModel.LoadAsync(_lifetime.Token);
    }

    private void ReleaseResources(object? sender, EventArgs e)
    {
        _lifetime.Cancel();
        _viewModel.Dispose();
        StockGrid.CloseRuntime();
        _lifetime.Dispose();
    }
}
