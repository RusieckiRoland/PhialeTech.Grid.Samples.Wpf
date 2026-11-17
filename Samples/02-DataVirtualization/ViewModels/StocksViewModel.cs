using System.ComponentModel;
using System.Runtime.CompilerServices;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Capabilities;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Configuration;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.DataVirtualization.ViewModels;

/// <summary>Describes columns, loads model rows and publishes properties for XAML binding.</summary>
public sealed class StocksViewModel(
    Func<int, IReadOnlyList<IGridColumnDescriptor>, GridDataBinding> createBinding, IGridApi? runtime = null) : INotifyPropertyChanged, IDisposable
{
    private GridDataBinding? _dataSession;
    public GridContext<StockRow>? Grid { get; private set; }
    public void Dispose() { Grid?.Dispose(); Grid = null; }
    private string _status = "Loading stock balances from SQLite…";

    public IReadOnlyList<int> DatasetSizes { get; } = new[] { 10000, 100000, 1000000 };
    public int SelectedSize { get; set; } = 100000;
    public bool IsIdle { get; private set; } = true;
    public System.Windows.Input.ICommand ReloadCommand => _reloadCommand ??= new ReloadCommandImpl(this);
    private ReloadCommandImpl? _reloadCommand;
    private CancellationToken _lifetime;

    private sealed class ReloadCommandImpl(StocksViewModel owner) : System.Windows.Input.ICommand
    {
        public bool CanExecute(object? parameter) => owner.IsIdle;
        public async void Execute(object? parameter) => await owner.LoadAsync(owner._lifetime);
        public event EventHandler? CanExecuteChanged;
        public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public IGridCapabilityPolicy Capabilities { get; } = new ReadOnlySnapshotPolicy();

    public IReadOnlyList<IGridColumnDescriptor> Columns { get; } = new GridColumnsBuilder<StockRow>()
        .Add(row => row.ProductCode, "Symbol", 130)
        .Add(row => row.ProductName, "Product", 310)
        .Add(row => row.Category, "Category", 180)
        .Add(row => row.WarehouseName, "Warehouse", 200)
        .Add(row => row.UnitCode, "Unit", 65)
        .Add(row => row.OnHand, "On hand", 115)
        .Add(row => row.Reserved, "Reserved", 115)
        .Add(row => row.Available, "Available", 115)
        .Build();


    public GridDataBinding? DataSession
    {
        get => _dataSession;
        private set { _dataSession = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        private set { _status = value; OnPropertyChanged(); }
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!IsIdle) return;
        _lifetime = cancellationToken;
        IsIdle = false; OnPropertyChanged(nameof(IsIdle)); _reloadCommand?.Refresh();
        var count = SelectedSize;
        Status = $"Loading {count:N0} rows…";
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await Task.CompletedTask; // Session configuration performs no database reads.
            cancellationToken.ThrowIfCancellationRequested();
            Grid?.Dispose(); Grid = null; OnPropertyChanged(nameof(Grid));
            DataSession = null;
            DataSession = createBinding(count, Columns);
            if (runtime != null) { Grid = GridApiFactory.CreateContext<StockRow>(runtime, DataSession); OnPropertyChanged(nameof(Grid)); }
            Status = $"Local source · up to {count:N0} records · reads only requested windows · scroll to load · paging may use the same source";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception error)
        {
            Status = $"Could not load SQLite data: {error.Message}";
        }
        finally { IsIdle = true; OnPropertyChanged(nameof(IsIdle)); _reloadCommand?.Refresh(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
