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
    public const decimal AreaCompletionBankrollReward = 250m;

    public MergeProfileSyncResult Sync(MergeGameState state, MergeCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        catalog ??= MergeCatalog.Default;
        MidnightMergeStatistics stats = progression.State.MidnightMerge;
        stats.Normalize();

        bool changed = false;
        long spawned = Math.Max(0, state.Producers.Sum(x => x.SpawnCount));
        if (spawned > stats.ItemsSpawned) { stats.ItemsSpawned = spawned; changed = true; }
        if (state.TotalMerges > stats.Merges) { stats.Merges = state.TotalMerges; changed = true; }
        if (state.Receipts.Count > stats.OrdersCompleted) { stats.OrdersCompleted = state.Receipts.Count; changed = true; }
        if (state.RenovationPoints > stats.RenovationPointsEarned) { stats.RenovationPointsEarned = state.RenovationPoints; changed = true; }
        if (state.HighestTierCreated > stats.HighestTierCreated) { stats.HighestTierCreated = state.HighestTierCreated; changed = true; }

        HashSet<string> rewardableMilestones = catalog.Areas
            .SelectMany(area => area.Milestones.Skip(1).Select(milestone => $"{area.Id}/{milestone.Id}"))
            .ToHashSet(StringComparer.Ordinal);
        string[] earnedMilestones = state.UnlockedMilestones
            .Where(rewardableMilestones.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (earnedMilestones.LongLength > stats.MilestonesReached) { stats.MilestonesReached = earnedMilestones.LongLength; changed = true; }

        string[] completedAreas = catalog.Areas
            .Where(area => area.Milestones.Count > 0 && state.UnlockedMilestones.Contains($"{area.Id}/{area.Milestones[^1].Id}", StringComparer.Ordinal))
            .Select(area => area.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (completedAreas.LongLength > stats.AreasCompleted) { stats.AreasCompleted = completedAreas.LongLength; changed = true; }

        long playerXp = 0, seasonXp = 0;
        decimal bankroll = 0m;
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
            if (stats.RewardedMilestoneIds.Contains(id)
                || !TryMilestone(catalog, id, out _, out MergeMilestone? milestone)
                || milestone is null)
                continue;
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
            bankroll = checked(bankroll + AreaCompletionBankrollReward);
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
            player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray(),
            bankroll + player.BankrollEarned + season.BankrollEarned);
        return new(true, award, ordersRewarded, milestonesRewarded, areasRewarded);
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
