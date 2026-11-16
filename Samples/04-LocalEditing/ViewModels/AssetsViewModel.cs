using System.ComponentModel;
using PhialeGrid.Contracts;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.ViewModels;

public sealed class AssetsViewModel : INotifyPropertyChanged, IDisposable
{
    public AssetsViewModel(IGridContext<AssetRow> grid, string database)
    {
        Grid=grid;DatabasePath=database;
        Grid.PropertyChanged+=GridChanged;
    }
    public IGridContext<AssetRow> Grid { get; }
    public IReadOnlyList<IGridColumnDescriptor> Columns => AssetColumns.All;
    public GridDataBinding DataSession => Grid.DataSession;
    public string DatabasePath { get; }
    public string Status => Grid.IsValidating ? "Sprawdzanie reguł biznesowych przed zapisem…" :
        Grid.IsSaving ? "Zapisywanie do SQLite…" :
        !Grid.HasPendingChanges ? "Brak oczekujących zmian. SQLite zawiera ostatni zapisany stan." :
        $"Oczekujące zmiany: {Grid.PendingRecordCount}. Zapisz albo Odrzuć zmiany.";
    private void GridChanged(object? sender,PropertyChangedEventArgs e)=>PropertyChanged?.Invoke(this,new(nameof(Status)));
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Dispose(){Grid.PropertyChanged-=GridChanged;Grid.Dispose();}
}
