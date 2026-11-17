using System.Windows;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Configuration;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels;
namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Views;
public partial class MainWindow : Window
{
    public MainWindow(LocationsViewModel model)
    {
        DataContext = model; InitializeComponent();
        LocationsGrid.BindDetailGrid(AssetRelation.Create(action => Dispatcher.BeginInvoke(action)), child => {
            child.CapabilityPolicy = new PhialeTech.Grid.Samples.Wpf.HttpRest.Configuration.RestExamplePolicy();
            child.ShowDiagnosticStatus = false; child.IsGridReadOnly = true; child.LanguageCode = "pl"; child.ShowFilterRow = true;
        });
        Closed += (_, _) => { model.Dispose(); LocationsGrid.CloseRuntime(); };
    }
}
