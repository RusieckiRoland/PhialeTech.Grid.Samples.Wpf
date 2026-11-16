using System.Windows;
using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.HttpRest.ViewModels;

namespace PhialeTech.Grid.Samples.Wpf.HttpRest.Views;

public partial class MainWindow : Window
{
    public MainWindow(IGridApi api, StocksViewModel model)
    {
        DataContext = model;
        InitializeComponent();
        // Context in XAML attaches the application-owned runtime.
        Closed += (_, _) => { model.Grid.Dispose(); StocksGrid.CloseRuntime(); };
    }
}
