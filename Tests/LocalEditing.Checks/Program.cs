using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Commit;
using PhialeGrid.Core.Query;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Composition;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

SQLitePCL.Batteries_V2.Init();
if (args is ["--seed", var path, var sql]) {
    if (File.Exists(path)) throw new IOException("Refusing to replace an existing seed.");
    using var db = new SqliteConnection("Data Source=" + path); db.Open();
    using var command = db.CreateCommand(); command.CommandText = File.ReadAllText(sql); command.ExecuteNonQuery();
    Console.WriteLine("Created demo 04 seed: " + path); return;
}
string FindSeed() {
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
        var file = Path.Combine(dir.FullName, "Data", "local-editing.sqlite");
        if (File.Exists(file)) return file;
    }
    throw new FileNotFoundException("Demo 04 seed missing");
}
var seed = FindSeed(); var originalHash = SHA256.HashData(File.ReadAllBytes(seed));
var directory = Path.Combine(Path.GetTempPath(), "Phiale-LocalEditing-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory); var working = Path.Combine(directory, "assets.sqlite"); File.Copy(seed, working);
int checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
IGridApi Runtime(AssetGridComposition data) {
    var api = GridApiFactory.CreateRuntime(); api.Projection.ConfigureColumns(AssetColumns.All); api.Data.Bind(data.Binding);
    api.Projection.IsPagingEnabled = false; api.Projection.EnableDataVirtualization = false;
    api.Editing.SetCulture(CultureInfo.GetCultureInfo("pl-PL")); return api;
}
Task Query(IGridApi api) => api.Data.ExecuteAsync(api.Projection.CreateDataRequest(GridDataRequestKind.Query, 0, 100), default);
Task<GridCommitResult> Save(IGridApi api) => api.Data.ExecuteAsync(new() { Kind = GridDataRequestKind.Commit }, default);
AssetRow Row(IGridApi api, string id) => api.Editing.Records.Cast<AssetRow>().Single(r => r.Id == id);
try {
    var repository = new AssetRepository(working); var persisted = repository.ReadAll();
    Check(persisted.Count == 24 && persisted.Select(r => r.Id).Distinct().Count() == 24, "small separate seed with 24 unique assets");
    Check(persisted.Select(r => r.Category).Distinct().Count() == 4 && persisted.Any(r => r.NextInspection is null) && persisted.Any(r => !r.InService) && persisted.Any(r => r.UnitPrice == 0), "varied categories, missing dates, booleans and zero prices");
    Check(AssetColumns.All.Where(c => c.IsEditable).Select(c => c.EditorKind).Distinct().Count() == 7, "seven editor kinds in the same example");
    using (var data = await AssetGridComposition.CreateAsync(working))
    using (var api = Runtime(data)) {
        await Query(api); var before = repository.ReadAll().Single(r => r.Id == "EQ-001");
        Check(!ReferenceEquals(Row(api, before.Id), before), "Grid owns working record copies");
        Check(api.Editing.TrySetFieldValue(before.Id, "Name", "Laptop po przeglądzie"), "text edit accepted");
        Check(api.Editing.TrySetFieldValue(before.Id, "Quantity", 7), "integer edit accepted");
        Check(api.Editing.TrySetFieldText(before.Id, "UnitPrice", "1234,56", CultureInfo.GetCultureInfo("pl-PL")), "decimal editor uses Polish decimal separator");
        Check(api.Editing.TrySetFieldValue(before.Id, "Category", "Biuro"), "restricted category edit accepted");
        Check(api.Editing.TrySetFieldValue(before.Id, "InService", true), "checkbox edit accepted");
        Check(api.Editing.TrySetFieldValue(before.Id, "NextInspection", new DateTime(2028, 2, 29)), "nullable date edit accepted");
        Check(api.Editing.TrySetFieldValue(before.Id, "Location", "Mobilny serwis — Kraków"), "autocomplete accepts custom location");
        Check(api.Editing.HasPendingEdits && api.Editing.GetFieldChanges(before.Id).Count >= 6, "pending buffer exposes individual field changes");
        Check(repository.ReadAll().Single(r => r.Id == before.Id) == before, "database unchanged before Save");
        api.Projection.Sorts = [new("Name", GridSortDirection.Descending)]; await Query(api);
        Check(api.Editing.HasPendingEdits && Row(api, before.Id).Quantity == 7, "query and sort preserve pending edits");
        Check(api.Editing.TrySetFieldValue("EQ-002", "Notes", "Drugie urządzenie"), "another record can be edited");
        Check(api.Editing.RestoreRecord(before.Id), "restore one record succeeds");
        Check(Row(api, before.Id).Name == before.Name && api.Editing.HasRecordChanges("EQ-002"), "restore one row preserves edits in another row");
        api.Editing.CancelAllEdits();
        Check(!api.Editing.HasPendingEdits && repository.ReadAll().SequenceEqual(persisted), "Discard clears buffer and leaves SQLite unchanged");

        api.Editing.TrySetFieldValue(before.Id, "Quantity", -1);
        Check(api.Editing.HasValidationIssues, "invalid quantity is exposed through Grid validation");
        api.Editing.CancelAllEdits();
        Check(!api.Editing.HasValidationIssues, "Discard also clears validation state");
        api.Editing.TrySetFieldValue(before.Id, "Category", "Nieistniejąca kategoria");
        Check(api.Editing.HasValidationIssues, "closed combo rejects a value outside its items"); api.Editing.CancelAllEdits();
        api.Editing.TrySetFieldValue(before.Id, "UnitPrice", 1.234m);
        Check(api.Editing.HasValidationIssues, "decimal scale rejects a third fractional digit"); api.Editing.CancelAllEdits();
        api.Editing.TrySetFieldValue(before.Id, "Name", "Laptop po przeglądzie");
        api.Editing.TrySetFieldValue(before.Id, "UnitPrice", 1234.56m);
        api.Editing.TrySetFieldValue(before.Id, "NextInspection", new DateTime(2028, 2, 29));
        api.Editing.TrySetFieldValue(before.Id, "InService", true);
        api.Projection.Filters = new GridFilterGroup([new("Category", GridFilterOperator.Equals, "BHP")], GridLogicalOperator.And);
        await Query(api);
        Check(api.Data.Current.VisibleRecords.Cast<AssetRow>().All(row => row.Id != before.Id) && api.Editing.HasPendingEdits,
            "filter hides the edited row without losing pending changes");
        var result = await Save(api);
        api.Projection.Filters = GridFilterGroup.EmptyAnd(); await Query(api);
        foreach (var outcome in result.Records) Console.WriteLine($"SAVE {outcome.RecordId} {outcome.Status}: {outcome.Message}");
        Check(result.Succeeded && !api.Editing.HasPendingEdits, "successful Save clears pending changes");
        var saved = repository.ReadAll().Single(r => r.Id == before.Id);
        Check(saved.Name == "Laptop po przeglądzie" && saved.UnitPrice == 1234.56m && saved.NextInspection == new DateTime(2028, 2, 29) && saved.InService && saved.Version == 2,
            "SQLite persists text, exact decimal cents, date, bool and incremented version");
        api.Editing.TrySetFieldValue(before.Id, "Notes", "Tylko w pamięci"); api.Editing.CancelAllEdits();
        Check(Row(api, before.Id).Name == saved.Name, "Discard after Save restores the last committed state");
        api.Editing.TrySetFieldValue(before.Id, "NextInspection", null);
        Check((await Save(api)).Succeeded && repository.ReadAll().Single(r => r.Id == before.Id).NextInspection is null, "date can be cleared and persisted as SQL NULL");
        api.Editing.TrySetFieldValue(before.Id, "Notes", "Zapis po usunięciu blokady");
        using (var locked = repository.Open())
        using (var transaction = locked.BeginTransaction()) {
            var failure = await Save(api);
            Check(!failure.Succeeded && api.Editing.HasPendingEdits, "SQLite failure retains the real session's pending buffer");
        }
        Check((await Save(api)).Succeeded && repository.ReadAll()[0].Notes == "Zapis po usunięciu blokady", "retry after unlocking saves retained edits");
    }
    using (var reopened = await AssetGridComposition.CreateAsync(working))
    using (var api = Runtime(reopened)) { await Query(api); Check(Row(api, "EQ-001").Name == "Laptop po przeglądzie", "saved data survives reopening a fresh session"); }

    using (var first = await AssetGridComposition.CreateAsync(working))
    using (var second = await AssetGridComposition.CreateAsync(working))
    using (var one = Runtime(first))
    using (var two = Runtime(second)) {
        await Query(one); await Query(two);
        one.Editing.TrySetFieldValue("EQ-002", "Quantity", 11); two.Editing.TrySetFieldValue("EQ-002", "Quantity", 12);
        Check((await Save(one)).Succeeded, "first concurrent session saves");
        var conflict = await Save(two);
        Check(!conflict.Succeeded && conflict.Records.All(r => r.Status == GridRecordCommitStatus.VersionConflict), "stale version is rejected instead of overwriting SQLite");
        Check(two.Editing.HasPendingEdits && repository.ReadAll().Single(r => r.Id == "EQ-002").Quantity == 11, "failed Save retains pending buffer and committed data");
        two.Editing.CancelAllEdits();
    }

    using (var data = await AssetGridComposition.CreateAsync(working))
    using (var api = Runtime(data))
    using (var context = GridApiFactory.CreateContext<AssetRow>(api,data.Binding)) {
        context.SetValidators(new AssetBusinessRules(),new AssetBudgetValidator(repository));
        await Query(api);
        Check(context.SetValue("EQ-001","Quantity",500),"MVVM proposes a legal quantity");
        Check(!context.SetValue("EQ-001","UnitPrice",100000m),"record rule validates the proposed combination of fields");
        context.DiscardChanges();
        var ids=repository.ReadAll().Take(3).Select(r=>r.Id).ToArray();
        foreach(var id in ids){context.SetValue(id,"Quantity",500);Check(context.SetValue(id,"UnitPrice",4000m),"individual position passes business limit");}
        var beforeBudget=repository.ReadAll();
        var budgetResult=await context.SaveAsync();
        Check(!budgetResult.Succeeded&&context.HasValidationErrors&&context.HasPendingChanges,"asynchronous budget rule rejects the whole pending batch");
        Check(repository.ReadAll().SequenceEqual(beforeBudget),"failed application validation leaves SQLite unchanged");
        context.DiscardChanges();
        Check(context.SetValue("EQ-001","Notes","MVVM saved"),"programmatic edit follows the same buffer");
        await context.SaveCommand.ExecuteAsync();
        Check(repository.ReadAll().Single(r=>r.Id=="EQ-001").Notes=="MVVM saved"&&!context.HasPendingChanges,"ViewModel command persists and clears the shared buffer");
    }
    var port = new SqliteCommitPort(repository, repository.ReadAll());
    var a = repository.ReadAll()[2]; var b = repository.ReadAll()[3];
    ChangeSetChange Change(AssetRow row, string field, object value) => new(ChangeTargetKind.Row, row.Id, "", ChangeOperation.Update,
        [new(field, null, value)], row.Version.ToString(CultureInfo.InvariantCulture));
    var rejected = await port.CommitAsync(new("atomic-check", "test", [Change(a, "Name", "Must roll back"), Change(b, "Quantity", -1)]), default);
    Check(!rejected.Succeeded && repository.ReadAll()[2] == a, "invalid second record rolls back the first update in the transaction");
    using (var locked = repository.Open())
    using (var transaction = locked.BeginTransaction()) {
        var failed = await port.CommitAsync(new("locked-check", "test", [Change(a, "Quantity", 2)]), default);
        Check(failed.Records.All(r => r.Status == GridRecordCommitStatus.TechnicalFailed), "locked SQLite returns a failed outcome without successful records");
    }
    using var canceled = new CancellationTokenSource(); canceled.Cancel();
    try { await port.CommitAsync(new("cancel", "test", [Change(a, "Quantity", 2)]), canceled.Token); throw new Exception("Ignored cancellation"); }
    catch (OperationCanceledException) { Check(repository.ReadAll()[2] == a, "cancellation does not write data"); }
    Check(SHA256.HashData(File.ReadAllBytes(seed)).SequenceEqual(originalHash), "seed database is never changed");
    Console.WriteLine($"PASS {checks} local editing checks");
} finally {
    if (Path.GetDirectoryName(Path.GetFullPath(directory)) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) ||
        !Path.GetFileName(directory).StartsWith("Phiale-LocalEditing-", StringComparison.Ordinal)) throw new IOException("Unexpected fixture path");
    Directory.Delete(directory, recursive: true);
}
