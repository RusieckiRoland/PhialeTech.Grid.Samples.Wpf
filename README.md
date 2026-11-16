# PhialeTech.Grid.Samples.Wpf

Small, focused WPF examples using MVVM and Phiale.Grid NuGet packages.

## Requirements

Windows 10/11, Git with Git LFS, and .NET 8 SDK (includes Windows Desktop Runtime 8).
A newer SDK can build this net8.0 project, but running it still requires Windows Desktop Runtime 8.
No other Phiale source repositories are needed. Grid preview.10 dependencies are published on nuget.org.

## Run

Execute these commands inside the cloned repository:

```powershell
git lfs install
git lfs pull
dotnet run --project Samples/01-SqliteBinding/SqliteBinding.csproj -c Release
```

In Visual Studio, open `PhialeTech.Grid.Samples.Wpf.sln` and run `SqliteBinding`.
No command-line arguments are required. If upgrading an older checkout, run `git pull --ff-only` first.

## 01: SQLite binding

The example loads a read-only snapshot from `Data/warehouse-1000000.sqlite`.
Choose 10,000, 100,000 or 1,000,000 stock balances to compare behavior.
The database is stored in Git LFS; downloading only its pointer is insufficient.
Data is synthetic and represents products, categories, warehouses and quantities.

| Location under Samples/01-SqliteBinding | Responsibility |
| --- | --- |
| Models/StockRow.cs | One product balance in a warehouse; Available = OnHand - Reserved |
| Models/IStockRepository.cs | Data access contract |
| Infrastructure/SqliteStockRepository.cs | Read-only SQLite query and explicit mapping to StockRow |
| ViewModels/StocksViewModel.cs | Column builder, loaded rows, commands and loading status |
| Composition/StockGridComposition.cs | Creates and owns the Grid local data session |
| Configuration/ReadOnlySnapshotPolicy.cs | Available Grid elements for this example |
| Views/MainWindow.xaml | Binds Columns and Context to PhialeGrid |
| App.xaml | Imports Phiale.Design and Grid resources |
| App.xaml.cs | Application composition and resource lifetime |

`GridColumnsBuilder<StockRow>` connects columns to model properties, for example
`.Add(row => row.ProductCode, "Symbol", 130)`. The repository explicitly assigns SQL
fields to those properties. The ViewModel supplies the collection through a local
Grid data session. Sorting, filtering, grouping and paging use the loaded snapshot;
they do not send further SQL queries. Editing is disabled.

This example deliberately reads the chosen dataset into memory. It is not SQL paging
or a REST example. Compare cold and warm runs separately; results depend on hardware.

## Packages

- PhialeTech.Grid.Core: 2.0.0-preview.10 (published MVVM preview)
- PhialeTech.Grid.Contracts: 2.0.0-preview.10 (published preview); PhialeTech.Grid.Wpf: 2.0.0-preview.10
- Phiale.Design.Wpf: 0.1.0-preview.48
- UniversalInput.Contracts: 3.0.0-preview.3
- Microsoft.Data.Sqlite.Core: 10.0.15
- SQLitePCLRaw.bundle_e_sqlite3: 3.0.5

NuGet.Config uses only nuget.org. No credentials for private Phiale sources are required.

## Checks

```powershell
dotnet run --project Tests/SqliteBinding.Checks -c Release
```

Checks cover SQLite mapping, model identities, column definitions, ViewModel
notifications, cancellation and local Grid session behavior.

## Licensing

The example source and generated demo data use MIT; see LICENSE.md.
Dependencies retain their own licenses. MIT for this example does not grant a
commercial license to Phiale components; see https://phialetech.com/en/licensing/.

## Update: 2026-11-15

The author requested a standalone clean-Windows test using published NuGet dependencies.
The agent replaced source project references and the local Design feed with package
references, retained MIT for the sample and updated these instructions.

## 02: HTTP REST binding

A small independent WPF MVVM client and a local REST server reading the same Data/warehouse-1000000.sqlite as example 01.
The ASP.NET Core server queries the existing SQLite database using EF Core by default, with a direct SQL comparison profile. It has no Phiale dependencies.
The WPF client uses PhialeTech.Grid.Data.Http and maps Grid requests to the independent API. See [the step-by-step guide](Samples/02-HttpRest/README.md).

```powershell
# Terminal 1
dotnet run --project Samples/02-HttpRest/Server/HttpRest.Server.csproj -c Release
# Terminal 2
dotnet run --project Samples/02-HttpRest/Client/HttpRest.csproj -c Release
# Integration checks start their own server
dotnet run --project Tests/HttpRest.Checks -c Release
```

## 03: Server-side grouping

A separate WPF client and EF Core REST server use the same SQLite database.
Warehouse/Category groups, server paging and full-group summaries are explained in
[lesson 03](Samples/03-ServerGrouping/README.md). Demo 02 remains the introductory REST example.

## 04: Local editing and saving

A new 24-row SQLite dataset demonstrates seven editor kinds, working copies,
change review, restoring a record, discarding pending edits and explicit atomic Save.
See [lesson 04](Samples/04-LocalEditing/README.md). Saved data stays in a persistent
local working copy; the seed and earlier warehouse database remain unchanged.

Column builder and MVVM: the examples use published Grid 2.0.0-preview.10 packages from nuget.org. No local package feed or other Phiale source repository is required. The repository NuGet.Config
remains nuget.org-only. GridColumnsBuilder derives ids/types from property expressions;
GridDataSource now accepts those descriptors directly, including editor metadata.

## Learning paths

Each lesson starts with its goal, run commands and a concrete data path, then ends
with a small modification or explanation task. See the guides:
[01 SQLite](Samples/01-SqliteBinding/README.md),
[02 HTTP REST](Samples/02-HttpRest/README.md),
[03 server grouping](Samples/03-ServerGrouping/README.md),
[04 local editing](Samples/04-LocalEditing/README.md).
In lesson 02, CreateServerRequest and ReadServerResponse expose the two translation
directions. StockSearchRequest describes this server's JSON; it is sample application
code, not a new Grid API. Existing package versions and server routes are unchanged.

## Observable MVVM context — preview.10

The application composition creates GridContext<T> over the existing runtime and
data session. Each ViewModel exposes Grid; XAML binds Context. CurrentRecordId,
SelectedRecordIds and CheckedRecordIds have separate meanings. SelectedItems only
contains loaded, available objects; it never fetches hidden server pages.

The local-editing sample exposes SaveCommand and DiscardCommand outside the Grid,
observes pending edits directly, and supplies record and asynchronous batch rules.
SQLite repeats business checks inside the transaction. Dispose the context before
closing the view; composition still owns the data session.

All Grid preview.10 packages are published on nuget.org. The repository NuGet.Config uses only nuget.org. Restore and build were verified with a fresh package cache after publication.

2026-11-16: the author requested a convenient MVVM contract; the agent implemented
the approved plan in the component, migrated examples 01–04 and tested protected
NuGet consumers. No commit, push or publication was performed.

2026-11-17: the author approved publishing preview.10 with the performance gate open. NuGet.org accepted all seven Grid packages; public availability and package-only restore were verified. No source commit, push or website deployment is implied.

2026-11-17: the author requested committing and pushing all current demo updates so the public repository matches the deployed documentation. The agent prepared the commit using the author’s configured Git identity. Examples 01–04 use published preview.10 packages; the source remains MIT.
