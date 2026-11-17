using PhialeGrid.Contracts;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Validation;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;

public static class AssetColumns
{
    public static readonly string[] Categories = ["Elektronika", "Narzędzia", "BHP", "Biuro"];
    public static readonly string[] Locations = ["Gdańsk", "Łódź", "Poznań", "Wrocław"];
    public static IReadOnlyList<IGridColumnDescriptor> All { get; } = new GridColumnsBuilder<AssetRow>()
        .Add(row => row.Id, "Kod", 95)
        .Add(row => row.Name, "Nazwa", 230, column => column.Editable().Editor(GridColumnEditorKind.Text)
            .Validate(new TextValidationConstraints(required: true, maxLength: 80, trimBeforeValidation: true)))
        .Add(row => row.Category, "Kategoria", 140, column => column.Editable().Editor(GridColumnEditorKind.Combo)
            .Items(Categories, GridEditorItemsMode.RestrictToItems)
            .Validate(new TextValidationConstraints(required: true, allowedValues: Categories)))
        .Add(row => row.Quantity, "Ilość", 90, column => column.Editable().Editor(GridColumnEditorKind.Integer)
            .Validate(new IntegerValidationConstraints(minValue: 0, maxValue: 500)))
        .Add(row => row.UnitPrice, "Cena jedn.", 115, column => column.Editable().Editor(GridColumnEditorKind.Decimal)
            .Validate(new DecimalValidationConstraints(minValue: 0, maxValue: 100000, scale: 2)))
        .Add(row => row.InService, "W użyciu", 95, column => column.Editable().Editor(GridColumnEditorKind.CheckBox))
        .Add(row => row.NextInspection, "Następny przegląd", 155, column => column.Editable()
            .Editor(GridColumnEditorKind.DatePicker).DateMode(GridDateTimeMode.DateOnly)
            .Validate(new DateValidationConstraints(minDate: new DateTime(2000, 1, 1), maxDate: new DateTime(2100, 12, 31))))
        .Add(row => row.Location, "Lokalizacja", 140, column => column.Editable().Editor(GridColumnEditorKind.Autocomplete)
            .Items(Locations).Validate(new TextValidationConstraints(required: true, maxLength: 60)))
        .Add(row => row.Notes, "Uwagi", 240, column => column.Editable().Editor(GridColumnEditorKind.Text)
            .Validate(new TextValidationConstraints(maxLength: 200)))
        .Build();
}
