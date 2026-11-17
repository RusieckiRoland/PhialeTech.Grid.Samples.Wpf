using System.Text;
using System.Text.Json;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

// At most 4 warehouses x 4 categories in the supplied database, not a million records.
internal sealed record GroupAggregate(string Warehouse, string Category, int Count, long OnHand, long Reserved);
internal sealed class StockGroup(GroupKey[] path, Totals totals)
{
    public GroupKey[] Path { get; } = path;
    public Totals Totals { get; } = totals;
    public string Id { get; } = "g-" + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(path)));
    public List<StockGroup> Children { get; } = [];
    public int EndIndex { get; set; }
}

internal static class GroupCatalog
{
    public static Totals Total(IEnumerable<GroupAggregate> aggregates)
        => new(aggregates.Sum(a => a.Count), aggregates.Sum(a => a.OnHand), aggregates.Sum(a => a.Reserved));

    public static List<StockGroup> Build(IReadOnlyList<GroupAggregate> aggregates, StockOrder[] fields)
        => fields.Length == 0 ? [] : Level(aggregates, fields, 0, []);

    private static List<StockGroup> Level(IEnumerable<GroupAggregate> aggregates, StockOrder[] fields, int depth, GroupKey[] parent)
    {
        var field = fields[depth];
        var groups = aggregates.GroupBy(a => field.Field == "warehouseName" ? a.Warehouse : a.Category, StringComparer.Ordinal);
        var ordered = field.Direction == "desc" ? groups.OrderByDescending(g => g.Key, StringComparer.Ordinal)
            : groups.OrderBy(g => g.Key, StringComparer.Ordinal);
        return ordered.Select(group => {
            var node = new StockGroup([.. parent, new GroupKey(field.Field, group.Key)], Total(group));
            if (depth + 1 < fields.Length) node.Children.AddRange(Level(group, fields, depth + 1, node.Path));
            return node;
        }).ToList();
    }
}
