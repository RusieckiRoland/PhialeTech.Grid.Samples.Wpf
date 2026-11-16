# 03 — Server-side grouping with REST, EF Core and SQLite

## Goal and quick start

Expand a warehouse and category, then navigate to the group summary. The addition to 02: responses also contain headers and summaries.

Run these commands from the PhialeTech.Grid.Samples.Wpf repository root. See README for prerequisites and exact dependency versions.
Run server and client in two separate terminals.

```powershell
dotnet run --project Samples/03-ServerGrouping/Server/ServerGrouping.Server.csproj -c Release
dotnet run --project Samples/03-ServerGrouping/Client/ServerGrouping.csproj -c Release
```

## Trace one case

```text
Grid grouping → API → SQLite aggregates → group layout → page of rows → Grid
```

1. **GroupedStocksEndpoint.cs** — Sends grouping fields and collapsed state. The response distinguishes group, record and summary.
2. **GroupCatalog.cs / GroupLayout.cs** — Aggregates describe counts and sums of whole groups. Layout locates headers, records and summaries without creating a million rows.
3. **GroupedStockProvider.cs / api/stocks/locate** — The group-end command locates the group summary. The client fetches its page.

## Check your understanding

- Collapse and expand a category. Explain which rows disappear and why the group count is unchanged.
- Navigate to a summary on another page. Explain why its sum covers the whole group.

Full file descriptions, configuration and scope follow below.


This independent WPF client and ASP.NET Core server extend lesson 02. They use the
existing Data/warehouse-1000000.sqlite from lesson 01. Run git lfs pull first.
No new database, seed or Phiale server dependency is needed.

From the repository root, start these in separate terminals:

~~~powershell
dotnet run --project Samples/03-ServerGrouping/Server/ServerGrouping.Server.csproj -c Release
dotnet run --project Samples/03-ServerGrouping/Client/ServerGrouping.csproj -c Release
~~~

In Visual Studio, the 03 solution folder contains the client and server. Start
ServerGrouping.Server with the EF server grouping profile, then ServerGrouping.Client.
The server uses http://127.0.0.1:5188; demo 02 keeps port 5187. Requires .NET 8 Desktop
Runtime for the client and .NET 8 SDK for building. Newer SDK alone does not supply it.

## Follow the request

1. MainWindow sets Warehouse → Category and four summaries before BindRuntime.
2. GroupedStocksEndpoint maps Grid state to server-owned JSON: query, groupBy,
   collapsed and showSummaries. Headers and footers count as visible page rows.
3. GroupedStockRepository applies filters and asks SQLite to GROUP BY warehouse
   and category, count and sum all filtered records. This returns 16 combinations
   for this million-row database, not a million materialized records.
4. GroupCatalog creates the small hierarchy. GroupLayout calculates header,
   record-block and footer positions from counts and expansion state.
5. The repository intersects those blocks with skip/take, then fetches only the
   required data records using EF Where / OrderBy / Skip / Take. Each response
   uses one read transaction, so its aggregates and records share a snapshot.
6. GroupedStockProvider connects IGridGroupLocationProvider to /api/stocks/locate.
   Go to end of group obtains the footer index and loads that page without
   walking through all member records. Group summaries include the complete path.

The server links EF mapping, filter and sorting source files from demo 02 as
ExistingEf. These appear in Visual Studio; there is no second implementation or
additional shared library project. GroupContract defines the independent API DTOs.
The client alone references Grid NuGet packages. Published packages are unchanged.

## Try it

Expand warehouses and categories, switch pages, filter Category, change sort order,
collapse/reopen groups, reverse the two group levels and remove grouping. Right-click
a group to go to its summary. Counts and sums cover the entire filtered group,
including members on other pages. Global record numbers include collapsed records;
numbers within the lowest group restart at 1.

## Deliberate scope

Read only; at most two grouping fields, warehouseName and category, in either order
and direction. Other grouping fields are rejected with HTTP 400. Four fixed
summaries: count, OnHand, Reserved and Available. Integer hundredths preserve the
two-decimal quantities used by this database; adapt the numeric model for other
precision requirements. Small group cardinality is assumed. No write, export,
arbitrary record location, authentication or authorization endpoint is implemented.
Bearer support and server authorization are explained in lesson 02. Changes between
separate requests require an application-specific version/snapshot strategy.

The aggregate scan is repeated on requests to keep the example straightforward.
LIMIT/OFFSET avoids transferring the full dataset but does not guarantee constant
query time. Measure real workloads before choosing indexes or a cache policy.

## Verify

~~~powershell
dotnet run --project Tests/ServerGrouping.Checks -c Release
dotnet run --project Tests/ServerGrouping.WpfChecks -c Release
~~~

Checks start and stop their own servers. The WPF check briefly creates an invisible,
non-activating window and closes it; it needs port 5188 free. Integration checks use
an ephemeral port, independent SQL totals, real HTTP and the packaged Core/adapter.
The existing database's SHA256 must remain unchanged.

The sample source and data use the repository MIT license. Phiale components retain
their separate evaluation/commercial licensing; see https://phialetech.com/en/licensing/.

2026-11-16 — MVVM: ViewModel udostępnia GridContext<T> jako Grid; XAML wiąże Context. CurrentRecordId i SelectedRecordIds można odczytać w ViewModelu. Paczki preview.10 zostały opublikowane na NuGet.org 2026-11-17. Kompozycja zachowuje własność sesji. Kontekst należy odłączyć przed zamknięciem widoku.
