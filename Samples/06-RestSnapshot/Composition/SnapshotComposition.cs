using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Data;
using PhialeGrid.Core.Query;
using PhialeGrid.Data.Http;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
using PhialeTech.Grid.Samples.Wpf.RestSnapshot.Infrastructure;
namespace PhialeTech.Grid.Samples.Wpf.RestSnapshot.Composition;

public sealed class SnapshotComposition : IDisposable
{
    private readonly RequestCounter _counter = new();
    private readonly HttpClient _http;
    private GridDataSession<PriceRow>? _current;
    public SnapshotComposition() => _http = new(_counter) { Timeout = TimeSpan.FromSeconds(60) };
    public int RequestCount => _counter.Count;
    public PriceEndpoint? Endpoint { get; private set; }
    public async Task<GridDataSession<PriceRow>> DownloadAsync(IReadOnlyList<IGridColumnDescriptor> columns, ReviewScope scope,
        CancellationToken token, string address = "http://127.0.0.1:5189")
    {
        var endpoint = new PriceEndpoint(scope);
        var provider = GridHttpDataSourceExtensions.CreateQueryProvider(_http, endpoint.Options(address), columns);
        var session = await GridDataBuilder.For<PriceRow>()
            .Columns(columns).Key(row => row.Id).Clone(row => row with { })
            .Version(row => row.Version.ToString(CultureInfo.InvariantCulture),
                (row, version) => row.Version = int.Parse(version, CultureInfo.InvariantCulture))
            .CacheSize(10000)
            .Snapshot(provider, maxRecords: 10000,
                initialQuery: new GridQueryRequest(0, 1000, [], new GridFilterGroup([], GridLogicalOperator.And), null, null))
            .CommitUsing(provider) // Only explicit Save sends the pending batch to REST.
            .BuildAsync(token);
        Endpoint = endpoint; return session;
    }
    public GridDataBinding Use(GridDataSession<PriceRow> next) { _current?.Dispose(); _current = next; return GridApiFactory.CreateDataBinding(next); }
    public async Task SetMarginPolicyAsync(string address, int value, CancellationToken token)
    {
        using var response = await _http.PutAsJsonAsync(address.TrimEnd('/') + "/api/demo/policy", new MarginPolicy(value), token);
        response.EnsureSuccessStatusCode();
    }
    public void Dispose() { _current?.Dispose(); _http.Dispose(); }
    private sealed class RequestCounter : DelegatingHandler
    {
        private int _count;
        public RequestCounter() : base(new HttpClientHandler()) { }
        public int Count => Volatile.Read(ref _count);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref _count); return base.SendAsync(request, token);
        }
    }
}

