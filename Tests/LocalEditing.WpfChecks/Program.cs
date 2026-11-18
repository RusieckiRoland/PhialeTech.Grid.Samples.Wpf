using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PhialeGrid.Core;
using PhialeGrid.Core.Regions;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Composition;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.ViewModels;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Views;
using PhialeTech.PhialeGrid.Wpf.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

internal static class Program
{
 [STAThread]
 static int Main(string[] args)
 {
    var file = Path.Combine(Path.GetTempPath(), "Phiale-local-wpf-" + Guid.NewGuid().ToString("N") + ".sqlite");
    File.Copy(Path.Combine(AppContext.BaseDirectory, "Data", "local-editing.sqlite"), file);
    using var data = AssetGridComposition.CreateAsync(file).GetAwaiter().GetResult();
    var app = new PhialeTech.Grid.Samples.Wpf.LocalEditing.App(); app.InitializeComponent(); app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    using var api = GridApiFactory.CreateRuntime();
    var model = new AssetsViewModel(PhialeGrid.Core.GridApiFactory.CreateContext<PhialeTech.Grid.Samples.Wpf.LocalEditing.Models.AssetRow>(api, data.Binding), file);
    var window = new MainWindow(api, model) { Opacity = 0, ShowActivated = false, ShowInTaskbar = false };
    var requestedChoice=args.Contains("--close-save")?MessageBoxResult.Yes:args.Contains("--close-cancel")?MessageBoxResult.Cancel:MessageBoxResult.No;
    int choices=0;window.ConfirmCloseChoice=()=>{choices++;return requestedChoice;};
    bool wasClosed=false;window.Closed+=(_,_)=>wasClosed=true;
    var frame = new DispatcherFrame(); var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    Exception? failure = null; var start = DateTime.UtcNow;
    void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
    async Task Flush() { window.UpdateLayout(); await Dispatcher.Yield(DispatcherPriority.ApplicationIdle); window.UpdateLayout(); }
    IEnumerable<T> Children<T>(DependencyObject parent) where T : DependencyObject {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
            var child = VisualTreeHelper.GetChild(parent, i); if (child is T item) yield return item;
            foreach (var nested in Children<T>(child)) yield return nested;
        }
    }
    timer.Tick += async (_, _) => {
        try {
            if (DateTime.UtcNow - start > TimeSpan.FromSeconds(30)) throw new TimeoutException("WPF snapshot load");
            if (api.Data.Current.IsBusy || !api.Data.Current.HasConfirmedQuery) return;
            timer.Stop(); await Flush();
            var grid = (PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid)window.FindName("AssetsGrid");

            Check(api.Editing.Records.Count == 24 && api.Workspace.IsRegionOpenAndActive(GridRegionKind.ChangePanelRegion), "actual WPF loads 24 assets and opens Changes panel");
            Check(!grid.IsPagingEnabled && !api.Projection.IsPagingEnabled, "local example has no paging");
            Check(api.Data.Current.VisibleRecords.Count == 24, "all 24 loaded records are available together");
            if(args.Contains("--immediate"))
            {
                model.Grid.SetValidators(new PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration.AssetBusinessRules());
                model.IsImmediate=true;await Flush();
                Check(Children<Button>(grid).Where(button=>new[]{"grid.commit","grid.cancel-all"}.Contains(AutomationProperties.GetAutomationId(button))).All(button=>!button.IsVisible),"Immediate mode hides both batch actions");
                grid.ScrollCellIntoView("EQ-001","Notes",GridScrollAlignment.Start,setCurrentCell:true);
                Check(grid.BeginEditCurrentCell(),"Immediate mode starts a native edit");await Flush();
                var presenter=Children<GridCellPresenter>(grid).Single(item=>item.CellData?.ColumnKey=="Notes" && item.CellData.IsEditing);
                Children<TextBox>(presenter).Single().Text="Immediate persisted note";await Flush();
                Check(new AssetRepository(file).ReadAll().Single(row=>row.Id=="EQ-001").Notes!="Immediate persisted note","Editing does not write before record acceptance");
                Check(grid.PostCurrentEdit(),"Native post accepts the record");
                async Task WaitForSave() {
                    var until=DateTime.UtcNow.AddSeconds(10);
                    while(DateTime.UtcNow<until && (model.Grid.IsBusy || model.Grid.LastSaveResult==null || model.Grid.HasPendingChanges && model.Grid.LastSaveResult.Succeeded)) {await Task.Delay(20);await Flush();}
                }
                await WaitForSave();
                Check(new AssetRepository(file).ReadAll().Single(row=>row.Id=="EQ-001").Notes=="Immediate persisted note","Native record acceptance writes SQLite automatically");
                Check(!model.Grid.HasPendingChanges,"Successful immediate save leaves no buffer");
                var changed=model.Grid.SetValue("EQ-004","UnitPrice",100000m);await Task.Delay(100);await Flush();
                Check(!changed && model.Grid.HasValidationErrors,"Whole-record business validation rejects invalid input before persistence");
                Check(new AssetRepository(file).ReadAll().Single(row=>row.Id=="EQ-004").UnitPrice!=100000m,"Rejected immediate save leaves SQLite unchanged");
                model.Grid.SetValue("EQ-004","UnitPrice",1000m);await WaitForSave();await Flush();
                Check(!model.Grid.HasPendingChanges && new AssetRepository(file).ReadAll().Single(row=>row.Id=="EQ-004").UnitPrice==1000m,"Correcting the record retries the automatic save");
                model.IsImmediate=false;await Flush();
                Check(Children<Button>(grid).Where(button=>new[]{"grid.commit","grid.cancel-all"}.Contains(AutomationProperties.GetAutomationId(button))).All(button=>button.IsVisible),"Buffered mode restores batch actions");
                window.Close();frame.Continue=false;return;
            }
            foreach (var (field, type) in new[] { ("Name", typeof(TextBox)), ("Category", typeof(ComboBox)),
                ("Quantity", typeof(TextBox)), ("UnitPrice", typeof(TextBox)), ("InService", typeof(CheckBox)),
                ("NextInspection", typeof(DatePicker)), ("Location", typeof(ComboBox)) }) {
                Check(grid.ScrollCellIntoView(field == "InService" ? "EQ-002" : "EQ-001", field, GridScrollAlignment.Start, setCurrentCell: true), "scroll to " + field);
                Check(grid.BeginEditCurrentCell(), "begin native editor " + field); await Flush();
                var presenter = Children<GridCellPresenter>(grid).Single(p => p.CellData?.ColumnKey == field && p.CellData.IsEditing);
                Check(Children<FrameworkElement>(presenter).Any(c => type.IsInstanceOfType(c)), "native " + type.Name + " for " + field);
                if(field == "InService")
                {
                    var editor=Children<CheckBox>(presenter).Single();editor.ApplyTemplate();
                    var box=(Border)editor.Template.FindName("Box",editor);
                    var row=(Border)editor.Template.FindName("Row",editor);
                    Check(Math.Abs(box.ActualWidth-20)<0.1 && Math.Abs(box.ActualHeight-20)<0.1,
                        "actual demo checkbox enlarges to 20x20");
                    Check(row.BorderThickness==new Thickness(0) && (row.Background as SolidColorBrush)?.Color.A==0,
                        "actual demo checkbox has no outer focus box");
                    Console.WriteLine("LOADED " + typeof(GridCellPresenter).Assembly.Location);
                    Console.WriteLine("LOADED " + typeof(Phiale.Design.Wpf.ThemeScope).Assembly.Location);
                    var capture=Environment.GetEnvironmentVariable("PHIALE_DEMO_SCREENSHOTS");
                    if(!string.IsNullOrEmpty(capture))
                    {
                        Directory.CreateDirectory(capture);var content=(FrameworkElement)window.Content;
                        var rect=new Rect(0,0,content.ActualWidth,content.ActualHeight);
                        var drawing=new DrawingVisual();
                        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(rect.Width),(int)Math.Ceiling(rect.Height),96,96,PixelFormats.Pbgra32);
                        using(var dc=drawing.RenderOpen()) { dc.DrawRectangle(window.Background,null,rect);dc.DrawRectangle(new VisualBrush(content),null,rect); }
                        bitmap.Render(drawing);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));
                        using var output=File.Create(Path.Combine(capture,"05-checkbox-edit.png"));png.Save(output);
                    }
                }

