using System.ComponentModel;
using System.Windows.Input;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Commit;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.Composition;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.Configuration;
namespace PhialeTech.Grid.Samples.Wpf.RestSnapshot.ViewModels;

public sealed class PriceReviewViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IGridApi _api;
    private readonly SnapshotComposition _source;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _busy, _disposed;
    private readonly List<ActionCommand> _commands = [];
    public PriceReviewViewModel(IGridApi api, SnapshotComposition source)
    {
        _api = api; _source = source;
        DownloadCommand = Command(() => LoadAsync(), () => !_busy && Grid == null);
        SaveCommand = Command(SaveAsync, () => !_busy && Grid?.CanSave == true);
        ReloadCommand = Command(() => LoadAsync(), () => !_busy && Grid?.HasPendingChanges != true);
        ApproveCommand = Command(() => { ApproveSelected(); return Task.CompletedTask; }, () => !_busy && Grid?.IsBusy == false && Grid.CheckedRecordIds.Count > 0);
    }
    public bool IsIdle => !_busy;
    public string Address { get; set; } = "http://127.0.0.1:5189";
    public string Supplier { get; set; } = "Nord";
    public string Category { get; set; } = "";
    public DateTime ImportedFrom { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime ImportedTo { get; set; } = DateTime.Today;
    public string[] Suppliers { get; } = ["Nord", "Baltic"];
    public string[] Categories { get; } = ["", "Cables", "Fasteners", "Packaging", "Workwear"];
    public ReviewScope Scope => new(Supplier, Category, ImportedFrom.Date, ImportedTo.Date);
    public IReadOnlyList<IGridColumnDescriptor> Columns => PriceColumns.All;
    public IGridContext<PriceRow>? Grid { get; private set; }
    public int RequestCount => _source.RequestCount;
    public string Status { get; private set; } = "Wybierz zakres. Dane zostaną pobrane przed rozpoczęciem lokalnej pracy.";
    public string SaveReport { get; private set; } = "";
    public ICommand DownloadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ApproveCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed() { PropertyChanged?.Invoke(this, new("")); foreach (var command in _commands) command.Raise(); }
    private void GridChanged(object? sender, PropertyChangedEventArgs e) => Changed();
    private ActionCommand Command(Func<Task> action, Func<bool> can)
    {
        var command = new ActionCommand(async () => { try { await action(); } catch (Exception e) { Status = e.Message; Changed(); } }, can); _commands.Add(command); return command;
    }
    public async Task LoadAsync(string? address = null)
    {
        if (_busy || _disposed) return;
        if (Grid?.HasPendingChanges == true || Grid?.IsEditing == true) { Status = "Najpierw zapisz albo świadomie odrzuć zmiany."; Changed(); return; }
        Address = address ?? Address; _busy = true; Status = "Pobieranie całego wybranego zakresu…"; Changed();
        try
        {
            var next = await _source.DownloadAsync(Columns, Scope, _lifetime.Token, Address);
            next.EditContext.CommitMode = GridCommitMode.Atomic;
            if (_disposed) { next.Dispose(); return; }
            if (Grid != null) { Grid.PropertyChanged -= GridChanged; Grid.Dispose(); }
            Grid = null; Changed();
            Grid = GridApiFactory.CreateContext<PriceRow>(_api, _source.Use(next));
            var rules = new PriceRules(); Grid.SetValidators(rules, rules); Grid.PropertyChanged += GridChanged;
            SaveReport = ""; Status = "Snapshot pobrany. Edytuj cenę sprzedaży, datę, decyzję i komentarz. Enter/Tab przenosi edycję do bufora. Dopiero Zapisz wysyła zmiany.";
        }
        catch (OperationCanceledException) when (_disposed) { }
        catch (Exception error) { Status = "Nie pobrano nowego snapshotu. Poprzedni pozostaje dostępny. " + error.Message; }
        finally { _busy = false; if (!_disposed) Changed(); }
    }
    public async Task SaveAsync()
    {
        if (_busy || Grid == null || !Grid.CanSave) return;
        _busy = true; Status = "Walidacja bufora i zapis całego pakietu przez REST…"; Changed();
        try
        {
            var result = await Grid.SaveAsync(_lifetime.Token);
            Status = result.Succeeded ? $"Zapisano {result.Records.Count} pozycji w jednej transakcji SQLite." : "Nic nie zapisano. Cały bufor pozostaje do poprawy; zobacz wynik i panel Walidacja.";
            SaveReport = string.Join(Environment.NewLine, result.Records.Take(30).Select(r => r.RecordId + ": " + r.Status + " — " + r.Message));
            var conflicts = _source.Endpoint?.LastResponse?.Rows.Where(r => r.Status == "Conflict" && r.Current != null).Take(5).ToArray() ?? [];
            foreach (var conflict in conflicts)
            {
                var server = conflict.Current!;
                SaveReport += $"\n{server.Id} — SERWER v{server.Version}: cena {server.ProposedSalePrice:N2}; decyzja {server.Decision}; komentarz {server.Comment}. Twoja propozycja pozostaje w tabeli i panelu Zmiany.";
            }
        }
        finally { _busy = false; Changed(); }
    }
    public void ApproveSelected()
    {
        if (Grid == null || Grid.IsBusy) return;
        var changed = Grid.CheckedRecordIds.Count(id => Grid.SetValue(id, nameof(PriceRow.Decision), "Zatwierdź"));
        Status = $"Zmieniono decyzję lokalnie dla {changed} pozycji. Zapisz wysyła bufor."; Changed();
    }
    public async Task SetMarginPolicyAsync(int value)
    {
        await _source.SetMarginPolicyAsync(Address, value, _lifetime.Token);
        Status = $"Zmieniono rzeczywistą politykę serwera: minimalna marża {value}%. Istniejący snapshot nie został pobrany ponownie."; Changed();
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _lifetime.Cancel();
        if (Grid != null) { Grid.PropertyChanged -= GridChanged; Grid.Dispose(); }
        _lifetime.Dispose();
    }
    private sealed class ActionCommand(Func<Task> action, Func<bool> can) : ICommand
    {
        public bool CanExecute(object? parameter) => can();
        public async void Execute(object? parameter) => await action();
        public event EventHandler? CanExecuteChanged;
        public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
