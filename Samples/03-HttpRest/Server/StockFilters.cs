using System.Linq.Expressions;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

internal static class StockFilters
{
    public static IQueryable<StockReadRow> Apply(IQueryable<StockReadRow> query, StockQuery request)
    {
        if (request.Conditions.Length == 0) return query;

        var predicate = Condition(request.Conditions[0]);
        foreach (var condition in request.Conditions.Skip(1)) {
            var next = Condition(condition);
            predicate = request.Match == "all"
                ? PredicateComposer.And(predicate, next)
                : PredicateComposer.Or(predicate, next);
        }
        return query.Where(predicate);
    }

    // Each API field points to a normal, compiler-checked C# property.
    private static Expression<Func<StockReadRow, bool>> Condition(StockCondition condition) => condition.Field switch {
        "id" => Text(row => row.Id, condition),
        "productCode" => Text(row => row.ProductCode, condition),
        "productName" => Text(row => row.ProductName, condition),
        "category" => Text(row => row.Category, condition),
        "unitCode" => Text(row => row.UnitCode, condition),
        "warehouseName" => Text(row => row.WarehouseName, condition),
        "onHand" => Number(row => row.OnHand, condition),
        "reserved" => Number(row => row.Reserved, condition),
        "available" => Number(row => row.Available, condition),
        _ => throw new ArgumentException("Unknown field: " + condition.Field)
    };

    private static Expression<Func<StockReadRow, bool>> Text(
        Expression<Func<StockReadRow, string>> field, StockCondition condition)
    {
        var value = condition.Value.GetString()!;
        var upper = condition.Comparison == "between" ? condition.Upper.GetString()! : "";

        Expression<Func<string, bool>> matches = condition.Comparison switch {
            "eq" => text => text == value,
            "contains" => text => WarehouseDbContext.Lower(text).Contains(WarehouseDbContext.Lower(value)),
            "startsWith" => text => WarehouseDbContext.Lower(text).StartsWith(WarehouseDbContext.Lower(value)),
            "gt" => text => text.CompareTo(value) > 0,
            "lt" => text => text.CompareTo(value) < 0,
            "between" => text => text.CompareTo(value) >= 0 && text.CompareTo(upper) <= 0,
            _ => throw new ArgumentException("Unsupported text comparison: " + condition.Comparison)
        };
        return PredicateComposer.ForField(field, matches);
    }

    private static Expression<Func<StockReadRow, bool>> Number(
        Expression<Func<StockReadRow, double>> field, StockCondition condition)
    {
        var value = (double)condition.Value.GetDecimal();
        var upper = condition.Comparison == "between" ? (double)condition.Upper.GetDecimal() : 0;

        Expression<Func<double, bool>> matches = condition.Comparison switch {
            "eq" => number => number == value,
            "gt" => number => number > value,
            "lt" => number => number < value,
            "between" => number => number >= value && number <= upper,
            _ => throw new ArgumentException("Unsupported quantity comparison: " + condition.Comparison)
        };
        return PredicateComposer.ForField(field, matches);
    }
}
