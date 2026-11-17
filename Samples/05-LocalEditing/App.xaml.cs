using System.Windows;
using PhialeGrid.Core;
using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Composition;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.ViewModels;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Views;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing;
public partial class App : Application
{
    private AssetGridComposition? _data;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try {
            _data = await AssetGridComposition.CreateAsync(DemoDatabase.OpenWorkingCopy());
            var api = GridApiFactory.CreateRuntime();
            try { MainWindow = new MainWindow(api, CreateModel(api)); MainWindow.Show(); }
            catch { api.Dispose(); throw; }
        } catch (Exception error) { MessageBox.Show(error.Message, "Nie udało się otworzyć przykładu"); Shutdown(1); }
    }
    private AssetsViewModel CreateModel(IGridApi api)
    {
        var context = GridApiFactory.CreateContext<PhialeTech.Grid.Samples.Wpf.LocalEditing.Models.AssetRow>(api, _data!.Binding);
        context.SetValidators(new Configuration.AssetBusinessRules(), new AssetBudgetValidator(new AssetRepository(_data.DatabasePath)));
        return new AssetsViewModel(context, _data.DatabasePath);
    }
    protected override void OnExit(ExitEventArgs e) { _data?.Dispose(); base.OnExit(e); }
}
