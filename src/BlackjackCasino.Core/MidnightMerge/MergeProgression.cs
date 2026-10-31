namespace BlackjackCasino.Core.MidnightMerge;

public static class MergeProgression
{
    public static IEnumerable<string> Earned(MergeCatalog catalog, long points) =>
        catalog.Areas.SelectMany(a => a.Milestones.Where(m => m.Points <= points).Select(m => $"{a.Id}/{m.Id}"));
    public static MergeMilestone Current(MergeArea area, long points) => area.Milestones.Last(m => m.Points <= points);
}
