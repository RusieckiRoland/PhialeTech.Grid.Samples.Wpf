using PhialeGrid.Contracts;
using PhialeGrid.Core.Editing;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;

namespace PhialeTech.Grid.Samples.Wpf.LocalEditing.Configuration;

// A relationship between fields belongs to the application, not the editor's numeric limits.
public sealed class AssetBusinessRules : IGridRecordValidator<AssetRow>
{
    public IReadOnlyList<GridValidationError> Validate(AssetRow proposed) =>
        proposed.Quantity * proposed.UnitPrice > 2_000_000m
        ? [new("", "Wartość pozycji (ilość × cena) nie może przekraczać 2 000 000 zł.")]
        : [];
}
