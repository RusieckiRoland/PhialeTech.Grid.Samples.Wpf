# PhialeTech.Grid.Samples.Wpf

Small, focused WPF examples using MVVM and published Phiale.Grid packages.

## Requirements

Windows 10/11, Git with Git LFS, and .NET 8 SDK (includes Windows Desktop Runtime 8).
A newer SDK can build this net8.0 project, but running it still requires Windows Desktop Runtime 8.
No other Phiale repositories or local package feeds are needed.

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
| Views/MainWindow.xaml | Binds Columns and DataSession to PhialeGrid |
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

- PhialeTech.Grid.Contracts, PhialeTech.Grid.Core, PhialeTech.Grid.Wpf: 2.0.0-preview.7
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
