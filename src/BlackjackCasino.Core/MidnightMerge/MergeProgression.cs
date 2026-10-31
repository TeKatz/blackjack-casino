namespace BlackjackCasino.Core.MidnightMerge;

public static class MergeProgression
{
    public static IEnumerable<string> Earned(MergeCatalog catalog, long points) =>
        catalog.Areas.SelectMany(a => a.Milestones.Where(m => m.Points <= points).Select(m => $"{a.Id}/{m.Id}"));

    public static MergeMilestone Current(MergeArea area, long points) =>
        area.Milestones.LastOrDefault(m => m.Points <= points) ?? area.Milestones[0];

    public static MergeMilestone? Next(MergeArea area, long points) =>
        area.Milestones.FirstOrDefault(m => m.Points > points);

    public static MergeArea ActiveArea(MergeCatalog catalog, long points) =>
        catalog.Areas.LastOrDefault(area => area.Milestones.Count > 0 && area.Milestones[0].Points <= points) ?? catalog.Areas[0];

    public static double AreaProgress(MergeArea area, long points)
    {
        long start = area.Milestones[0].Points;
        long goal = area.Milestones[^1].Points;
        if (goal <= start) return 1;
        return Math.Clamp((points - start) / (double)(goal - start), 0, 1);
    }

    /// <summary>
    /// Progress inside the current restoration step rather than across the whole area.
    /// This is intentionally area-agnostic so every future Midnight Merge level can use
    /// the same "next upgrade" presentation without adding level-specific UI rules.
    /// </summary>
    public static double UpgradeProgress(MergeArea area, long points)
    {
        MergeMilestone current = Current(area, points);
        MergeMilestone? next = Next(area, points);
        if (next is null) return 1;
        long span = next.Points - current.Points;
        if (span <= 0) return 1;
        return Math.Clamp((points - current.Points) / (double)span, 0, 1);
    }

    public static long PointsUntilNext(MergeArea area, long points)
    {
        MergeMilestone? next = Next(area, points);
        return next is null ? 0 : Math.Max(0, next.Points - points);
    }
}
