namespace PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

public interface IStockRepository
{
    Task<IReadOnlyList<StockRow>> ReadAllAsync(CancellationToken cancellationToken);
}
