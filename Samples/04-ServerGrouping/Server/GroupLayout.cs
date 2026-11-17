namespace PhialeTech.Grid.Samples.HttpRest.Server;

internal sealed record GroupBlock(int Start, int Length, string Kind, StockGroup? Group, int RecordBase = 0);

// Positions are computed from counts. Data records are not needed to skip a subtree.
internal sealed class GroupLayout
{
    public List<GroupBlock> Blocks { get; } = [];
    public List<StockGroup> AllGroups { get; } = [];
    public int VisibleCount { get; private set; }
    private int _recordCursor;

    public GroupLayout(List<StockGroup> roots, Totals total, GroupRequest request)
    {
        void Collect(StockGroup group) { AllGroups.Add(group); foreach (var child in group.Children) Collect(child); }
        foreach (var root in roots) Collect(root);
        if (request.GroupBy.Length == 0) {
            Blocks.Add(new(0, total.Count, "records", null));
            VisibleCount = total.Count;
        } else foreach (var root in roots) Append(root, request);
    }

    private void Append(StockGroup group, GroupRequest request)
    {
        Blocks.Add(new(VisibleCount++, 1, "group", group));
        if (!request.Collapsed.Contains(group.Id, StringComparer.Ordinal)) {
            if (group.Children.Count > 0) {
                foreach (var child in group.Children) Append(child, request);
            } else {
                Blocks.Add(new(VisibleCount, group.Totals.Count, "records", group, _recordCursor));
                VisibleCount += group.Totals.Count;
                _recordCursor += group.Totals.Count;
            }
            if (request.ShowSummaries) Blocks.Add(new(VisibleCount++, 1, "summary", group));
        } else _recordCursor += group.Totals.Count;
        group.EndIndex = VisibleCount - 1;
    }
}
