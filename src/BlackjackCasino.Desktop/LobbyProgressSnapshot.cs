using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

/// <summary>One cheap data pass shared by the lobby and header; never creates off-screen menus.</summary>
internal sealed record LobbyProgressSnapshot(
    int SeasonTier, int SeasonNextTier, long SeasonExperience, long SeasonRequired, bool SeasonComplete,
    string? ChallengeId, string ChallengeName, decimal ChallengeProgress, decimal ChallengeGoal,
    int ReadyCount, long ReadyCoins)
{
    internal double SeasonFraction => SeasonComplete ? 1 : Math.Clamp((double)SeasonExperience / Math.Max(1, SeasonRequired), 0, 1);
    internal double ChallengeFraction => ChallengeId is null ? 1 : Math.Clamp((double)(ChallengeProgress / Math.Max(1, ChallengeGoal)), 0, 1);

    internal static LobbyProgressSnapshot From(PlayerProfileState profile)
    {
        SeasonDefinition season = SeasonCatalog.Get(profile.ActiveSeasonId);
        long xp = Math.Max(0, profile.Seasons.GetValueOrDefault(season.Id)?.Experience ?? 0);
        SeasonTierDefinition? next = season.Tiers.FirstOrDefault(t => t.RequiredExperience > xp);
        long floor = season.Tiers.LastOrDefault(t => t.RequiredExperience <= xp)?.RequiredExperience ?? 0;
        int reached = season.Tiers.Count(t => t.RequiredExperience <= xp);
        var claimed = profile.ClaimedChallengeIds.ToHashSet(StringComparer.Ordinal);
        ChallengeDefinition? closest = null;
        decimal value = 0, fraction = -1;
        int ready = 0; long coins = 0;
        foreach (ChallengeDefinition challenge in ChallengeCatalog.All)
        {
            if (claimed.Contains(challenge.Id)) continue;
            decimal progress = Math.Max(0, ChallengeCatalog.Progress(profile, challenge));
            if (progress >= challenge.Goal) { ready++; coins += challenge.CoinReward; continue; }
            decimal ratio = progress / Math.Max(1, challenge.Goal);
            if (ratio <= fraction) continue;
            fraction = ratio; closest = challenge; value = progress;
        }
        return new(reached, next?.Tier ?? reached, xp - floor, next is null ? 1 : next.RequiredExperience - floor,
            next is null, closest?.Id, closest?.Name ?? "All challenges complete", value, closest?.Goal ?? 1, ready, coins);
    }
}
