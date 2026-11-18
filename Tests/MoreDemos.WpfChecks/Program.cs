// Independent sample assemblies link model sources; aliases identify their distinct runtime types.
extern alias snapshot;
extern alias priceserver;
extern alias card;
extern alias nested;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using PhialeGrid.Contracts;
using PhialeGrid.Core;
using PhialeGrid.Core.Query;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using PriceServer = priceserver::PhialeTech.Grid.Samples.PriceReview.Server.PriceServer;
using PriceRow = snapshot::PhialeTech.Grid.Samples.PriceReview.Contracts.PriceRow;
using PricePage = snapshot::PhialeTech.Grid.Samples.PriceReview.Contracts.PricePage;
using PriceCommit = snapshot::PhialeTech.Grid.Samples.PriceReview.Contracts.PriceCommit;
using PriceChange = snapshot::PhialeTech.Grid.Samples.PriceReview.Contracts.PriceChange;
using PriceCommitResponse = snapshot::PhialeTech.Grid.Samples.PriceReview.Contracts.PriceCommitResponse;
using ReviewScope = snapshot::PhialeTech.Grid.Samples.PriceReview.Contracts.ReviewScope;
using PriceSearch = snapshot::PhialeTech.Grid.Samples.PriceReview.Contracts.PriceSearch;
using SnapshotComposition = snapshot::PhialeTech.Grid.Samples.Wpf.RestSnapshot.Composition.SnapshotComposition;
using PriceReviewViewModel = snapshot::PhialeTech.Grid.Samples.Wpf.RestSnapshot.ViewModels.PriceReviewViewModel;
using PriceWindow = snapshot::PhialeTech.Grid.Samples.Wpf.RestSnapshot.Views.MainWindow;
using AssetRow = card::PhialeTech.Grid.Samples.Wpf.LocalEditing.Models.AssetRow;
using NestedAssetRow = nested::PhialeTech.Grid.Samples.Wpf.LocalEditing.Models.AssetRow;
using AssetCatalog = card::PhialeTech.Grid.Samples.Wpf.RowDetails.Infrastructure.AssetCatalog;
using AssetDetailsView = card::PhialeTech.Grid.Samples.Wpf.RowDetails.Views.AssetDetailsView;
using AssetsViewModel = card::PhialeTech.Grid.Samples.Wpf.RowDetails.ViewModels.AssetsViewModel;
using AssetDetailsViewModel = card::PhialeTech.Grid.Samples.Wpf.RowDetails.ViewModels.AssetDetailsViewModel;
using LocationRow = nested::PhialeTech.Grid.Samples.Wpf.NestedGrid.Models.LocationRow;
using GridControl = PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;

