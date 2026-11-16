# 04 — Local SQLite editing

## Goal and quick start

Change one quantity and decide whether to discard it or persist it in SQLite. New topic: the edit buffer and explicit Save.

Run these commands from the PhialeTech.Grid.Samples.Wpf repository root. See README for prerequisites and exact dependency versions.

```powershell
dotnet run --project Samples/04-LocalEditing/LocalEditing.csproj -c Release
```

## Trace one case

```text
Cell → Grid buffer → Changes panel → Save → SqliteCommitPort → SQLite
```

1. **AssetColumns.cs / Quantity editor** — Choose a record, note quantity q and enter q + 1 (use 499 when q = 500). Enter posts to the buffer; SQLite is unchanged.
2. **PhialeChangePanel / DataSession** — Review before/after. Restore record discards its buffer; Discard changes discards all pending edits. Esc cancels the active unposted cell edit.
3. **SqliteCommitPort.cs / Save** — Only Save submits changes to a SQLite transaction. After success, the new value becomes the baseline and survives restart.

## Check your understanding

- Edit, press Enter and restore the record. Was SQLite updated? Find the actual UPDATE.
- Edit, save and restart. Edit again and discard. Explain which baseline is restored.

Full file descriptions, configuration and scope follow below.


24 synthetic asset records, four categories, seven editor kinds. No HTTP or server.
This example concentrates on edit buffering, review, discard and explicit Save.

~~~powershell
dotnet run --project Samples/04-LocalEditing/LocalEditing.csproj -c Release
~~~

Windows and .NET 8 SDK/Desktop Runtime are required, as in example 01. In Visual
Studio, select LocalEditing in Samples / 04 — Local editing. Grid NuGet packages
are used; no component source checkout or local NuGet feed is required.

## Data and editors

| Column | Type | Editor / rule |
| --- | --- | --- |
| Id | string | Read-only stable identity |
| Name | string | Text; required, max 80 characters |
| Category | string | Combo; one of four fixed categories |
| Quantity | int | Integer; 0–500 |
| UnitPrice | decimal | Decimal; 0–100000, max two fractional digits |
| InService | bool | CheckBox |
| NextInspection | DateTime? | DatePicker; date only, nullable, years 2000–2100 |
| Location | string | Autocomplete suggestions; custom locations accepted |
| Notes | string | Optional text, max 200 characters |

The UI uses Polish labels and pl-PL data culture, so type prices such as 1234,56.
There are zeros, empty dates/notes, checked and unchecked flags, and accented names.

## Where edits live

1. AssetRepository loads the small database once.
2. CreateSnapshotSessionAsync creates a Grid snapshot and working copies. The copy
   function row => row with { } isolates scalar record values from the loaded source.
   SnapshotReader only supplies those loaded rows; it does not use HTTP.
3. Typing edits the active cell. Enter/Tab/Apply posts a value to the in-memory
   edit session. It does not call SQLite. Grid tracks original and current values.
4. The Changes panel opens automatically and shows records and fields before/after.
   Changed cells and the pending counter use that same Grid editing state.
5. Save sends the entire ChangeSet to SqliteCommitPort. The application checks
   versions captured at load, validates values and executes parameterized UPDATEs
   in one transaction. Prices use integer cents, dates use ISO yyyy-MM-dd or NULL.
6. Only a successful SQLite commit is reported as success. Grid accepts a new
   snapshot baseline and clears pending change markers. Failure retains the buffer.

The snapshot API is independent of transport: here everything runs locally. The
application owns persistence; Grid owns the edit buffer and review presentation.
The plain CreateLocalSession helper commits to its in-memory source and alone does
not provide SQLite persistence. This example therefore supplies its own commit port.

## Cancel and restore

| Action | Result |
| --- | --- |
| Esc / Anuluj | Cancels the current unposted cell edit; earlier edits remain |
| Restore record in Changes | Discards that record's pending fields, preserving other rows |
| Odrzuć zmiany | Discards all pending edits back to the last accepted session baseline |
| Zapisz | Persists all pending edits, including filtered-out rows |

Discard does not undo a previous successful save. After Save, the new values are the
baseline. It also does not reload edits made externally by another process. There is
no autosave or persistent undo log. Closing with edits asks whether to save, discard
or stay; a crash loses unsaved memory. File errors and stale versions do not report
success. On conflict, discard and reopen to load the current database.

## Seed and working database

Data/local-editing.sqlite is a 12 KB seed, generated from readable Data/local-editing.sql.
The app copies it only on the first launch to:

~~~text
%LOCALAPPDATA%\PhialeTech\GridSamples\04-LocalEditing\assets.sqlite
~~~

The full working path is displayed above the Grid. Subsequent launches reuse it, so
saved changes survive restart. The seed and earlier warehouse database remain intact.
To deliberately reset the example, close all its windows, remove only this working
assets.sqlite yourself and reopen. The app then copies the seed again.

## Files

- AssetRow: typed model; its version is database concurrency metadata.
- AssetColumns: editor metadata and interactive validation.
- AssetGridComposition: snapshot load, record copies and commit port lifetime.
- AssetRepository: explicit SQLite reads and updates.
- SqliteCommitPort: version checks and atomic save outcomes.
- DemoDatabase: first-run working copy.
- MainWindow.xaml: built-in PhialeChangePanel and PhialeValidationPanel.
- MainWindow.xaml.cs: view lifetime, culture, opening the panel and close prompt.

## Verify

~~~powershell
dotnet run --project Tests/LocalEditing.Checks -c Release
dotnet run --project Tests/LocalEditing.WpfChecks -c Release
~~~

Tests use disposable database copies, never the user's working database. The native
check briefly creates an invisible non-activating WPF window and closes it. It tests
editor creation, posting, Changes visibility and actual Save/Discard toolbar commands.

This example updates existing rows only; add/delete are disabled. The snapshot limit
is 100 records, deliberately above the seed's 24. It does not demonstrate large-data
editing or restore an unsaved buffer after a crash.

The example source and synthetic data use MIT, as in LICENSE.md. Phiale component
packages retain their own licenses: https://phialetech.com/en/licensing/.

Column builder and MVVM: the examples use published Grid 2.0.0-preview.10 packages from nuget.org. No local package feed or other Phiale source repository is required. The repository NuGet.Config
remains nuget.org-only. GridColumnsBuilder derives ids/types from property expressions;
GridDataSource now accepts those descriptors directly, including editor metadata.

2026-11-16 — MVVM: ViewModel udostępnia GridContext<T> jako Grid; XAML wiąże Context. CurrentRecordId i SelectedRecordIds można odczytać w ViewModelu. Paczki preview.10 zostały opublikowane na NuGet.org 2026-11-17. Kompozycja zachowuje własność sesji. Kontekst należy odłączyć przed zamknięciem widoku.
