using System.Text.Json;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

// This API contract belongs to the server. It has no Grid types.
public sealed class StockQuery
{
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
    public StockOrder[] OrderBy { get; set; } = [];
    public string Match { get; set; } = "all";
    public StockCondition[] Conditions { get; set; } = [];

    public void Validate()
    {
        if (Skip < 0 || Take is < 1 or > 100) throw new ArgumentException("skip must be non-negative; take must be between 1 and 100.");
        if (OrderBy is null || OrderBy.Length > 8 || Conditions is null || Conditions.Length > 8 || Match is not ("all" or "any"))
            throw new ArgumentException("Use up to 8 sorts, 8 conditions and match all/any.");
        static bool Numeric(string field) => field is "onHand" or "reserved" or "available";
        static void Field(string field) {
            if (field is not ("id" or "productCode" or "productName" or "category" or "unitCode" or "warehouseName" or "onHand" or "reserved" or "available"))
                throw new ArgumentException("Unknown field: " + field);
        }
        static void Value(string field, JsonElement value) {
            if (Numeric(field)) {
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out _)) throw new ArgumentException("Quantity filters require a decimal number.");
            } else if (value.ValueKind != JsonValueKind.String || value.GetString()!.Length > 200) throw new ArgumentException("Text filters require a string of at most 200 characters.");
        }
        foreach (var sort in OrderBy) {
            if (sort is null || sort.Direction is not ("asc" or "desc")) throw new ArgumentException("Sort direction must be asc or desc.");
            Field(sort.Field);
        }
        foreach (var condition in Conditions) {
            if (condition is null) throw new ArgumentException("A condition cannot be null.");
            Field(condition.Field);
            if (condition.Comparison is not ("eq" or "gt" or "lt" or "between" or "contains" or "startsWith") ||
                Numeric(condition.Field) && condition.Comparison is ("contains" or "startsWith")) throw new ArgumentException("Unsupported comparison for this field.");
            Value(condition.Field, condition.Value);
            if (condition.Comparison == "between") Value(condition.Field, condition.Upper);
        }
    }


}

public sealed class StockOrder
{
    public string Field { get; set; } = "";
    public string Direction { get; set; } = "asc";
}

public sealed class StockCondition
{
    public string Field { get; set; } = "";
    public string Comparison { get; set; } = "eq";
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Value { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Upper { get; set; }
}