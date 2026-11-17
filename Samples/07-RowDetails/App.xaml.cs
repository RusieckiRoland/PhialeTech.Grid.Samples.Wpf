using System.Windows;
using PhialeGrid.Core;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.RowDetails.Composition;
using PhialeTech.Grid.Samples.Wpf.RowDetails.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.RowDetails.ViewModels;
using PhialeTech.Grid.Samples.Wpf.RowDetails.Views;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails;
public partial class App : Application
{
    private AssetGridComposition? _source;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try {
            _source = new AssetGridComposition(AssetCatalog.Read());
            var api = GridApiFactory.CreateRuntime();
            var model = new AssetsViewModel(GridApiFactory.CreateContext<AssetRow>(api, _source.Binding));
            MainWindow = new MainWindow(model); MainWindow.Show();
        } catch (Exception error) { MessageBox.Show(error.Message, "Nie otwarto przykładu 06"); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { _source?.Dispose(); base.OnExit(e); }
}
