using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Query;
using PhialeGrid.Core.Summaries;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
namespace PhialeTech.Grid.Samples.Wpf.RestSnapshot.Infrastructure;

// Translate Grid's vocabulary to this application's HTTP messages in one place.
public sealed class PriceEndpoint(ReviewScope scope)
{
    private long? _downloadRevision;
    private string _changeSetId = "";
    public PriceCommitResponse? LastResponse { get; private set; }
    public GridHttpDataOptions<PriceRow> Options(string address) => new(address, "api/price-reviews/search")
    {
        CommitPath = "api/price-reviews/commit",
        MapQueryRequest = q => new PriceSearch(scope, q.Offset, q.Size),
        ReadQueryResponse = ReadPage,
        MapCommitRequest = WriteChanges,
        ReadCommitResponse = ReadSavedChanges
    };
    private GridQueryResult<PriceRow> ReadPage(string json, JsonSerializerOptions options)
    {
        var page = JsonSerializer.Deserialize<PricePage>(json, options) ?? throw new JsonException("Brak strony cennika.");
        if (_downloadRevision.HasValue && _downloadRevision != page.Revision)
            throw new InvalidOperationException("Cennik zmienił się w trakcie pobierania. Pobierz cały snapshot ponownie.");
        _downloadRevision = page.Revision;
        return new(page.Items, page.TotalCount, null, GridSummarySet.Empty);
    }
    private PriceCommit WriteChanges(ChangeSet changes)
    {
        LastResponse = null; _changeSetId = changes.ChangeSetId;
        if (changes.Changes.Any(c => c.Operation != ChangeOperation.Update)) throw new InvalidOperationException("Ten przykład edytuje istniejące pozycje.");
        var rows = changes.Changes.OrderBy(c => c.TargetId, StringComparer.Ordinal).Select(c => new PriceChange(c.TargetId,
            int.Parse(c.VersionToken, CultureInfo.InvariantCulture), c.FieldChanges.OrderBy(f => f.FieldName, StringComparer.Ordinal)
                .ToDictionary(f => f.FieldName, f => JsonSerializer.SerializeToElement(f.NewValue)))).ToArray();
        // Same version + same changes => same request ID. A retry after a lost response is safe.
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { scope, rows }))));
        return new(id, scope, rows);
    }
    private GridCommitResult ReadSavedChanges(string json, JsonSerializerOptions options)
    {
        LastResponse = JsonSerializer.Deserialize<PriceCommitResponse>(json, options) ?? throw new JsonException("Brak wyniku zapisu.");
        // Return exactly the active Grid ChangeSet id; the server uses its own idempotency key.
        return new(_changeSetId, LastResponse.Rows.Select(r => new GridRecordCommitResult(r.Id,
            r.Status == "Saved" ? GridRecordCommitStatus.Succeeded : r.Status == "Conflict" ? GridRecordCommitStatus.VersionConflict : GridRecordCommitStatus.BusinessRejected,
            r.Current?.Version.ToString(CultureInfo.InvariantCulture), r.Status == "Saved" ? Values(r.Current!) : null, message: r.Message)).ToArray());
    }
    private static IReadOnlyDictionary<string, object> Values(PriceRow row) => new Dictionary<string, object>
    {
        ["ProposedSalePrice"] = row.ProposedSalePrice,
        ["ValidFrom"] = row.ValidFrom,
        ["Decision"] = row.Decision,
        ["Comment"] = row.Comment
    };
}
