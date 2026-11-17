# 06 — Review a supplier price list: REST → snapshot → local edits → commit

The operator reviews 10,000 imported supplier prices, proposes sale prices and decides which lines to approve or reject. A selection form appears **before** the table. This is a separate WPF client and a normal ASP.NET Core / EF Core / SQLite server, with no Phiale dependency on the server.

## Run

From the sample repository root, run `git lfs pull` once to obtain the existing warehouse catalog. Start these two independent projects (two terminals, or both as startup projects in Visual Studio):

```powershell
# Terminal 1 — wait for “Now listening” on port 5189
 dotnet run --project Samples/06-RestSnapshot/Server/PriceReview.Server.csproj -c Release
# Terminal 2 — selection form and editable Grid
 dotnet run --project Samples/06-RestSnapshot/RestSnapshot.csproj -c Release
```

Choose Nord or Baltic, an optional category, and the import date range. The default range covers all 10,000 rows of that supplier on the first run. Click **Pobierz do weryfikacji**. An empty category means all categories. The table then works on the complete downloaded result, not one server page.

## The five things to follow

| File | Responsibility | Why it exists |
| --- | --- | --- |
| [Contracts/PriceRow.cs](Contracts/PriceRow.cs), [PriceMessages.cs](Contracts/PriceMessages.cs) | Product prices, scope, search and save messages | These are this application's data and REST vocabulary, not a server requirement imposed by Grid. |
| [Configuration/PriceColumns.cs](Configuration/PriceColumns.cs), [PriceRules.cs](Configuration/PriceRules.cs) | Editors and local rules | Product identity and purchase prices are read-only; sale proposal, date, decision and comment are editable. |
| [Infrastructure/PriceEndpoint.cs](Infrastructure/PriceEndpoint.cs) | Translate query / ChangeSet and read the response | The server receives only edited fields and their expected record versions. |
| [Composition/SnapshotComposition.cs](Composition/SnapshotComposition.cs) | Own HttpClient and data session | Download in 1,000-row windows, enforce a 10,000-row limit, then use local filtering/sorting/paging. Commit still goes through HTTP. |
| [ViewModels/PriceReviewViewModel.cs](ViewModels/PriceReviewViewModel.cs), [Views/MainWindow.xaml](Views/MainWindow.xaml) | Expose the MVVM context and application commands | XAML binds Grid.Context; the ViewModel sees pending edits, checked IDs, validation and Save results. |

The project name RestSnapshot describes the lesson data-loading mode. The warehouse balance server from lesson 02 is not used.

## Where the data is

The server reads product names, codes and categories from the existing `Data/warehouse-1000000.sqlite` **read-only**. It creates its own working database at `%LOCALAPPDATA%/PhialeTech.Grid.Samples/06-PriceReview/pricing.sqlite`, with two suppliers and 10,000 price lines each. Demo prices and inactive-product flags are deterministic test data. Money is stored in integer cents, and rows have an integer Version.

Import dates are assigned on the first server run and remain persisted. On a later day, adjust the selection's date range to include that original import. To reset the exercise, stop the server and remove only its own pricing.sqlite working database (never the warehouse catalog). The server prints the absolute database path.

## Local buffer and explicit Save

1. Double-click / F2 edits a cell. Enter or Tab accepts it into the local buffer; Esc cancels the current editor.
2. Filters, sorting and page changes use the snapshot and do not send HTTP requests. The HTTP counter makes this observable.
3. Use the checkboxes on the left, then **Zatwierdź zaznaczone** to change several decisions locally. MVVM CheckedRecordIds is separate from the current cell / selected rows.
4. Open **Zmiany** from the Grid side tools to compare original and proposed values. **Walidacja** shows validation failures. **Odrzuć bufor** restores pending changes without writing to the server.
5. **Zapisz zmiany przez REST** invokes Grid.SaveAsync from the ViewModel. Only changed rows and fields are sent, with their baseline versions. Both client and server use atomic batches: all submitted edits save, or none save. A successful response advances the local version baseline.
6. Ordinary Grid refresh is local. **Pobierz aktualne dane** downloads a new complete snapshot; pending edits must first be saved or explicitly discarded. Failed or cancelled downloads never replace a usable snapshot.

The displayed margin/change percentages are rounded for presentation. Server validation uses unrounded decimal arithmetic. Approval is the decision saved with a price line; this lesson does not schedule an external ERP price activation job.

## A real conflict with another operator

Open **Drugi operator** before saving. Both windows download the same range from the same server. Change the same product in both windows, then save in the second window first. Saving in the first window now returns a version conflict from SQLite, not a simulated client error.

The first operator's buffer remains intact. The save report displays the server version and values; the table and Zmiany panel retain local proposals. No automatic overwrite or silent merge occurs. To use the latest server values, click **Odrzuć bufor i pobierz aktualne dane…** and explicitly confirm discarding the entire buffer. This simple lesson does not offer a per-field merge editor.

## A real business rejection

Expand **Próby: konflikt i dodatkowa walidacja serwera**. Approve a line locally; the generated proposals normally have about 25% margin. Click **Serwer: marża 35%**, then Save. That control really changes the persisted server policy through HTTP. The server rejects approval under the new policy, even though the client accepted the numeric edit. All other rows in that batch also remain unsaved. Raise the sale price or restore the policy to 15%, then retry. The server also refuses approval of inactive products and rechecks dates, decisions, comments and the editable-field allowlist.

## What protects the write

[Server/PriceCommitService.cs](Server/PriceCommitService.cs) checks expected versions and business rules inside a database transaction. EF Core includes Version in each UPDATE condition. It increments the versions, writes a receipt and commits only after every row passes. An error rolls everything back. A stable request ID and persisted receipt prevent repeating a successful write after a lost response. Reusing an ID with a different payload is rejected.

[Server/PriceQueries.cs](Server/PriceQueries.cs) reads each download window with its count and dataset revision in one transaction. The client rejects the whole download if the revision changes between windows, avoiding a mixed snapshot. It does not silently replace the previous snapshot with a partial result.

This is a **localhost educational server**, without authentication or production administration. The policy endpoint exists solely to demonstrate changing server rules. Before real deployment, provide authentication/authorization, trusted supplier scope, protected administration, HTTPS, operational limits and receipt retention. These application responsibilities are outside the Grid component.

## Verify / modify

```powershell
 dotnet run --project Tests/MoreDemos.WpfChecks -c Release -- 05
```

Checks start their own server with a temporary SQLite working database and close their hidden WPF windows. They verify the full download, local navigation, actual writes, repeated saves, two-operator conflict, atomic rollback, current policy, server validation, idempotency, cancellation, revision checks, network failure and preservation of the original warehouse catalog.

Try adding a new business rule on the server, then observe rejection without throwing away local edits. Or change the selected category and verify that local filters cannot broaden the original downloaded scope.
