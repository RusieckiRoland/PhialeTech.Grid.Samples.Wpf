using System.Linq.Expressions;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

internal static class StockSorting
{
    public static IOrderedQueryable<StockReadRow> Apply(IQueryable<StockReadRow> query, StockOrder[] sorts)
    {
        IOrderedQueryable<StockReadRow>? ordered = null;
        foreach (var sort in sorts) {
            var descending = sort.Direction == "desc";
            ordered = sort.Field switch {
                "id" => By(query, ordered, row => row.Id, descending),
                "productCode" => By(query, ordered, row => row.ProductCode, descending),
                "productName" => By(query, ordered, row => row.ProductName, descending),
                "category" => By(query, ordered, row => row.Category, descending),
                "unitCode" => By(query, ordered, row => row.UnitCode, descending),
                "warehouseName" => By(query, ordered, row => row.WarehouseName, descending),
                "onHand" => By(query, ordered, row => row.OnHand, descending),
                "reserved" => By(query, ordered, row => row.Reserved, descending),
                "available" => By(query, ordered, row => row.Available, descending),
                _ => throw new ArgumentException("Unknown field: " + sort.Field)
            };
        }

        // Stable ordering also when several rows have the same requested sort value.
        ordered = ordered == null ? query.OrderBy(row => row.ProductCode) : ordered.ThenBy(row => row.ProductCode);
        return ordered.ThenBy(row => row.WarehouseId).ThenBy(row => row.ProductId);
    }

    private static IOrderedQueryable<StockReadRow> By<TKey>(
        IQueryable<StockReadRow> query, IOrderedQueryable<StockReadRow>? ordered,
        Expression<Func<StockReadRow, TKey>> field, bool descending)
    {
        if (ordered == null)
            return descending ? query.OrderByDescending(field) : query.OrderBy(field);

        return descending ? ordered.ThenByDescending(field) : ordered.ThenBy(field);
    }
}
