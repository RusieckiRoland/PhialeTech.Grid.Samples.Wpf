using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;
namespace PhialeTech.Grid.Samples.HttpRest.Server;
public sealed record StockPage(StockRow[] Stocks, int Total);
