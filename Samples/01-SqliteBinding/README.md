# 01 — SQLite binding

## Goal and quick start

Load warehouse data from SQLite into Grid. New topic: a local data session.

Run these commands from the PhialeTech.Grid.Samples.Wpf repository root. See README for prerequisites and exact dependency versions.

```powershell
dotnet run --project Samples/01-SqliteBinding/SqliteBinding.csproj -c Release
```

## Trace one case

```text
SQLite → SqliteStockRepository → StockRow[] → StockGridComposition → Grid
```

1. **SqliteStockRepository.cs** — Reads the selected number of records. Subsequent operations use an in-memory snapshot.
2. **StockGridComposition.cs** — Passes records to a local Grid session for filtering, sorting and paging.
3. **MainWindow.xaml** — Displays Columns and DataSession. Changing pages does not query SQLite again.

## Check your understanding

- Change the Symbol column width in the builder and inspect the result.
- Load 10,000 records, filter and switch pages. Find the database read and explain why paging does not repeat it.

Full file descriptions, configuration and scope follow below.


See the repository README for prerequisites and package versions.
