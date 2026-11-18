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
    public bool IsImmediate
    {
        get => Grid.SaveMode == GridSaveMode.Immediate;
        set { Grid.SaveMode = value ? GridSaveMode.Immediate : GridSaveMode.Buffered; PropertyChanged?.Invoke(this,new(string.Empty)); }
    }
    public bool IsBuffered { get => !IsImmediate; set { if(value) IsImmediate=false; } }
    public bool CanChangeSaveMode => !Grid.IsBusy && !Grid.IsEditing && !Grid.HasPendingChanges;
    public string ModeExplanation => IsImmediate
        ? "Zapis natychmiastowy: Enter/Zastosuj zatwierdza edycję rekordu i uruchamia zapis. Walidacja sprawdza cały rekord. Esc cofa bieżącą edycję. Nie ma przycisków zapisu całego bufora."
        : "Zapis buforowany: Enter/Zastosuj przenosi edycję do bufora. Zapisz utrwala wszystkie oczekujące zmiany; Odrzuć zmiany je wycofuje.";
    public string Status => Grid.IsValidating ? "Sprawdzanie reguł biznesowych przed zapisem…" :
        Grid.IsSaving ? "Zapisywanie do SQLite…" :
        !Grid.HasPendingChanges ? "Brak oczekujących zmian. SQLite zawiera ostatni zapisany stan." :
        $"Oczekujące zmiany: {Grid.PendingRecordCount}. " + (IsImmediate ? "Jeśli zapis został odrzucony, popraw rekord albo cofnij jego zmianę w panelu Zmiany." : "Zapisz albo Odrzuć zmiany.");
    private void GridChanged(object? sender,PropertyChangedEventArgs e)=>PropertyChanged?.Invoke(this,new(string.Empty));
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Dispose(){Grid.PropertyChanged-=GridChanged;Grid.Dispose();}
}
