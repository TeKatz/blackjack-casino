namespace BlackjackCasino.Core.Empire;

public sealed record EmpireAchievementBoost(string ChallengeId, string Name, string? RoomId, decimal Percent);

/// <summary>
/// Permanent Empire production rewards granted by already-claimed player challenges.
/// Challenge claims remain the source of truth; Empire saves do not duplicate profile state.
/// </summary>
public static class EmpireAchievementBoostCatalog
{
    public static IReadOnlyList<EmpireAchievementBoost> All { get; } = Array.AsReadOnly<EmpireAchievementBoost>(
    [
        new("bj-blackjack-100", "Natural Born", "tables", .05m),
        new("bj-wager-1000000", "Million Dollar Table", "tables", .10m),
        new("reels-spins-1500", "Mechanical Memory", "slots", .05m),
        new("reels-spins-10000", "Clockwork Legend", "slots", .10m),
        new("manor-exit-10", "Escape Artist", "vip", .05m),
        new("manor-max-exit-1", "Impossible Door", "vip", .10m),
        new("neon-jackpots-5", "Vault Breaker", "slots", .075m),
        new("neon-grand-10", "Grand Vault Legend", "high-limit-slots", .15m),
        new("nine-pounce-25", "Pounce!", "slots", .05m),
        new("nine-frenzy-1", "All Nine", "high-limit-slots", .10m),
        new("lifetime-xp-1000000", "Casino Institution", null, .05m),
        new("lifetime-wager-10000000", "Ten Million in Motion", null, .10m),
    ]);

    public static IReadOnlyList<EmpireAchievementBoost> Active(IEnumerable<string>? claimedChallengeIds)
    {
        var claimed = (claimedChallengeIds ?? []).ToHashSet(StringComparer.Ordinal);
        return All.Where(boost => claimed.Contains(boost.ChallengeId)).ToArray();
    }

    public static decimal BonusForRoom(IEnumerable<string>? claimedChallengeIds, string roomId)
        => Active(claimedChallengeIds)
            .Where(boost => boost.RoomId is null || string.Equals(boost.RoomId, roomId, StringComparison.Ordinal))
            .Sum(boost => boost.Percent);
}
