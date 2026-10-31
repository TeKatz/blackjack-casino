namespace BlackjackCasino.Core.MidnightMerge;

public static class MergeProgression
{
    public static IEnumerable<string> Earned(MergeCatalog catalog, long points) =>
        catalog.Areas.SelectMany(a => a.Milestones.Where(m => m.Points <= points).Select(m => $"{a.Id}/{m.Id}"));

    public static MergeMilestone Current(MergeArea area, long points) =>
        area.Milestones.LastOrDefault(m => m.Points <= points) ?? area.Milestones[0];

    public static MergeArea ActiveArea(MergeCatalog catalog, long points) =>
        catalog.Areas.LastOrDefault(area => area.Milestones.Count > 0 && area.Milestones[0].Points <= points) ?? catalog.Areas[0];

    public static double AreaProgress(MergeArea area, long points)
    {
        long start = area.Milestones[0].Points;
        long goal = area.Milestones[^1].Points;
        if (goal <= start) return 1;
        return Math.Clamp((points - start) / (double)(goal - start), 0, 1);
    }
}
