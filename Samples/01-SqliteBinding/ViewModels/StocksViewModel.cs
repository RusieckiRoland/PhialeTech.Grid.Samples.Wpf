using System.ComponentModel;
using System.Runtime.CompilerServices;
using PhialeGrid.Contracts;
using PhialeGrid.Core.Capabilities;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Configuration;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding.ViewModels;

/// <summary>Describes columns, loads model rows and publishes properties for XAML binding.</summary>
public sealed class StocksViewModel(IStockRepository repository,
    Func<IReadOnlyList<StockRow>, IReadOnlyList<IGridColumnDescriptor>, GridDataBinding> createBinding) : INotifyPropertyChanged
{
    private readonly IStockRepository _repository = repository;
    private IReadOnlyList<StockRow> _rows = Array.Empty<StockRow>();
    private GridDataBinding? _dataSession;
    private string _status = "Loading stock balances from SQLite…";

    public IGridCapabilityPolicy Capabilities { get; } = new ReadOnlySnapshotPolicy();

    public IReadOnlyList<IGridColumnDescriptor> Columns { get; } = new GridColumnsBuilder<StockRow>()
        .Add(row => row.ProductCode, "Symbol", 130)
        .Add(row => row.ProductName, "Product", 310)
        .Add(row => row.WarehouseName, "Warehouse", 200)
        .Add(row => row.UnitCode, "Unit", 65)
        .Add(row => row.OnHand, "On hand", 115)
        .Add(row => row.Reserved, "Reserved", 115)
        .Add(row => row.Available, "Available", 115)
        .Build();

    public IReadOnlyList<StockRow> Rows
    {
        get => _rows;
        private set { _rows = value; OnPropertyChanged(); }
    }

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
        try
        {
            var rows = await _repository.ReadAllAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Rows = rows;
            DataSession = createBinding(rows, Columns);
            Status = $"SQLite · {Rows.Count:N0} rows loaded in memory · read-only snapshot · local paging";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception error)
        {
            Status = $"Could not load SQLite data: {error.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
