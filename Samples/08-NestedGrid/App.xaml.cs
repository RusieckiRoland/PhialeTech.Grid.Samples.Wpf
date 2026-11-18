using System.Windows;
using PhialeGrid.Core;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Composition;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Views;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Infrastructure;
namespace PhialeTech.Grid.Samples.Wpf.NestedGrid;
public partial class App : Application
{
    private LocationGridComposition? _source;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try {
            _source = new LocationGridComposition(LocationRepository.OpenWorkingCopy());
            var api = GridApiFactory.CreateRuntime();
            var model = new LocationsViewModel(GridApiFactory.CreateContext<LocationRow>(api, _source.Binding), _source.DatabasePath);
            MainWindow = new MainWindow(model, _source); MainWindow.Show();
        } catch (Exception error) { MessageBox.Show(error.Message, "Nie otwarto przykładu 08"); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { _source?.Dispose(); base.OnExit(e); }
}
