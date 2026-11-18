using System.Windows;
using PhialeTech.Grid.Samples.Wpf.RowDetails.ViewModels;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.Views;
public partial class MainWindow : Window
{
    public MainWindow(AssetsViewModel model)
    {
        DataContext = model; InitializeComponent();
        Closed += (_, _) => { model.Dispose(); AssetsGrid.CloseRuntime(); };
    }
}
