# 05 — Local SQLite editing

## Read the three record-definition lines

- `.Columns(AssetColumns.All)` supplies field definitions, headers, editors and
  validation rules from AssetColumns. It does not fetch rows from SQLite.
- `.Key(row => row.Id)` supplies a stable, unique identity. Grid uses it to match
  records and pending edits after sorting or filtering; it is not a row number.
- `.Clone(row => row with { })` supplies the function that creates an editable
  working copy. **AssetRow is a record**, which supports `with { }`. This is a
  shallow copy: nested mutable objects would still be shared and must be copied
  separately. This demo has strings and value-type fields, so no mutable nested
  object needs copying. **Clone itself does not require a record**: for an ordinary
  class, supply your own copy function, for example through its copy constructor.

## Goal and quick start

Change one quantity and decide whether to discard it or persist it in SQLite. New topic: the edit buffer and explicit Save.

Run these commands from the PhialeTech.Grid.Samples.Wpf repository root. See README for prerequisites and exact dependency versions.

```powershell
dotnet run --project Samples/05-LocalEditing/LocalEditing.csproj -c Release
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
dotnet run --project Samples/05-LocalEditing/LocalEditing.csproj -c Release
~~~

Windows and .NET 8 SDK/Desktop Runtime are required, as in example 01. In Visual
Studio, select LocalEditing in Samples / 04 — Local editing. Grid NuGet packages
are used. The local builder draft currently requires the candidate feed described above.

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
2. `Records(rows).CommitUsing(port).Build()` creates the session directly from the
   complete list. `row => row with { }` supplies working copies of the record.
   There is no SnapshotReader, provider query, loading page, or MaxRecords setting.
3. Typing edits the active cell. Enter/Tab/Apply posts a value to the in-memory
   edit session. It does not call SQLite. Grid tracks original and current values.
4. The Changes panel opens automatically and shows records and fields before/after.
   Changed cells and the pending counter use that same Grid editing state.
5. Save sends the entire ChangeSet to SqliteCommitPort. The application checks
   versions captured at load, validates values and executes parameterized UPDATEs
   in one transaction. Prices use integer cents, dates use ISO yyyy-MM-dd or NULL.
6. Only a successful SQLite commit is reported as success. Grid accepts a new
   accepted baseline and clears pending change markers. Failure retains the buffer.

The application reads the complete small dataset once. Grid owns working copies,
the edit buffer and review presentation. Only the supplied commit port writes to
SQLite. All rows are presented without paging; sorting and filtering remain local.

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
%LOCALAPPDATA%\PhialeTech\GridSamples\05-LocalEditing\assets.sqlite
~~~

The full working path is displayed above the Grid. Subsequent launches reuse it, so
saved changes survive restart. The seed and earlier warehouse database remain intact.
To deliberately reset the example, close all its windows, remove only this working
assets.sqlite yourself and reopen. The app then copies the seed again.

## Files

- AssetRow: typed model; its version is database concurrency metadata.
- AssetColumns: editor metadata and interactive validation.
- AssetGridComposition: complete local records, working copies and commit port lifetime.
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

This example updates existing rows only; add/delete and paging are disabled.
It reads all 24 records into memory and does not demonstrate large-data loading.
There is no paging provider or artificial loading limit, and no recovery of an
unsaved buffer after a crash.

The example source and synthetic data use MIT, as in LICENSE.md. Phiale component
packages retain their own licenses: https://phialetech.com/en/licensing/.

Column and data builders use the pinned preview.11 packages from nuget.org.
No private builder feed is required. The composition owns the session.

2026-11-16 — MVVM: ViewModel udostępnia GridContext<T> jako Grid; XAML wiąże Context. CurrentRecordId i SelectedRecordIds można odczytać w ViewModelu. Paczki preview.10 zostały opublikowane na NuGet.org 2026-11-17. Kompozycja zachowuje własność sesji. Kontekst należy odłączyć przed zamknięciem widoku.

## Dwa sposoby zapisu

Przed edycją wybierz Zapis buforowany albo Zapis natychmiastowy. ViewModel ustawia
Grid.SaveMode na GridSaveMode.Buffered/Immediate. Nie zmienia repozytorium ani
portu zapisu. W pierwszym trybie Enter/Zastosuj zostawia zmiany do wspólnego
Zapisz/Odrzuć. W drugim ta sama ścieżka zapisu jest uruchamiana po zatwierdzeniu
edycji, a oba przyciski bufora są ukryte. Walidacja sprawdza cały rekord.
Trybu nie przełączamy przy trwającej edycji/zapisie ani z niezapisanymi zmianami.
Odrzucenie zapisu nie usuwa bufora i nie powoduje pętli ponowień; popraw rekord,
cofnij jego zmianę albo jawnie ponów zapis z aplikacji. Nowy kandydat jest lokalny,
nieopublikowany. HideUnavailableMenuItems=True w widoku pomija niedostępne pozycje
menu Elementy. Ramki grup paska usunięto; przy braku miejsca zostają ikony z podpowiedziami.
