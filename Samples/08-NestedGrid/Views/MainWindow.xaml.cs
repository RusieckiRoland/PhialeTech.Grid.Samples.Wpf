using System.Windows;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Composition;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Configuration;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels;

namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Views;

public partial class MainWindow : Window
{
    public MainWindow(LocationsViewModel model, LocationGridComposition source)
    {
        DataContext = model;
        InitializeComponent();
        LocationsGrid.BindDetailGrid(AssetRelation.Create(source, action => Dispatcher.BeginInvoke(action)), child => {
            // This is a full Grid with its own standard toolbar, editors and buffered Save.
            child.CapabilityPolicy = new NestedExamplePolicy();
            child.ShowDiagnosticStatus = false;
            child.IsGridReadOnly = false;
            child.LanguageCode = "pl";
            child.ShowFilterRow = true;
            child.IsPagingEnabled = false;
            child.HideUnavailableMenuItems = true;
        });
        Closing += (_, args) => {
            // Do not silently lose a collapsed child's pending changes.
            // Keep this lesson explicit: the user saves/discards in each owning table.
            if (!LocationsGrid.CanCloseRuntime || model.Grid.IsEditing || source.HasActiveOperations || source.HasPendingChanges)
            {
                args.Cancel = true;
                CloseNotice.Visibility = Visibility.Visible;
            }
        };
        Closed += (_, _) => { LocationsGrid.CloseRuntime(); model.Dispose(); };
    }
}
