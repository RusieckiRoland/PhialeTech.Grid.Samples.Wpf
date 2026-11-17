# PhialeTech.Grid.Samples.Wpf

Eight focused WPF examples using Phiale.Grid NuGet packages and MVVM.
The tutorial sequence separates full in-memory work from real on-demand reads.

## Requirements and first run

Windows 10/11, Git with Git LFS, .NET 8 SDK (or newer), and Windows Desktop Runtime 8.
A newer SDK can build these net8.0 applications; the .NET 8 desktop runtime is still required to run them.

```powershell
git lfs install
git lfs pull
dotnet run --project Samples/01-SqliteBinding/SqliteBinding.csproj -c Release
```

Open PhialeTech.Grid.Samples.Wpf.sln in Visual Studio. Each numbered example is a
separate application. No other Phiale source repository or private package feed
is required. NuGet.Config contains only nuget.org. Grid packages are pinned to
2.0.0-preview.11; Phiale.Design.Wpf is 0.1.0-preview.48 and UniversalInput.Contracts
is 3.0.0-preview.3. Individual projects declare the additional SQLite/EF dependencies.

## Choose an example

| Example | What it demonstrates | Data strategy |
| --- | --- | --- |
| [01 — SQLite](Samples/01-SqliteBinding/README.md) | Load a selected 10k/100k/1m set; columns, filtering, sorting, grouping and local pages. | Entire selected set in memory, read-only. |
| [02 — Data virtualization](Samples/02-DataVirtualization/README.md) | Same warehouse data; QueryAsync receives Offset/Size and reads actual local windows. | Local source on demand; scrolling or paging through the same source. |
| [03 — HTTP REST](Samples/03-HttpRest/README.md) | Request/response mapping, EF/SQLite reads, server filters/sorts/pages. | REST on demand, read-only. |
| [04 — Server grouping](Samples/04-ServerGrouping/README.md) | Warehouse/category groups, full-group totals and navigation to group end. | REST grouped windows, read-only. |
| [05 — Local editing](Samples/05-LocalEditing/README.md) | 24 varied assets, seven editors, pending changes, discard and atomic file save. | Complete local set, buffered edits, explicit save. |
| [06 — REST snapshot](Samples/06-RestSnapshot/README.md) | Select supplier price batch, edit locally, save through REST with version conflicts and server validation. | Complete selected REST snapshot, local work, remote commit. |
| [07 — Row details](Samples/07-RowDetails/README.md) | Application-owned UserControl inside an expanded row. | Read-only detail presentation. |
| [08 — Nested Grid](Samples/08-NestedGrid/README.md) | Full child Grid with independent filtering and selection. | Read-only parent/child presentation. |

UI virtualization prepares only the needed presentation area in every example.
Data virtualization is separate: example 02 reads needed windows instead of first
loading all records. In example 01, page buttons divide a collection already in memory.
In example 02, page buttons and scrolling both ask the same query source for windows.

## Read the composition

Records(rows) supplies a complete loaded collection. Query(provider) connects a
reader whose QueryAsync is called on demand. Snapshot(provider, maxRecords)
downloads a complete selected set before local work. Columns, Key and Clone describe
metadata, identity and working copies. Build creates Records/Query sessions;
BuildAsync downloads a Snapshot. CommitUsing explicitly connects persistence.

The composition owns and disposes the session. CreateDataBinding exposes that same
session to the control; it does not create a second session or load records.
ViewModels expose the binding and, where required, GridContext<T> for observable
selection, pending changes, validation and commands. Dispose the context before
closing its session/runtime. SelectedRecordIds and CheckedRecordIds are distinct;
SelectedItems contains only available loaded records and never fetches missing pages.

## Verify

```powershell
dotnet build PhialeTech.Grid.Samples.Wpf.sln -c Release
dotnet run --project Tests/SqliteBinding.Checks -c Release
dotnet run --project Tests/DataVirtualization.Checks -c Release
dotnet run --project Tests/DataVirtualization.WpfChecks -c Release
dotnet run --project Tests/HttpRest.Checks -c Release
dotnet run --project Tests/ServerGrouping.Checks -c Release
dotnet run --project Tests/ServerGrouping.WpfChecks -c Release
dotnet run --project Tests/LocalEditing.Checks -c Release
dotnet run --project Tests/LocalEditing.WpfChecks -c Release
dotnet run --project Tests/MoreDemos.WpfChecks -c Release
```

HTTP integration checks start and stop their own servers. Native checks create
non-activating test windows and close them. Edit checks use temporary database copies.
Data/warehouse-1000000.sqlite is synthetic, read-only source data stored in Git LFS;
download its full contents, not just the pointer. Source-dependent query operations
and limits are explained in each example; they are not universally supplied by Grid.

Tutorials: https://phialetech.com/en/tutorials/ (PL/EN/DE/FR).

## Licensing

Example source and synthetic data are MIT: see LICENSE.md. Phiale packages keep
their own licenses. MIT for these examples does not grant a commercial Phiale
license. Evaluation terms and the free individual commercial-license application:
https://phialetech.com/en/licensing/.

## Previous numbering

The new local-virtualization example occupies 02. The former 02–07 are now 03–08;
project names, namespaces and server ports remain stable. Earlier working README
notes are preserved in docs/History and do not describe the current dependency set.
