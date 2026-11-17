using System.Windows;
using PhialeGrid.Core;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.Composition;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.ViewModels;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.Views;
namespace PhialeTech.Grid.Samples.Wpf.RestSnapshot;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var source = new SnapshotComposition(); var api = GridApiFactory.CreateRuntime(); var model = new PriceReviewViewModel(api, source);
        MainWindow = new MainWindow(api, model); MainWindow.Closed += (_, _) => source.Dispose(); MainWindow.Show();
    }
}
