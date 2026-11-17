using System.Windows;
using PhialeTech.PhialeGrid.Wpf.Surface;
using PhialeTech.Grid.Samples.Wpf.RowDetails.Views;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.Details;

// Only this WPF adapter knows how to turn the neutral descriptor into a UserControl.
public sealed class AssetDetailsFactory : IGridRowDetailContentFactory
{
    public FrameworkElement CreateContent(GridRowDetailWpfContext context) =>
        new AssetDetailsView { DataContext = context.ContentDescriptor };
}
