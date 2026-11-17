using System.Text.Json;
namespace PhialeTech.Grid.Samples.PriceReview.Contracts;

// These are the application's REST messages. The server does not reference Grid.
public sealed record ReviewScope(string Supplier, string Category, DateTime ImportedFrom, DateTime ImportedTo);
public sealed record PriceSearch(ReviewScope Scope, int Skip, int Take);
public sealed record PricePage(PriceRow[] Items, int TotalCount, long Revision);
public sealed record PriceChange(string Id, int ExpectedVersion, Dictionary<string, JsonElement> Fields);
public sealed record PriceCommit(string RequestId, ReviewScope Scope, PriceChange[] Changes);
public sealed record PriceOutcome(string Id, string Status, string Message, PriceRow? Current = null);
public sealed record PriceCommitResponse(PriceOutcome[] Rows);
public sealed record MarginPolicy(decimal MinimumMargin);
