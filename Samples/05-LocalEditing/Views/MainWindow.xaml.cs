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
    private bool _closeFlowRunning;
    public MainWindow(IGridApi api, AssetsViewModel model)
    {
        _api = api; _model = model; DataContext = model; InitializeComponent();
        // Context binding connects the runtime; ViewModel observes its state directly.
        AssetsGrid.Culture = CultureInfo.GetCultureInfo("pl-PL");
        Loaded += (_, _) => Dispatcher.BeginInvoke(new Action(() => AssetsGrid.OpenWorkspacePanel(GridRegionKind.ChangePanelRegion)), System.Windows.Threading.DispatcherPriority.Loaded);

        Closing += ConfirmPendingChanges;
        Closed += (_, _) => { model.Dispose(); AssetsGrid.CloseRuntime(); };
    }

    private void ConfirmPendingChanges(object? sender, CancelEventArgs e)
    {
        if (AssetsGrid.CanCloseRuntime && (_closingAfterChoice ||
            (!_api.Editing.HasPendingEdits && !_api.Editing.IsSurfaceEditing))) return;

        // Closing must return before a dialog or another Close call is allowed.
        // SaveAsync may complete synchronously, so awaiting it alone is not a deferral.
        e.Cancel = true;
        if (_closeFlowRunning) return;
        _closeFlowRunning = true;
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            try { await FinishClosingAsync(); }
            catch (Exception error) { MessageBox.Show(this, error.Message, "Nie udało się zamknąć okna"); }
            finally { _closeFlowRunning = false; }
        }));
    }

    internal Func<MessageBoxResult>? ConfirmCloseChoice { get; set; }
    private MessageBoxResult AskCloseChoice() => ConfirmCloseChoice?.Invoke() ?? MessageBox.Show(this,
        "Zapisać oczekujące zmiany w SQLite przed zamknięciem?\nTak — zapisz. Nie — odrzuć bufor. Anuluj — wróć do edycji.",
        "Niezapisane zmiany", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

    private async Task FinishClosingAsync()
    {
        while (!AssetsGrid.CanCloseRuntime) await Task.Delay(25);
        if (_api.Editing.HasPendingEdits || _api.Editing.IsSurfaceEditing)
        {
            var choice = AskCloseChoice();
            if (choice == MessageBoxResult.Cancel) return;
            if (choice == MessageBoxResult.Yes)
            {
                if (_api.Editing.IsSurfaceEditing && !_api.Editing.CommitCell()) return;
                if (!(await _model.Grid.SaveAsync()).Succeeded) return;
            }
            else _model.Grid.DiscardChanges();
        }
        while (!AssetsGrid.CanCloseRuntime) await Task.Delay(25);
        _closingAfterChoice = true;
        Close();
    }
}
