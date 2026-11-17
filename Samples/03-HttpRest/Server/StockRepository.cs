using Microsoft.Data.Sqlite;
using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

// SQLite executes WHERE, ORDER BY, COUNT and LIMIT/OFFSET. Only one page is read.
public sealed class StockRepository
{
    private readonly string _connectionString;

    public StockRepository(string databasePath)
    {
        SQLitePCL.Batteries_V2.Init();
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("Missing warehouse database. Run git lfs pull in the sample repository.", databasePath);
        _connectionString = new SqliteConnectionStringBuilder {
            DataSource = Path.GetFullPath(databasePath), Mode = SqliteOpenMode.ReadOnly, Pooling = false
        }.ToString();
    }

    private const string Tables = """
         FROM stock_balances AS b
         JOIN products AS p ON p.id = b.product_id
         JOIN warehouses AS w ON w.id = b.warehouse_id
        """;
    public async Task<StockPage> SearchAsync(StockQuery query, CancellationToken cancellationToken)
    {
        query.Validate();
        if (query.Skip < 0 || query.Take is < 1 or > 100)
            throw new ArgumentException("skip must be non-negative; take must be between 1 and 100.");
        if (query.OrderBy is null || query.OrderBy.Length > 8 ||
            query.Conditions is null || query.Conditions.Length > 8 || query.Match is not ("all" or "any"))
            throw new ArgumentException("Use up to 8 sorts, 8 conditions and match all/any.");

        var parameters = new List<(string Name, object Value)>();
        var predicates = query.Conditions.Select(condition => {
            if (condition is null) throw new ArgumentException("A condition cannot be null.");
            var column = Column(condition.Field);
            string Parameter(System.Text.Json.JsonElement value) {
                object converted;
                if (IsNumeric(condition.Field)) {
                    if (value.ValueKind != System.Text.Json.JsonValueKind.Number || !value.TryGetDecimal(out var quantity))
                        throw new ArgumentException("Quantity filters require a decimal number.");
                    converted = (double)quantity;
                } else {
                    if (value.ValueKind != System.Text.Json.JsonValueKind.String)
                        throw new ArgumentException("Text conditions require a string.");
                    var text = value.GetString()!;
                    if (text.Length > 200) throw new ArgumentException("Filter text is too long.");
                    converted = text;
                }
                var name = "$p" + parameters.Count;
                parameters.Add((name, converted));
                return name;
            }
            var first = Parameter(condition.Value);
            return condition.Comparison switch {
                "eq" => column + " = " + first,
                "gt" => column + " > " + first,
                "lt" => column + " < " + first,
                "between" => column + " BETWEEN " + first + " AND " + Parameter(condition.Upper),
                "contains" when !IsNumeric(condition.Field) => "instr(lower(" + column + "), lower(" + first + ")) > 0",
                "startsWith" when !IsNumeric(condition.Field) => "substr(lower(" + column + "), 1, length(" + first + ")) = lower(" + first + ")",
                _ => throw new ArgumentException("Unsupported comparison for this field.")
            };
        }).ToArray();
        var where = predicates.Length == 0 ? "" : " WHERE " + string.Join(query.Match == "all" ? " AND " : " OR ", predicates);
        var order = query.OrderBy.Select(sort => {
            if (sort is null || sort.Direction is not ("asc" or "desc"))
                throw new ArgumentException("Sort direction must be asc or desc.");
            return Column(sort.Field) + (sort.Direction == "desc" ? " DESC" : " ASC");
        }).ToList();
        order.Add("p.code ASC, w.id ASC, p.id ASC"); // Unique tie-breaker keeps page boundaries stable.

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(deferred: true); // Count and page share one snapshot.
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        command.CommandText = "SELECT COUNT(*)" + Tables + where;
        var total = checked((int)(long)(await command.ExecuteScalarAsync(cancellationToken))!);
        command.CommandText = """
            SELECT b.warehouse_id || ':' || b.product_id,
                   p.code, p.name, p.unit, w.name, b.on_hand, b.reserved, p.category
            """ + Tables + where
            + " ORDER BY " + string.Join(", ", order) + " LIMIT $take OFFSET $skip";
        command.Parameters.AddWithValue("$take", query.Take);
        command.Parameters.AddWithValue("$skip", query.Skip);
        var stocks = new List<StockRow>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken)) {
            while (await reader.ReadAsync(cancellationToken)) {
                cancellationToken.ThrowIfCancellationRequested();
                stocks.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4),
                    decimal.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(6), System.Globalization.CultureInfo.InvariantCulture),
                    reader.GetString(7)));
            }
        }
        transaction.Commit();
        return new StockPage(stocks.ToArray(), total);
    }

    // Only these trusted SQL identifiers can enter command text. Values are parameters.
    private static bool IsNumeric(string field) => field is "onHand" or "reserved" or "available";

    private static string Column(string field) => field switch {
        "id" => "(b.warehouse_id || ':' || b.product_id)",
        "productCode" => "p.code", "productName" => "p.name", "category" => "p.category",
        "unitCode" => "p.unit", "warehouseName" => "w.name",
        "onHand" => "CAST(b.on_hand AS REAL)", "reserved" => "CAST(b.reserved AS REAL)",
        "available" => "ROUND(CAST(b.on_hand AS REAL) - CAST(b.reserved AS REAL), 2)",
        _ => throw new ArgumentException("Unknown field: " + field)
    };
}

