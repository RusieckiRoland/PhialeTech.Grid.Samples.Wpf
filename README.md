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
is required for a normal clone. NuGet.Config contains only nuget.org. By default, Grid packages use
2.0.0-preview.11; Phiale.Design.Wpf is 0.1.0-preview.48 and UniversalInput.Contracts
is 3.0.0-preview.3. Individual projects declare the additional SQLite/EF dependencies.
A machine-local LocalPackages.props, when present, overrides Grid and Design
for every sample, test and both build configurations; see Local binary candidates below.

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
| [08 — Nested Grid](Samples/08-NestedGrid/README.md) | Editable location master and asset detail; independent buffers and SQLite saves. | Complete local sets, separate buffered sessions. |

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

## Local binary candidates

All eight examples select their Grid and Design versions in Directory.Build.props.
Normal clones use the published versions from NuGet.org. For local verification,
an ignored LocalPackages.props can select unpublished versions and add the
artifacts/local-feed directory to restore. Local candidates are optional and are never required by a fresh clone.
The samples continue to use PackageReference, without references to private
Grid or Design source projects. Removing the local override restores the normal
published package selection.

For an explicit public-package check, pass -p:UseLocalPhialePackages=false.
The current release selects Grid 2.0.0-preview.12 and Design 0.1.0-preview.49.

## Public release verification — 2026-11-19

Grid preview.12 and Design preview.49 were restored exclusively from NuGet.org into a fresh cache. All eight samples build in Debug and Release with zero warnings/errors. Nine check runners passed, plus Immediate and close-save/close-cancel cases. Actual dependency outputs and six Phiale DLL hashes match the published protected Grid and unobfuscated Design packages in all sixteen outputs. The author requested these scenarios and publication; AI implemented and verified the changes. Evidence stays in ignored artifacts/release-public-preview12. Fresh Windows VM verification is a separate step.

Windows VM verification: fresh public clone1bdb865a35df26a7c9806da90078ea28464417c4, new cache, all eight outputs resolved Grid12/Design49. All nine data and native WPF runners passed; the source database was unchanged. Reports are kept privately under the host VM test configuration. Guest sleep interrupted report transfer; the complete passing report was recovered and the collector transfer guard was corrected.

The three standalone HTTP/grouping/local-edit data runners now consume the shared PhialeGridVersion property as well; their previous explicit preview.11 pins were detected during the Avalonia port. All three were rerun successfully on preview.12 on the host. This does not change the eight applications or the author's current local XAML edits.

Follow-up VM run grid-preview12-data-current-20261119: a fresh clone of 1e4235e, restore, build and all five standalone data runners passed after removing their old explicit package pins. Eight application outputs again verified Grid12/Design49. This complements the previously accepted native WPF run; those native checks were not rerun in this data-only follow-up.