internal static class Program
{
    private static int checks;
    [STAThread] private static int Main(string[] args)
    {
        if(args.Length==0){
            foreach(var lesson in new[]{"06","07","08"}){
                var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add(lesson);
                using var child=Process.Start(start)!;var output=child.StandardOutput.ReadToEndAsync();var errors=child.StandardError.ReadToEndAsync();
                if(!child.WaitForExit(60000)){child.Kill(entireProcessTree:true);throw new TimeoutException("Lesson check timed out: "+lesson);}
                Console.Write(output.GetAwaiter().GetResult());Console.Error.Write(errors.GetAwaiter().GetResult());
                if(child.ExitCode!=0)return child.ExitCode;
            }
            return 0;
        }
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        foreach (var uri in new[]{"Phiale.Design.Wpf;component/Themes/Palette.xaml", "Phiale.Design.Wpf;component/Themes/Controls.xaml", "PhialeTech.Grid.Styles.Wpf;component/Themes/Generic.xaml"})
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + uri) });
        var result = 0;
        app.Dispatcher.BeginInvoke(async () => {
            try { if(args[0]=="06")await Snapshot();else if(args[0]=="07")await Card();else if(args[0]=="08")await Nested();else throw new ArgumentException("Use 06, 07 or 08."); Console.WriteLine($"PASS {checks} native/REST checks for lesson {args[0]}"); }
            catch (Exception error) { Console.Error.WriteLine(error); result = 1; }
            finally { foreach(Window window in app.Windows.Cast<Window>().ToArray()) window.Close(); app.Shutdown(); }
        });
        app.Run(); return result;
    }
    private static void Check(bool condition, string text) { if(!condition)throw new Exception(text);checks++;Console.WriteLine("PASS " + text); }
    private static void Show(Window window) { window.Opacity=0;window.ShowActivated=false;window.ShowInTaskbar=false;window.Show(); }
    private static async Task Wait(Func<bool> ready)
    {
        var end=DateTime.UtcNow.AddSeconds(30);
        while(!ready()){if(DateTime.UtcNow>end)throw new TimeoutException("Native view did not reach the expected state.");await Task.Delay(20);await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);}
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }
    private static IEnumerable<T> Children<T>(DependencyObject root) where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){
            var child=VisualTreeHelper.GetChild(root,i);if(child is T match)yield return match;
            foreach(var nested in Children<T>(child))yield return nested;
        }
    }
    private static void Capture(Window window,string name)
    {
        var directory=Environment.GetEnvironmentVariable("PHIALE_DEMO_SCREENSHOTS");if(string.IsNullOrEmpty(directory))return;
        Directory.CreateDirectory(directory);var content=(FrameworkElement)window.Content;content.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,PixelFormats.Pbgra32);
        var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen()){var rect=new Rect(0,0,content.ActualWidth,content.ActualHeight);dc.DrawRectangle(window.Background,null,rect);dc.DrawRectangle(new VisualBrush(content) {ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,0,content.ActualWidth,content.ActualHeight)},null,rect);}
        bitmap.Render(drawing);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));
        using var output=File.Create(Path.Combine(directory,name+".png"));png.Save(output);
    }
    private static Task Query(IGridApi api,int offset=0,int size=25) => api.Data.ExecuteAsync(api.Projection.CreateDataRequest(GridDataRequestKind.ChangeQuery,offset,size),default);


    private static async Task Snapshot()
    {
        var folder=Path.Combine(Path.GetTempPath(),"PhialePriceChecks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var database=Path.Combine(folder,"prices.sqlite");
        var catalog=priceserver::PhialeTech.Grid.Samples.PriceReview.Server.PriceSeed.FindCatalog();
        var before=SHA256.HashData(File.ReadAllBytes(catalog));
        await using var server=PriceServer.Create("http://127.0.0.1:0",database);
        await PriceServer.InitializeAsync(server,catalog);await server.StartAsync();
        var address=server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http=new HttpClient {BaseAddress=new Uri(address)};
        using var source=new SnapshotComposition();var api=GridApiFactory.CreateRuntime();
        var model=new PriceReviewViewModel(api,source) {Address=address};
        var window=new PriceWindow(api,model);Show(window);
        using var source2=new SnapshotComposition();var api2=GridApiFactory.CreateRuntime();
        var model2=new PriceReviewViewModel(api2,source2) {Address=address};
        var window2=new PriceWindow(api2,model2);Show(window2);
        try {
            Check(model.Grid==null,"05 starts with a scope form before creating the snapshot");Capture(window,"05-price-selection");
            await model.LoadAsync();await Wait(()=>api.Data.Current?.HasConfirmedQuery==true);
            Check(model.Grid?.TotalCount==10000,"05 downloads all 10000 selected supplier lines");
            Check(source.RequestCount==10,"05 uses ten HTTP windows to build one complete snapshot");
            Check(api.Editing.CanEdit,"05 snapshot has a remote commit endpoint");
            var grid=model.Grid!;var requests=source.RequestCount;
            var first=(PriceRow)api.Data.Current!.VisibleRecords[0];var second=(PriceRow)api.Data.Current.VisibleRecords[1];
            var firstId=first.Id;var secondId=second.Id;
            api.Projection.Filters=new GridFilterGroup([new("Category",GridFilterOperator.Equals,first.Category)],GridLogicalOperator.And);
            api.Projection.Sorts=[new("ProductCode",GridSortDirection.Descending)];await Query(api,0,100);
            Check(api.Data.Current.TotalCount>1000 && api.Data.Current.TotalCount<10000,"05 filters categories locally across the snapshot");
            await api.Data.ExecuteAsync(api.Projection.CreateDataRequest(GridDataRequestKind.Page,100,100,parameter:1),default);
            Check(api.Data.Current.Paging.Offset==100 && source.RequestCount==requests,"05 local filtering, sorting and paging send no HTTP");
            api.Projection.Filters=new GridFilterGroup([],GridLogicalOperator.And);api.Projection.Sorts=[];await Query(api,0,100);
            async Task<PriceRow> ReadFirst() {
                using var response=await http.PostAsJsonAsync("api/price-reviews/search",new PriceSearch(model.Scope,0,1));response.EnsureSuccessStatusCode();
                return (await response.Content.ReadFromJsonAsync<PricePage>())!.Items[0];
            }
            var original=await ReadFirst();
            Check(grid.SetValue(firstId,"ProposedSalePrice",original.ProposedSalePrice+10) && grid.SetValue(firstId,"Decision","Zatwierdź"),"05 edits a price and decision through the ViewModel context");
            Check(source.RequestCount==requests && (await ReadFirst()).Version==original.Version,"05 edits stay in the local buffer before Save");
            await model.SaveAsync();
            Check(grid.LastSaveResult.Succeeded && !grid.HasPendingChanges,"05 REST save acknowledges and clears the buffer");
            var saved=await ReadFirst();
            Check(saved.ProposedSalePrice==original.ProposedSalePrice+10 && saved.Version==original.Version+1,"05 persists exact cents and increments the server version");
            Check(source.RequestCount==requests+1,"05 sends only one commit for the changed row");
            grid.SetValue(firstId,"Comment","Drugi zapis tego samego snapshotu");await model.SaveAsync();
            Check(grid.LastSaveResult.Succeeded && (await ReadFirst()).Version==saved.Version+1,"05 updates the local baseline version after Save");
            await model2.LoadAsync();await Wait(()=>api2.Data.Current?.HasConfirmedQuery==true);
            grid.SetValue(firstId,"Comment","Lokalna propozycja operatora 1");
            grid.SetValue(secondId,"Comment","Ta pozycja musi być wycofana razem z konfliktem");
            model2.Grid!.SetValue(firstId,"Comment","Zapis operatora 2");await model2.SaveAsync();
            Check(model2.Grid.LastSaveResult.Succeeded,"05 second operator writes to the same SQLite database");
            await model.SaveAsync();
            Check(!grid.LastSaveResult.Succeeded && grid.PendingRecordCount==2,"05 a real version conflict keeps both pending rows");
            Check(grid.GetChanges(firstId).Any() && model.SaveReport.Contains("SERWER v"),"05 shows server values and preserves the operator's conflicting proposal");
            using(var response=await http.PostAsJsonAsync("api/price-reviews/search",new PriceSearch(model.Scope,0,2))) {
                var rows=(await response.Content.ReadFromJsonAsync<PricePage>())!.Items;
                Check(rows[0].Comment=="Zapis operatora 2" && rows[1].Comment=="","05 conflict rolls back the other valid row too");
            }
            grid.DiscardChanges();await model.LoadAsync();grid=model.Grid!;await Wait(()=>api.Data.Current?.HasConfirmedQuery==true);
            grid.SetValue(secondId,"Decision","Zatwierdź");grid.SetValue(firstId,"Comment","Pakiet odrzucony przez politykę");
            await model.SetMarginPolicyAsync(35);await model.SaveAsync();
            Check(!grid.LastSaveResult.Succeeded && grid.PendingRecordCount==2 && model.SaveReport.Contains("35%"),"05 current server policy rejects a locally valid approval atomically");
            await model.SetMarginPolicyAsync(15);await model.SaveAsync();
            Check(grid.LastSaveResult.Succeeded && !grid.HasPendingChanges,"05 unchanged proposals save after restoring the real policy");

            // An invalid row blocks the complete local buffer, including other valid edits.
            var beforeInvalid=source.RequestCount;
            grid.SetValue(firstId,"Comment","");grid.SetValue(firstId,"Decision","Odrzuć");
            grid.SetValue(secondId,"Comment","Nie wysyłaj części pakietu");
            await model.SaveAsync();
            Check(!grid.LastSaveResult.Succeeded && grid.PendingRecordCount==2 && source.RequestCount==beforeInvalid,"05 atomic client validation blocks the whole buffer before HTTP");
            grid.DiscardChanges();
            await Wait(()=>!grid.IsBusy);
            var unapprovedId=((PriceRow)api.Data.Current!.VisibleRecords[2]).Id;
            grid.CheckRecords([firstId,unapprovedId]);await Wait(()=>grid.CheckedRecordIds.Count==2);
            model.ApproveSelected();
            Check(grid.PendingRecordCount>=1,"05 bulk approval uses MVVM checked-record IDs");
            grid.DiscardChanges();
            // A client cannot bypass server validation or write read-only columns.
            async Task<PriceCommitResponse> Raw(PriceCommit body) {
                using var response=await http.PostAsJsonAsync("api/price-reviews/commit",body);response.EnsureSuccessStatusCode();
                return (await response.Content.ReadFromJsonAsync<PriceCommitResponse>())!;
            }
            PriceCommit Change(PriceRow row,string field,object value,string? id=null)=>new(id??Guid.NewGuid().ToString("N"),model.Scope,[new(row.Id,row.Version,new() {[field]=JsonSerializer.SerializeToElement(value)})]);
            var latest=await ReadFirst();
            Check((await Raw(Change(latest,"ProductCode","FORGED"))).Rows[0].Status=="Rejected","05 server refuses edits to read-only product identity");
            Check((await Raw(Change(latest,"ValidFrom",DateTime.Today.AddDays(-1)))).Rows[0].Status=="Rejected","05 server rejects expired dates even if the caller bypasses Grid");
            var request=Change(latest,"Comment","Bezpieczna ponowna wysyłka");
            var receipt=await Raw(request);var retry=await Raw(request);
            Check(receipt.Rows[0].Status=="Saved" && retry.Rows[0].Current!.Version==receipt.Rows[0].Current!.Version && (await ReadFirst()).Version==latest.Version+1,"05 retrying an acknowledged commit does not write twice");
            using(var changedBody=await http.PostAsJsonAsync("api/price-reviews/commit",Change(latest,"Comment","Inna treść",request.RequestId)))
                Check((int)changedBody.StatusCode==400,"05 idempotency key cannot be reused for a different payload");
            Check((await Raw(Change(latest,"Comment","Stara wersja"))).Rows[0].Status=="Conflict","05 server rejects a stale expected version");

            using(var inactivePage=await http.PostAsJsonAsync("api/price-reviews/search",new PriceSearch(model.Scope,996,1))) {
                var inactive=(await inactivePage.Content.ReadFromJsonAsync<PricePage>())!.Items[0];
                Check((await Raw(Change(inactive,"Decision","Zatwierdź"))).Rows[0].Message.Contains("wycofany"),"05 current product availability is enforced by the server");
            }
            var wrongScope=request with {RequestId=Guid.NewGuid().ToString("N"),Scope=model.Scope with {Supplier="Baltic"}};
            Check((await Raw(wrongScope)).Rows[0].Status=="Rejected","05 server refuses a row outside the selected supplier scope");
            // A change between download windows must reject the incomplete snapshot.
            var endpoint=new snapshot::PhialeTech.Grid.Samples.Wpf.RestSnapshot.Infrastructure.PriceEndpoint(model.Scope);
            var options=endpoint.Options(address);var jsonOptions=new JsonSerializerOptions(JsonSerializerDefaults.Web);
            using(var page=await http.PostAsJsonAsync("api/price-reviews/search",new PriceSearch(model.Scope,0,1000)))
                options.ReadQueryResponse(await page.Content.ReadAsStringAsync(),jsonOptions);
            await model.SetMarginPolicyAsync(16);
            var inconsistent=false;
            using(var page=await http.PostAsJsonAsync("api/price-reviews/search",new PriceSearch(model.Scope,1000,1000))) {
                try {options.ReadQueryResponse(await page.Content.ReadAsStringAsync(),jsonOptions);}catch(InvalidOperationException){inconsistent=true;}
            }
            Check(inconsistent,"05 detects server changes between snapshot download windows");
            await model.SetMarginPolicyAsync(15);
            using(var cancellation=new CancellationTokenSource()) {
                cancellation.Cancel();var cancelled=false;
                try {using var unused=await source.DownloadAsync(model.Columns,model.Scope,cancellation.Token,address);}catch(OperationCanceledException){cancelled=true;}
                Check(cancelled,"05 snapshot download observes cancellation");
            }
            // Local validation must stop the whole batch before HTTP, including a valid second row.
            await model.LoadAsync();grid=model.Grid!;await Wait(()=>api.Data.Current?.HasConfirmedQuery==true);
            Check(!grid.SetValue(firstId,"ProposedSalePrice",-1m),"05 numeric constraints reject an invalid proposal");
            grid.SetValue(firstId,"Comment","W buforze po awarii");await Wait(()=>grid.PendingRecordCount==1);Capture(window,"05-price-review");
            await server.StopAsync();await model.SaveAsync();
            Check(grid.HasPendingChanges && !grid.LastSaveResult.Succeeded,"05 connection failure preserves the local buffer");
            grid.DiscardChanges();var old=model.Grid;await model.LoadAsync();
            Check(ReferenceEquals(old,model.Grid) && model.Status.Contains("Nie pobrano"),"05 failed download retains the previously usable snapshot");
            Check(SHA256.HashData(File.ReadAllBytes(catalog)).SequenceEqual(before),"05 warehouse source database remains unchanged");
        } finally {
            model.Grid?.DiscardChanges();model2.Grid?.DiscardChanges();window2.Close();window.Close();await server.StopAsync();
            Console.WriteLine("Price-check database: "+database);
        }
    }
    private static async Task Card()
    {
        var assets=AssetCatalog.Read();
        Check(assets.Count==24,"06 reuses the small existing asset database");
        using var source=new card::PhialeTech.Grid.Samples.Wpf.RowDetails.Composition.AssetGridComposition(assets);
        var api=GridApiFactory.CreateRuntime();
        var model=new AssetsViewModel(GridApiFactory.CreateContext<AssetRow>(api,source.Binding));
        var window=new card::PhialeTech.Grid.Samples.Wpf.RowDetails.Views.MainWindow(model);Show(window);
        try {
            await Wait(()=>api.Data.Current?.HasConfirmedQuery==true);
            var grid=(GridControl)window.FindName("AssetsGrid");
            var one=api.Projection.ResolveSurfaceRowKeys(["EQ-001"]).Single();
            var two=api.Projection.ResolveSurfaceRowKeys(["EQ-002"]).Single();
            Check(grid.ExpandRowDetail(one),"06 opens an inline detail");
            await Wait(()=>Children<AssetDetailsView>(grid).Any());
            Check(Children<AssetDetailsView>(grid).Single().DataContext is AssetDetailsViewModel vm && vm.Id=="EQ-001","06 creates the application's UserControl with the owning record");
            Check(Children<System.Windows.Controls.TextBlock>(Children<AssetDetailsView>(grid).Single()).Any(t=>t.Text.Contains("Laptop")),"06 renders the card's XAML bindings");
            Check(grid.ExpandRowDetail(two),"06 opens a second inline card");
            await Wait(()=>Children<AssetDetailsView>(grid).Count()==2);
            Capture(window,"06-row-details");
            api.Selection.SetCurrentCell(two,"Name");
            Check(Children<AssetDetailsView>(grid).Select(v=>((AssetDetailsViewModel)v.DataContext).Id).Order().SequenceEqual(new[]{"EQ-001","EQ-002"}),"06 changing the current record does not replace the other card's data");
            api.Details.Collapse(one);await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(!api.Details.IsExpanded(one),"06 collapses the correct card");
            Check(!model.Grid.HasPendingChanges,"06 remains read-only");
        } finally {window.Close();}
    }
    private static async Task Nested()
    {
        // Tests use a private database, never the user's working copy.
        var database=Path.Combine(Path.GetTempPath(),"Phiale-nested-"+Guid.NewGuid().ToString("N")+".sqlite");
        var seed=Path.Combine(AppContext.BaseDirectory,"Data","local-editing.sqlite");
        var original=SHA256.HashData(File.ReadAllBytes(seed)); File.Copy(seed,database);
        var repository=new nested::PhialeTech.Grid.Samples.Wpf.NestedGrid.Infrastructure.LocationRepository(database);
        using var source=new nested::PhialeTech.Grid.Samples.Wpf.NestedGrid.Composition.LocationGridComposition(repository);
        var api=GridApiFactory.CreateRuntime();
        var model=new nested::PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels.LocationsViewModel(
            GridApiFactory.CreateContext<LocationRow>(api,source.Binding),database);
        var window=new nested::PhialeTech.Grid.Samples.Wpf.NestedGrid.Views.MainWindow(model,source) { Height=1100 };Show(window);
        try {
            await Wait(()=>api.Data.Current?.HasConfirmedQuery==true);
            var grid=(GridControl)window.FindName("LocationsGrid");
            var parents=api.Data.Current.VisibleRecords.Cast<LocationRow>().ToArray();
            Check(grid.UseSharedDetailCommandBar,"08 starts with one shared command bar");
            // First exercise the original separate-bar variant without changing the data tests.
            grid.UseSharedDetailCommandBar=false;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(parents.Length==6,"08 shows six parent locations");
            Check(model.Columns.Select(column=>column.Id).SequenceEqual(new[]{"Id","Contact","Phone","Notes"}),
                "08 master has a stable key and three editable fields");
            Check(!grid.IsGridReadOnly && model.Columns.Count(column=>column.IsEditable)==3,"08 master is genuinely editable");

            var parent=parents[0]; var parentKey=api.Projection.ResolveSurfaceRowKeys([parent.Id]).Single();
            api.Selection.SetCurrentCell(parentKey,"Contact");
            Check(grid.BeginEditCurrentCell(),"08 native master editor opens");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var masterEditor=Children<PhialeTech.PhialeGrid.Wpf.Surface.Presenters.GridCellPresenter>(grid).Single(p=>p.CellData?.IsEditing==true);
            Children<System.Windows.Controls.TextBox>(masterEditor).Single().Text="Anna — opiekun";
            Check(grid.PostCurrentEdit(),"08 native master editor accepts text");
            Check(model.Grid.HasPendingChanges && repository.ReadLocations().First().Contact!="Anna — opiekun",
                "08 master edit stays buffered before Save");
            await model.Grid.SaveAsync();
            Check(repository.ReadLocations().First().Contact=="Anna — opiekun" && !model.Grid.HasPendingChanges,
                "08 master Save persists the location");

            foreach(var row in parents.Take(2))Check(grid.ExpandRowDetail(api.Projection.ResolveSurfaceRowKeys([row.Id]).Single()),"08 opens a location's table");
            await Wait(()=>Children<GridControl>(grid).Count()==2);
            var children=Children<GridControl>(grid).ToArray();
            await Wait(()=>children.All(child=>child.Api.Data.Current?.HasConfirmedQuery==true));
            foreach(var child in children) {
                var records=child.Api.Projection.ProjectCurrent().Records.Cast<NestedAssetRow>().ToArray();
                Check(records.Length==4 && records.Select(row=>row.Location).Distinct().Count()==1,"08 child contains only its parent's four assets");
                Check(!child.IsGridReadOnly && child.Columns.Count(column=>column.IsEditable)==3,"08 child has three editable fields");
                Check(Children<System.Windows.Controls.Button>(child).Any(button=>System.Windows.Automation.AutomationProperties.GetAutomationId(button)=="grid.commit" && button.IsVisible),
                    "08 child displays its own standard Save action");
            }
            var first=children.Single(child=>child.Api.Projection.ProjectCurrent().Records.Cast<NestedAssetRow>().First().Location==parent.Id);
            var other=children.Single(child=>child!=first); var asset=first.Api.Projection.ProjectCurrent().Records.Cast<NestedAssetRow>().First();
            var childKey=first.Api.Projection.ResolveSurfaceRowKeys([asset.Id]).Single();
            var oldName=repository.Assets.ReadAll().Single(row=>row.Id==asset.Id).Name;
            first.Api.Selection.SetCurrentCell(childKey,"Name");
            Check(first.BeginEditCurrentCell(),"08 native detail editor opens");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var editor=Children<PhialeTech.PhialeGrid.Wpf.Surface.Presenters.GridCellPresenter>(first).Single(p=>p.CellData?.IsEditing==true);
            Children<System.Windows.Controls.TextBox>(editor).Single().Text="Wyposażenie po przeglądzie";
            Check(first.PostCurrentEdit(),"08 native detail editor accepts text");
            Check(first.Api.Editing.HasPendingEdits && repository.Assets.ReadAll().Single(row=>row.Id==asset.Id).Name==oldName,
                "08 detail edit stays buffered before Save");
            Check(!model.Grid.HasPendingChanges && !other.Api.Editing.HasPendingEdits,"08 buffers are isolated from master and sibling");
            window.Close();
            Check(window.IsVisible,"08 closing with an unsaved child is blocked");
            await model.Grid.SaveAsync();
            Check(repository.Assets.ReadAll().Single(row=>row.Id==asset.Id).Name==oldName,"08 master Save does not commit a child buffer");
            grid.Api.Details.Collapse(parentKey);await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            grid.ExpandRowDetail(parentKey);await Wait(()=>Children<GridControl>(grid).Count()==2);
            first=Children<GridControl>(grid).Single(child=>child.Api.Projection.ProjectCurrent().Records.Cast<NestedAssetRow>().First().Location==parent.Id);
            Check(first.Api.Editing.HasRecordChanges(asset.Id),"08 collapse/reopen retains the child buffer");
            await first.CommitEditsAsync();
            Check(repository.Assets.ReadAll().Single(row=>row.Id==asset.Id).Name=="Wyposażenie po przeglądzie" && !first.Api.Editing.HasPendingEdits,
                "08 detail Save persists its own records");
            Check(first.Api.Editing.TrySetFieldValue(asset.Id,"Quantity",17),"08 quantity is editable");
            first.Api.Editing.CancelPendingChanges();
            Check(repository.Assets.ReadAll().Single(row=>row.Id==asset.Id).Quantity==asset.Quantity,"08 detail Discard leaves the database unchanged");
            first.Api.Editing.TrySetFieldValue(asset.Id,"Quantity",-1); await first.CommitEditsAsync();
            Check(first.Api.Editing.HasPendingEdits && repository.Assets.ReadAll().Single(row=>row.Id==asset.Id).Quantity>=0,"08 persistence rejects invalid child quantity and retains the buffer");
            first.Api.Editing.CancelPendingChanges();
            Check(model.Grid.SetValue(parent.Id,"Phone","discard me"),"08 parent phone is editable");
            model.Grid.DiscardChanges();
            Check(repository.ReadLocations().First().Phone=="","08 master Discard leaves the database unchanged");
            var otherCount=other.Api.Projection.ProjectCurrent().TotalCount;
            first.Api.Projection.Filters=new GridFilterGroup([new("Category",GridFilterOperator.Equals,"Elektronika")],GridLogicalOperator.And);
            first.Api.Projection.RefreshLocalSurface();await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(other.Api.Projection.Filters.Filters.Count==0 && other.Api.Projection.ProjectCurrent().TotalCount==otherCount,
                "08 child filters do not affect siblings");
            Capture(window,"08-separate-master-detail");
            var reopened=new nested::PhialeTech.Grid.Samples.Wpf.NestedGrid.Infrastructure.LocationRepository(database);
            Check(reopened.ReadLocations().First().Contact=="Anna — opiekun" && reopened.Assets.ReadAll().Single(row=>row.Id==asset.Id).Name=="Wyposażenie po przeglądzie",
                "08 saved master/detail values survive reopening the database");
            Check(SHA256.HashData(File.ReadAllBytes(seed)).SequenceEqual(original),"08 shipped seed remains unchanged");
            grid.UseSharedDetailCommandBar=true;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            first=Children<GridControl>(grid).Single(child=>child.Api.Projection.ProjectCurrent().Records.Cast<NestedAssetRow>().Any(row=>row.Id==asset.Id));
            Check(Children<GridControl>(grid).All(child=>!child.Api.Workspace.ResolveCurrent().Single(region=>region.RegionKind==PhialeGrid.Core.Regions.GridRegionKind.TopCommandRegion).IsAvailable),
                "08 shared mode makes every child toolbar unavailable");
            Check(Children<GridControl>(grid).All(child=>!((FrameworkElement)child.TopCommandContent).IsVisible),"08 shared mode really removes child bars from the native layout");
            first.RaiseEvent(new System.Windows.Input.KeyboardFocusChangedEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,0,grid,first)
                {RoutedEvent=System.Windows.Input.Keyboard.GotKeyboardFocusEvent});
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var bar=(PhialeTech.PhialeGrid.Wpf.Controls.PhialeGridCommandBar)grid.TopCommandContent;
            Check(ReferenceEquals(bar.DataContext,first),"08 shared bar follows keyboard activation of a detail");
            model.Grid.SetValue(parent.Id,"Phone","Pending master phone");
            first.Api.Editing.TrySetFieldValue(asset.Id,"Name","Zapis przez wspólny pasek");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var sharedButton=Children<System.Windows.Controls.Button>(bar).First();
            sharedButton.RaiseEvent(new System.Windows.Input.KeyboardFocusChangedEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,0,first,sharedButton)
                {RoutedEvent=System.Windows.Input.Keyboard.GotKeyboardFocusEvent});
            Check(ReferenceEquals(bar.DataContext,first),"08 clicking/focusing shared bar preserves the detail target");
            bar.CommitCommand.Execute(null);
            await Wait(()=>!first.Api.Editing.IsCommitting && !first.Api.Editing.HasPendingEdits);
            Check(repository.Assets.ReadAll().Single(row=>row.Id==asset.Id).Name=="Zapis przez wspólny pasek",
                "08 shared Save persists the active detail to SQLite");
            Check(model.Grid.HasPendingChanges && repository.ReadLocations().First().Phone=="",
                "08 shared detail Save does not persist the master buffer");
            grid.Api.Details.ActivateCommandBarTarget();await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(ReferenceEquals(bar.DataContext,grid),"08 returning to master changes the shared target");
            bar.CancelAllCommand.Execute(null);await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(!model.Grid.HasPendingChanges,"08 shared Discard clears only the active master's buffer");
            grid.UseSharedDetailCommandBar=false;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(first.Api.Workspace.ResolveCurrent().Single(region=>region.RegionKind==PhialeGrid.Core.Regions.GridRegionKind.TopCommandRegion).IsAvailable,
                "08 turning off sharing restores the separate toolbar option");
            grid.UseSharedDetailCommandBar=true;first.Api.Details.ActivateCommandBarTarget();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Check(Children<GridControl>(grid).All(child=>!((FrameworkElement)child.TopCommandContent).IsVisible),"08 repeated mode switch does not leave duplicate bars");
            Capture(window,"08-shared-master-detail");
        } finally {
            foreach(var child in Children<GridControl>((GridControl)window.FindName("LocationsGrid")).ToArray()) {
                child.CancelCurrentEdit(); child.Api.Editing.CancelPendingChanges();
            }
            model.Grid.DiscardChanges();window.Close();
            Console.WriteLine("Nested check database: "+database);
        }
    }
}




