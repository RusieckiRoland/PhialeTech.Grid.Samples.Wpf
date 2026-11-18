using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;
using System.Windows.Threading;
using PhialeGrid.Core;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Composition;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.Infrastructure;
using PhialeTech.Grid.Samples.Wpf.DataVirtualization.ViewModels;
using GridControl=PhialeTech.PhialeGrid.Wpf.Controls.PhialeGrid;
using WindowView=PhialeTech.Grid.Samples.Wpf.DataVirtualization.Views.MainWindow;

internal static class Program
{
 [STAThread] private static int Main()
 {
  var app=new PhialeTech.Grid.Samples.Wpf.DataVirtualization.App();app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
  int result=0;
  app.Dispatcher.BeginInvoke(async ()=>{
   using var composition=new StockGridComposition(new StockRepository(Path.Combine(AppContext.BaseDirectory,"Data/warehouse-1000000.sqlite")));
   using var api=GridApiFactory.CreateRuntime();
   var model=new StocksViewModel(composition.CreateBinding,api){SelectedSize=10000};
   var window=new WindowView(api,model){Opacity=0,ShowActivated=false,ShowInTaskbar=false};
   try {
    window.Show();await Wait(()=>api.Data.Current?.TotalCount==10000 && !api.Data.Current.IsBusy);
    var grid=Children<GridControl>(window).Single();
    Check(grid.EnableDataVirtualization && !grid.IsPagingEnabled,"02 starts with data virtualization and no paging");
    Check(api.Data.Current!.MaterializedVirtualRows.Count<=300,"initial read does not materialize the complete dataset");
    var scroll=Children<ScrollViewer>(grid).OrderByDescending(s=>s.ScrollableHeight).First();
    Check(scroll.ScrollableHeight>10000,"scrollbar represents the complete logical result");
    var surface=Children<GridSurfaceHost>(grid).Single();
    var oldCode=Children<GridCellPresenter>(grid).First(cell=>cell.CellData?.ColumnKey=="ProductCode").CellData.DisplayText;
    scroll.ScrollToVerticalOffset(scroll.ScrollableHeight);
    await Wait(()=>surface.IsLoadingPreview || api.Data.Current!.MaterializedVirtualRows.Any(pair=>pair.Key==9999));
    if(surface.IsLoadingPreview)
    {
        Check(Children<GridCellPresenter>(grid).Any(cell=>cell.CellData?.DisplayText==oldCode),"actual SQLite scroll retains the previous visible values while loading");
        var capture=Environment.GetEnvironmentVariable("PHIALE_DEMO_SCREENSHOTS");
        if(!string.IsNullOrEmpty(capture))
        {
            Directory.CreateDirectory(capture);var content=(FrameworkElement)window.Content;var rect=new Rect(0,0,content.ActualWidth,content.ActualHeight);var drawing=new DrawingVisual();
            using(var dc=drawing.RenderOpen()) {dc.DrawRectangle(window.Background,null,rect);dc.DrawRectangle(new VisualBrush(content),null,rect);}
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(rect.Width),(int)Math.Ceiling(rect.Height),96,96,PixelFormats.Pbgra32);bitmap.Render(drawing);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(capture,"02-scroll-preview.png"));png.Save(output);
        }
    }
    else Console.WriteLine("INFO SQLite completed before the intermediate frame; deterministic surface tests cover the animation interval.");

    await Wait(()=>api.Data.Current!.MaterializedVirtualRows.Any(pair=>pair.Key==9999)&&!api.Data.Current.IsBusy);
    Check(api.Data.Current!.MaterializedVirtualRows.Count<1000,"scroll to last record loads windows, not every intervening row");
    scroll.ScrollToVerticalOffset(0);
    await Wait(()=>!api.Data.Current!.IsBusy);
    grid.IsPagingEnabled=true;await grid.LoadDataAsync(0,100);await grid.GoToPageAsync(1);
    Check(grid.Paging.PageNumber==2 && grid.Paging.TotalRowCount==10000,"paging uses the same local query source");
    await grid.ChangePageSizeAsync(200);
    Check(grid.Paging.PageSize==200,"local source accepts the built-in 200-row page size");
    Console.WriteLine("PASS native scrolling and paging checks");
   } catch(Exception error){Console.Error.WriteLine(error);result=1;}
   finally {model.Dispose();window.Close();app.Shutdown();}
  });
  app.Run();return result;
 }
 private static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS "+message);}
 private static async Task Wait(Func<bool> ready){var until=DateTime.UtcNow.AddSeconds(60);while(!ready()){if(DateTime.UtcNow>until)throw new TimeoutException("Native window did not finish loading");await Task.Delay(20);await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);}await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);}
 private static IEnumerable<T> Children<T>(DependencyObject root) where T:DependencyObject{for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T value)yield return value;foreach(var nested in Children<T>(child))yield return nested;}}
}
