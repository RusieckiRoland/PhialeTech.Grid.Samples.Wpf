namespace PhialeTech.Grid.Samples.Wpf.NestedGrid.Models;

// The location key never changes. Contact fields can be edited without moving its assets.
// Children have their own sessions; they are not mutable properties of a master working copy.
public sealed record LocationRow
{
    public string Id { get; init; } = "";
    public string Contact { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Notes { get; set; } = "";
}