                grid.CancelCurrentEdit(); await Flush();
            }
            grid.ScrollCellIntoView("EQ-024", "Name", GridScrollAlignment.Start, setCurrentCell: true);await Flush();
            var remove=Children<Button>(grid).Single(button=>AutomationProperties.GetAutomationId(button)=="grid.remove");
            remove.Command.Execute(remove);await Flush();
            Check(grid.IsDeleteConfirmationOpen && !api.Editing.HasPendingEdits,"native Remove waits for embedded confirmation");
            var cancelDelete=Children<Button>(grid).Single(button=>AutomationProperties.GetAutomationId(button)=="grid.delete.cancel");
            cancelDelete.Command.Execute(cancelDelete);await Flush();
            Check(!grid.IsDeleteConfirmationOpen && !api.Editing.HasPendingEdits,"Cancel deletion preserves the row and buffer");
            remove.Command.Execute(remove);await Flush();
            var confirmDelete=Children<Button>(grid).Single(button=>AutomationProperties.GetAutomationId(button)=="grid.delete.confirm");
            var captureDelete=Environment.GetEnvironmentVariable("PHIALE_DEMO_SCREENSHOTS");
            if(!string.IsNullOrEmpty(captureDelete))
            {
                Directory.CreateDirectory(captureDelete);var content=(FrameworkElement)window.Content;var rect=new Rect(0,0,content.ActualWidth,content.ActualHeight);var drawing=new DrawingVisual();
                var bitmap=new RenderTargetBitmap((int)Math.Ceiling(rect.Width),(int)Math.Ceiling(rect.Height),96,96,PixelFormats.Pbgra32);
                using(var dc=drawing.RenderOpen()) {dc.DrawRectangle(window.Background,null,rect);dc.DrawRectangle(new VisualBrush(content),null,rect);}
                bitmap.Render(drawing);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(captureDelete,"05-delete-confirmation.png"));png.Save(output);
            }
            confirmDelete.Command.Execute(confirmDelete);await Flush();
            Check(api.Editing.GetPendingOperation("EQ-024")==PhialeGrid.Core.Commit.ChangeOperation.Remove,"Confirmed deletion is staged in the buffer");
            Check(new AssetRepository(file).ReadAll().Any(row=>row.Id=="EQ-024"),"Confirmation alone does not write SQLite");
            grid.UndoRemoveRecord("EQ-024");await Flush();Check(!api.Editing.HasPendingEdits,"Undo restores the staged deletion");
            grid.ScrollCellIntoView("EQ-001", "Name", GridScrollAlignment.Start, setCurrentCell: true); grid.BeginEditCurrentCell(); await Flush();
            var active = Children<GridCellPresenter>(grid).Single(p => p.CellData?.ColumnKey == "Name" && p.CellData.IsEditing);
            Children<TextBox>(active).Single().Text = "WPF — zmiana w buforze"; await Flush();
            Check(grid.PostCurrentEdit(), "posting native text editor changes only buffer"); await Flush();
            Console.WriteLine($"LABELS save={grid.CommitEditsText}; discard={grid.CancelEditsText}; cancelCell={grid.CancelEditText}");
            var repo = new AssetRepository(file);
            Check(api.Editing.HasPendingEdits && repo.ReadAll()[0].Name != "WPF — zmiana w buforze", "SQLite unchanged after native cell post");
            Check(grid.ChangePanelItems.Any(item => item.DetailsText.Contains("Laptop serwisowy") && item.DetailsText.Contains("WPF — zmiana w buforze")), "Changes panel contains original and current field values");
            Check(Children<PhialeChangePanel>(grid).Any(p => p.IsVisible) && model.Status.Contains("Oczekujące"), "Changes panel and application status expose pending edits");
            var cancel = Children<Button>(grid).Single(b => AutomationProperties.GetAutomationId(b) == "grid.cancel-all");
            Check(cancel.IsEnabled && cancel.Command.CanExecute(cancel), "Discard toolbar command available"); cancel.Command.Execute(cancel); await Flush();
            Check(!api.Editing.HasPendingEdits && repo.ReadAll()[0].Name != "WPF — zmiana w buforze", "native Discard leaves database unchanged");
            api.Editing.TrySetFieldValue("EQ-001", "Name", "WPF — zapis do SQLite"); await Flush();
            var save = Children<Button>(grid).Single(b => AutomationProperties.GetAutomationId(b) == "grid.commit");
            Check(save.IsEnabled && save.Command.CanExecute(save), "Save toolbar command available"); save.Command.Execute(save);
            var wait = DateTime.UtcNow;
            while (api.Editing.HasPendingEdits || api.Editing.IsCommitting) { if (DateTime.UtcNow - wait > TimeSpan.FromSeconds(15)) throw new TimeoutException("Native toolbar Save"); await Task.Delay(40); }
            Check(repo.ReadAll()[0].Name == "WPF — zapis do SQLite", "native Save command persists buffer after Discard");
            while (!grid.CanCloseRuntime) { if (DateTime.UtcNow - wait > TimeSpan.FromSeconds(15)) throw new TimeoutException("WPF operation cleanup"); await Task.Delay(40); }
            Check(grid.CanCloseRuntime, "window can close after the save pipeline finishes");
            model.Grid.SetValue("EQ-001", "Notes", "Pending close regression");await Flush();
            Check(api.Editing.HasPendingEdits,"prepare unsaved changes for Close");
            window.Close();
            Check(!wasClosed,"Closing is cancelled before deferred confirmation");
            if(requestedChoice==MessageBoxResult.Cancel) {
                await Flush();Check(!wasClosed && api.Editing.HasPendingEdits,"Cancel keeps window and pending edits");
                requestedChoice=MessageBoxResult.No;window.Close();
            }
            var closingStarted=DateTime.UtcNow;
            while(!wasClosed) { if(DateTime.UtcNow-closingStarted>TimeSpan.FromSeconds(5))throw new TimeoutException("Deferred close"); await Dispatcher.Yield(DispatcherPriority.ApplicationIdle); }
            Check(wasClosed && choices==(args.Contains("--close-cancel")?2:1),"discard and Close run once after the original Closing event returns");
            Check((repo.ReadAll()[0].Notes=="Pending close regression")==args.Contains("--close-save"),"Close persists only when Save was chosen");
            frame.Continue = false;
        } catch (Exception error) { failure = error; timer.Stop(); frame.Continue = false; }
    };
    try { window.Show(); timer.Start(); Dispatcher.PushFrame(frame); }
    finally { timer.Stop(); if(!wasClosed) {api.Editing.CancelAllEdits();window.Close();} app.Shutdown(); data.Dispose(); File.Delete(file); }
    if (failure != null) { Console.Error.WriteLine(failure); return 1; }
    return 0;
 }
}
