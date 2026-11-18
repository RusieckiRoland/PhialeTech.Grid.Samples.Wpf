using PhialeGrid.Contracts;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Validation;
using PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;

namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.ViewModels;

public sealed class LocationsViewModel(IGridContext<LocationRow> grid, string databasePath) : IDisposable
{
    public static IReadOnlyList<IGridColumnDescriptor> ColumnDefinitions { get; } = new GridColumnsBuilder<LocationRow>()
        // A stable, read-only key keeps the master/detail relationship intact.
        .Add(location => location.Id, "Lokalizacja", 190)
        .Add(location => location.Contact, "Osoba kontaktowa", 200, column => column.Editable()
            .Editor(GridColumnEditorKind.Text).Validate(new TextValidationConstraints(required: true, maxLength: 80)))
        .Add(location => location.Phone, "Telefon", 145, column => column.Editable()
            .Editor(GridColumnEditorKind.Text).Validate(new TextValidationConstraints(maxLength: 40)))
        .Add(location => location.Notes, "Uwagi do lokalizacji", 260, column => column.Editable()
            .Editor(GridColumnEditorKind.Text).Validate(new TextValidationConstraints(maxLength: 200)))
        .Build();

    // Context exposes the master session to XAML and ViewModel code, as in lesson 05.
    public IGridContext<LocationRow> Grid { get; } = grid;
    public IReadOnlyList<IGridColumnDescriptor> Columns => ColumnDefinitions;
    public string DatabasePath { get; } = databasePath;
    public void Dispose() => Grid.Dispose();
}
