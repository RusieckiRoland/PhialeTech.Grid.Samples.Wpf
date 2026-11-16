using System.ComponentModel;
using System.Globalization;
using System.Windows;
using PhialeGrid.Contracts;
using PhialeGrid.Core.Regions;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.ViewModels;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Views;

public partial class MainWindow : Window
{
    private readonly IGridApi _api;
    private readonly AssetsViewModel _model;
    private bool _closingAfterChoice;
    private bool _waitingToClose;
    public MainWindow(IGridApi api, AssetsViewModel model)
    {
        _api = api; _model = model; DataContext = model; InitializeComponent();
        // Context binding connects the runtime; ViewModel observes its state directly.
        AssetsGrid.Culture = CultureInfo.GetCultureInfo("pl-PL");
        Loaded += (_, _) => Dispatcher.BeginInvoke(new Action(() => AssetsGrid.OpenWorkspacePanel(GridRegionKind.ChangePanelRegion)), System.Windows.Threading.DispatcherPriority.Loaded);

        Closing += ConfirmPendingChanges;
        Closed += (_, _) => { model.Dispose(); AssetsGrid.CloseRuntime(); };
    }

    private async void ConfirmPendingChanges(object? sender, CancelEventArgs e)
    {
        if (!AssetsGrid.CanCloseRuntime) {
            e.Cancel = true;
            if (_waitingToClose) return;
            _waitingToClose = true;
            while (!AssetsGrid.CanCloseRuntime) await Task.Delay(25);
            _waitingToClose = false; Close(); return;
        }
        if (_closingAfterChoice || (!_api.Editing.HasPendingEdits && !_api.Editing.IsSurfaceEditing)) return;
        e.Cancel = true;
        if (_api.Editing.IsCommitting) return;
        var choice = MessageBox.Show(this, "Zapisać oczekujące zmiany w SQLite przed zamknięciem?\nTak — zapisz. Nie — odrzuć bufor. Anuluj — wróć do edycji.",
            "Niezapisane zmiany", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (choice == MessageBoxResult.Cancel) return;
        if (choice == MessageBoxResult.Yes) {
            if (_api.Editing.IsSurfaceEditing && !_api.Editing.CommitCell()) return;
            try { if (!(await _model.Grid.SaveAsync()).Succeeded) return; }
            catch (Exception error) { MessageBox.Show(this, error.Message, "Zapis nie powiódł się"); return; }
        } else _model.Grid.DiscardChanges();
        _closingAfterChoice = true; Close();
    }
}
