namespace BlackjackCasino.Core.MidnightMerge;

public sealed class MidnightMergeStatistics
{
    public HashSet<Guid> RewardedOrderIds { get; set; } = [];
    public HashSet<string> RewardedMilestoneIds { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> RewardedAreaIds { get; set; } = new(StringComparer.Ordinal);
    public long ItemsSpawned { get; set; }
    public long Merges { get; set; }
    public long OrdersCompleted { get; set; }
    public long RenovationPointsEarned { get; set; }
    public long MilestonesReached { get; set; }
    public long AreasCompleted { get; set; }
    public int HighestTierCreated { get; set; }

    public void Normalize()
    {
        RewardedOrderIds ??= [];
        RewardedOrderIds.Remove(Guid.Empty);
        RewardedMilestoneIds ??= new(StringComparer.Ordinal);
        RewardedAreaIds ??= new(StringComparer.Ordinal);
        RewardedMilestoneIds = RewardedMilestoneIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
        RewardedAreaIds = RewardedAreaIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
        ItemsSpawned = Math.Max(0, ItemsSpawned);
        Merges = Math.Max(0, Merges);
        OrdersCompleted = Math.Max(0, OrdersCompleted);
        RenovationPointsEarned = Math.Max(0, RenovationPointsEarned);
        MilestonesReached = Math.Max(0, MilestonesReached);
        AreasCompleted = Math.Max(0, AreasCompleted);
        HighestTierCreated = Math.Max(0, HighestTierCreated);
    }
}

public sealed record MergeProfileSyncResult(
    bool Changed,
    ProgressionAward Award,
    int OrdersRewarded,
    int MilestonesRewarded,
    int AreasRewarded)
{
    public static MergeProfileSyncResult None { get; } = new(false, ProgressionAward.None, 0, 0, 0);
}

/// <summary>
/// Synchronizes durable Midnight Merge state into the global profile. Reward ledgers make the
/// synchronization safe to repeat after reloads or after a profile write failed following a board save.
/// </summary>
public sealed class MergePlayerProgression(ProgressionService progression)
{
    public static long OrderPlayerExperience(long renovationReward) => Math.Clamp(25L + renovationReward * 2L, 40L, 500L);
    public static long OrderSeasonExperience(long renovationReward) => Math.Max(20L, OrderPlayerExperience(renovationReward) / 2L);
    public static long MilestonePlayerExperience(long points) => Math.Clamp(100L + points * 5L, 150L, 1_000L);
    public static long MilestoneSeasonExperience(long points) => MilestonePlayerExperience(points) / 2L;
    public const long AreaCompletionPlayerExperience = 500;
    public const long AreaCompletionSeasonExperience = 250;

    public MergeProfileSyncResult Sync(MergeGameState state, MergeCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        catalog ??= MergeCatalog.Default;
        MidnightMergeStatistics stats = progression.State.MidnightMerge;
        stats.Normalize();

        bool changed = false;
        changed |= Raise(ref stats.ItemsSpawned, state.Producers.Sum(x => x.SpawnCount));
        changed |= Raise(ref stats.Merges, state.TotalMerges);
        changed |= Raise(ref stats.OrdersCompleted, state.Receipts.Count);
        changed |= Raise(ref stats.RenovationPointsEarned, state.RenovationPoints);
        changed |= Raise(ref stats.HighestTierCreated, state.HighestTierCreated);

        string[] earnedMilestones = state.UnlockedMilestones
            .Where(id => TryMilestone(catalog, id, out _, out MergeMilestone? milestone) && milestone.Points > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        changed |= Raise(ref stats.MilestonesReached, earnedMilestones.LongLength);

        string[] completedAreas = catalog.Areas
            .Where(area => area.Milestones.Count > 0 && state.UnlockedMilestones.Contains($"{area.Id}/{area.Milestones[^1].Id}", StringComparer.Ordinal))
            .Select(area => area.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        changed |= Raise(ref stats.AreasCompleted, completedAreas.LongLength);

        long playerXp = 0, seasonXp = 0;
        int ordersRewarded = 0, milestonesRewarded = 0, areasRewarded = 0;

        foreach (MergeCompletionReceipt receipt in state.Receipts)
        {
            if (stats.RewardedOrderIds.Contains(receipt.OrderId)) continue;
            MergeOrderDefinition? order = catalog.Orders.FirstOrDefault(x => x.Id == receipt.DefinitionId);
            if (order is null) continue;
            stats.RewardedOrderIds.Add(receipt.OrderId);
            playerXp = checked(playerXp + OrderPlayerExperience(order.Reward));
            seasonXp = checked(seasonXp + OrderSeasonExperience(order.Reward));
            ordersRewarded++;
            changed = true;
        }

        foreach (string id in earnedMilestones)
        {
            if (stats.RewardedMilestoneIds.Contains(id) || !TryMilestone(catalog, id, out _, out MergeMilestone? milestone)) continue;
            stats.RewardedMilestoneIds.Add(id);
            playerXp = checked(playerXp + MilestonePlayerExperience(milestone.Points));
            seasonXp = checked(seasonXp + MilestoneSeasonExperience(milestone.Points));
            milestonesRewarded++;
            changed = true;
        }

        foreach (string areaId in completedAreas)
        {
            if (!stats.RewardedAreaIds.Add(areaId)) continue;
            playerXp = checked(playerXp + AreaCompletionPlayerExperience);
            seasonXp = checked(seasonXp + AreaCompletionSeasonExperience);
            areasRewarded++;
            changed = true;
        }

        if (playerXp <= 0 && seasonXp <= 0)
            return new(changed, ProgressionAward.None, ordersRewarded, milestonesRewarded, areasRewarded);

        ProgressionAward player = progression.GrantPlayerExperience(playerXp);
        ProgressionAward season = progression.GrantSeasonExperience(progression.State.ActiveSeasonId, seasonXp);
        progression.State.LastPlayedAt = DateTimeOffset.UtcNow;
        var award = new ProgressionAward(
            playerXp,
            seasonXp,
            player.LevelsGained,
            season.SeasonTiersGained,
            checked(player.CoinsEarned + season.CoinsEarned),
            player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray());
        return new(true, award, ordersRewarded, milestonesRewarded, areasRewarded);
    }

    private static bool Raise(ref long target, long candidate)
    {
        candidate = Math.Max(0, candidate);
        if (candidate <= target) return false;
        target = candidate;
        return true;
    }

    private static bool Raise(ref int target, int candidate)
    {
        candidate = Math.Max(0, candidate);
        if (candidate <= target) return false;
        target = candidate;
        return true;
    }

    private static bool TryMilestone(MergeCatalog catalog, string id, out MergeArea? area, out MergeMilestone? milestone)
    {
        foreach (MergeArea candidate in catalog.Areas)
        {
            string prefix = candidate.Id + "/";
            if (!id.StartsWith(prefix, StringComparison.Ordinal)) continue;
            MergeMilestone? found = candidate.Milestones.FirstOrDefault(x => string.Equals(x.Id, id[prefix.Length..], StringComparison.Ordinal));
            if (found is null) break;
            area = candidate;
            milestone = found;
            return true;
        }
        area = null;
        milestone = null;
        return false;
    }
}
