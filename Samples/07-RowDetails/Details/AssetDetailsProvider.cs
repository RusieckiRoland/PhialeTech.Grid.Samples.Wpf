using PhialeGrid.Core.Details;
using PhialeTech.Grid.Samples.Wpf.LocalEditing.Models;
using PhialeTech.Grid.Samples.Wpf.RowDetails.ViewModels;
namespace PhialeTech.Grid.Samples.Wpf.RowDetails.Details;

// A neutral descriptor: data + row identity + height. No WPF controls here.
public sealed class AssetDetailsProvider : IGridRowDetailProvider
{
    public bool HasDetail(GridRowDetailRequest request) => request.Record is AssetRow;
    public GridRowDetailDescriptor CreateDetail(GridRowDetailRequest request) => new(
        "asset-card:" + request.RecordKey, request.RowKey, GridRowDetailHeightPolicy.Fixed(180),
        new AssetDetailsViewModel((AssetRow)request.Record));
}
