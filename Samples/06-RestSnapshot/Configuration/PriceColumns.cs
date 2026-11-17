using PhialeGrid.Contracts;
using PhialeGrid.Core.Columns;
using PhialeGrid.Core.Validation;
using PhialeTech.Grid.Samples.PriceReview.Contracts;
namespace PhialeTech.Grid.Samples.Wpf.RestSnapshot.Configuration;

public static class PriceColumns
{
    public static IReadOnlyList<IGridColumnDescriptor> All { get; } = new GridColumnsBuilder<PriceRow>()
        .Add(r => r.ProductCode, "Symbol", 145)
        .Add(r => r.ProductName, "Produkt", 290)
        .Add(r => r.Category, "Kategoria", 110)
        .Add(r => r.OldPurchasePrice, "Zakup: poprzednio", 130)
        .Add(r => r.NewPurchasePrice, "Zakup: nowa cena", 130)
        .Add(r => r.ChangePercent, "Zmiana %", 95)
        .Add(r => r.ProposedSalePrice, "Sprzedaż: propozycja", 150, c => c.Editable().Editor(GridColumnEditorKind.Decimal)
            .Validate(new DecimalValidationConstraints(minValue: 0, maxValue: 1000000, scale: 2)))
        .Add(r => r.MarginPercent, "Marża %", 95)
        .Add(r => r.ValidFrom, "Obowiązuje od", 135, c => c.Editable().Editor(GridColumnEditorKind.DatePicker).DateMode(GridDateTimeMode.DateOnly))
        .Add(r => r.Decision, "Decyzja", 125, c => c.Editable().Editor(GridColumnEditorKind.Combo)
            .Items(["Oczekuje", "Zatwierdź", "Odrzuć"], GridEditorItemsMode.RestrictToItems))
        .Add(r => r.Comment, "Uzasadnienie", 240, c => c.Editable().Editor(GridColumnEditorKind.Text)
            .Validate(new TextValidationConstraints(maxLength: 200)))
        .Build();
}
