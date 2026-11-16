using PhialeTech.Grid.Samples.Wpf.SqliteBinding.Models;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

// Server-owned REST DTOs. No Phiale.Grid package or Grid types on the server.
public sealed class GroupRequest
{
    public StockQuery Query { get; set; } = new();
    public StockOrder[] GroupBy { get; set; } = [];
    public string[] Collapsed { get; set; } = [];
    public bool ShowSummaries { get; set; } = true;

    public void Validate()
    {
        if (Query is null) throw new ArgumentException("query is required.");
        Query.Validate();
        if (GroupBy is null || GroupBy.Length > 2 || GroupBy.Any(g => g is null ||
            g.Field is not ("warehouseName" or "category") || g.Direction is not ("asc" or "desc")) ||
            GroupBy.Select(g => g.Field).Distinct().Count() != GroupBy.Length)
            throw new ArgumentException("Group by warehouseName and/or category once, with asc/desc direction.");
        if (Collapsed is null || Collapsed.Length > 4096 || Collapsed.Any(id => id is null || id.Length > 2048))
            throw new ArgumentException("Invalid collapsed group identifiers.");
    }
}

public sealed record GroupKey(string Field, string Value);
public sealed record Totals(int Count, long OnHandCents, long ReservedCents)
{
    public decimal OnHand => OnHandCents / 100m;
    public decimal Reserved => ReservedCents / 100m;
    public decimal Available => (OnHandCents - ReservedCents) / 100m;
}
public sealed record GroupRow(string Kind, int Level, string? Id = null,
    string? Field = null, string? Key = null, int Count = 0, bool Expanded = false,
    StockRow? Item = null, Totals? Totals = null, GroupKey[]? Path = null,
    int GlobalNumber = 0, int GroupNumber = 0);
public sealed record GroupPage(GroupRow[] Rows, int VisibleCount, int TotalItems,
    int RootCount, string[] GroupIds, Totals Totals);
public sealed record LocateRequest(string Target, GroupRequest Request);
public sealed record LocatedRow(int? Index);
