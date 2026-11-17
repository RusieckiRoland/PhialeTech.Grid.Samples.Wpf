# 02 — Local data virtualization

Same warehouse model, columns and read-only database as example 01, but no ReadAll.
The Grid asks StockQuerySource.QueryAsync for Offset/Size; EF/SQLite selects only
that window. Grouped windows use QueryGroupedAsync. Repository calls run away
from the UI thread. The existing EF queries are linked from examples 03/04,
so there is no duplicate SQL engine or HTTP server in this application.

```powershell
dotnet run --project Samples/02-DataVirtualization/DataVirtualization.csproj -c Release
```

Choose the logical dataset scope (10,000 / 100,000 / 1,000,000), then Load snapshot
to reconnect the source. This does not download the selected set. Scroll to fetch
windows. Enable paging in Grid options to use the same QueryAsync via page buttons.
Default: EnableDataVirtualization=True, IsPagingEnabled=False, DataPageSize=100.

## Read the example in order

1. Composition/StockGridComposition.cs: Columns/Key/Clone/Query/Build, no initial I/O.
2. Infrastructure/StockQuerySource.cs: receive a window, map filters and return TotalCount.
3. Infrastructure/StockRepository.cs: execute actual SQLite queries, off the UI thread.
4. Infrastructure/Queries: shared EF Where/Count/OrderBy/Skip/Take implementations.
5. Views/MainWindow.xaml: bind the session, enable data virtualization.

## Scope

Read-only. Filters/sorts use the operations supported by the sample queries;
grouping supports WarehouseName and Category. Group totals use a fixed Count/Sum
set. Arbitrary custom summaries, full exports, and edits are not demonstrated.
Unlike example 01, source-side operations need explicit query implementation.
Both examples retain UI virtualization; only this one reads data on demand.

The example source uses MIT. Phiale dependencies retain their own licenses.
