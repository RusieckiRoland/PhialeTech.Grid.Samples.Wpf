using System.Windows;
using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.Composition;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.ViewModels;
using PhialeGrid.Core;
namespace PhialeTech.Grid.Samples.Wpf.RestSnapshot.Views;

public partial class MainWindow : Window
{
    private readonly PriceReviewViewModel _model;
    public MainWindow(IGridApi api, PriceReviewViewModel model)
    {
        _model = model; DataContext = model; InitializeComponent(); PricesGrid.BindRuntime(api);
        Closing += (_, e) =>
        {
            if (model.Grid?.IsBusy == true) { e.Cancel = true; return; }
            if (model.Grid?.HasPendingChanges == true && MessageBox.Show(this, "Zamknąć okno i odrzucić niezapisany bufor?", "Niezapisane zmiany", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) e.Cancel = true;
        };
        Closed += (_, _) => { model.Dispose(); PricesGrid.CloseRuntime(); };
    }
    private async void SecondOperator(object sender, RoutedEventArgs e)
    {
        var source = new SnapshotComposition(); var api = GridApiFactory.CreateRuntime();
        var other = new PriceReviewViewModel(api, source) { Address = _model.Address, Supplier = _model.Supplier, Category = _model.Category, ImportedFrom = _model.ImportedFrom, ImportedTo = _model.ImportedTo };
        var window = new MainWindow(api, other) { Title = "06 · Drugi operator", Left = Left + 30, Top = Top + 30 };
        window.Closed += (_, _) => source.Dispose(); window.Show(); await other.LoadAsync();
    }
    private async void DiscardAndReload(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Odrzucić CAŁY lokalny bufor i pobrać aktualny cennik? Ta operacja nie nadpisuje danych serwera.", "Pobranie aktualnych danych", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _model.Grid?.DiscardChanges(); await _model.LoadAsync();
    }
    private async Task Policy(int value)
    {
        try { await _model.SetMarginPolicyAsync(value); } catch (Exception error) { MessageBox.Show(this, error.Message, "Serwer niedostępny"); }
    }
    private async void StrictPolicy(object sender, RoutedEventArgs e) => await Policy(35);
    private async void NormalPolicy(object sender, RoutedEventArgs e) => await Policy(15);
}
