namespace BlackjackCasino.Core;

public enum ChallengeCategory
{
    Blackjack,
    MidnightReels,
    MidnightManor,
}

public enum ChallengeMetric
{
    BlackjackRounds,
    BlackjackHands,
    BlackjackWins,
    BlackjackBlackjacks,
    BlackjackDoubles,
    BlackjackDoubleWins,
    BlackjackSplits,
    BlackjackSurrenders,
    BlackjackInsurance,
    BlackjackWagered,
    BlackjackNetProfit,
    BlackjackBiggestWin,
    BlackjackHighestBankroll,
    BlackjackBestWinStreak,

    ReelsSpins,
    ReelsWins,
    ReelsWagered,
    ReelsReturned,
    ReelsBiggestPayout,
    ReelsNetProfit,

    ManorPaidSpins,
    ManorFreeSpins,
    ManorWagered,
    ManorReturned,
    ManorBigWins,
    ManorFreeSessions,
    ManorBonuses,
    ManorExits,
    ManorMaximumExits,
    ManorCatches,
    ManorCashOuts,
    ManorBestMultiplier,
}

public sealed record ChallengeDefinition(
    string Id,
    ChallengeCategory Category,
    string Name,
    string Description,
    ChallengeMetric Metric,
    decimal Goal,
    long CoinReward);

public static class ChallengeCatalog
{
    private static readonly IReadOnlyList<ChallengeDefinition> Challenges = Array.AsReadOnly(new[]
    {
        // Blackjack — 25 challenges. Milestones intentionally overlap so ordinary laid-back play
        // always has something moving without forcing exotic rules or high stakes.
        C("bj-rounds-10", ChallengeCategory.Blackjack, "Take a Seat", "Complete 10 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 10, 60),
        C("bj-rounds-50", ChallengeCategory.Blackjack, "Settled In", "Complete 50 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 50, 120),
        C("bj-rounds-250", ChallengeCategory.Blackjack, "Table Regular", "Complete 250 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 250, 300),
        C("bj-rounds-1000", ChallengeCategory.Blackjack, "Night Shift", "Complete 1,000 Blackjack rounds.", ChallengeMetric.BlackjackRounds, 1000, 800),
        C("bj-hands-500", ChallengeCategory.Blackjack, "Many Hands", "Play 500 Blackjack hands, including split hands.", ChallengeMetric.BlackjackHands, 500, 350),
        C("bj-win-1", ChallengeCategory.Blackjack, "First Victory", "Win your first Blackjack hand.", ChallengeMetric.BlackjackWins, 1, 25),
        C("bj-win-25", ChallengeCategory.Blackjack, "Winning Ways", "Win 25 Blackjack hands.", ChallengeMetric.BlackjackWins, 25, 100),
        C("bj-win-100", ChallengeCategory.Blackjack, "Table Veteran", "Win 100 Blackjack hands.", ChallengeMetric.BlackjackWins, 100, 250),
        C("bj-win-500", ChallengeCategory.Blackjack, "House Nemesis", "Win 500 Blackjack hands.", ChallengeMetric.BlackjackWins, 500, 700),
        C("bj-blackjack-5", ChallengeCategory.Blackjack, "Natural Talent", "Hit 5 natural Blackjacks.", ChallengeMetric.BlackjackBlackjacks, 5, 100),
        C("bj-blackjack-25", ChallengeCategory.Blackjack, "Blackjack Hunter", "Hit 25 natural Blackjacks.", ChallengeMetric.BlackjackBlackjacks, 25, 250),
        C("bj-blackjack-100", ChallengeCategory.Blackjack, "Natural Born", "Hit 100 natural Blackjacks.", ChallengeMetric.BlackjackBlackjacks, 100, 700),
        C("bj-double-10", ChallengeCategory.Blackjack, "Double Down", "Double 10 hands.", ChallengeMetric.BlackjackDoubles, 10, 100),
        C("bj-double-win-10", ChallengeCategory.Blackjack, "Double Trouble", "Win 10 doubled hands.", ChallengeMetric.BlackjackDoubleWins, 10, 150),
        C("bj-double-win-50", ChallengeCategory.Blackjack, "Committed", "Win 50 doubled hands.", ChallengeMetric.BlackjackDoubleWins, 50, 450),
        C("bj-split-10", ChallengeCategory.Blackjack, "Split Decision", "Split 10 pairs.", ChallengeMetric.BlackjackSplits, 10, 120),
        C("bj-split-50", ChallengeCategory.Blackjack, "Split Personality", "Split 50 pairs.", ChallengeMetric.BlackjackSplits, 50, 400),
        C("bj-surrender-10", ChallengeCategory.Blackjack, "Know When to Fold", "Use surrender 10 times.", ChallengeMetric.BlackjackSurrenders, 10, 100),
        C("bj-insurance-10", ChallengeCategory.Blackjack, "Covered", "Take insurance 10 times.", ChallengeMetric.BlackjackInsurance, 10, 100),
        C("bj-wager-5000", ChallengeCategory.Blackjack, "Chips in Motion", "Wager $5,000 at Blackjack in total.", ChallengeMetric.BlackjackWagered, 5000, 150),
        C("bj-wager-25000", ChallengeCategory.Blackjack, "Serious Action", "Wager $25,000 at Blackjack in total.", ChallengeMetric.BlackjackWagered, 25000, 400),
        C("bj-profit-1000", ChallengeCategory.Blackjack, "Ahead of the House", "Reach $1,000 lifetime Blackjack net profit.", ChallengeMetric.BlackjackNetProfit, 1000, 300),
        C("bj-biggest-500", ChallengeCategory.Blackjack, "One Big Hand", "Record a $500+ Blackjack round win.", ChallengeMetric.BlackjackBiggestWin, 500, 250),
        C("bj-streak-5", ChallengeCategory.Blackjack, "Hot Streak", "Reach a 5-round win streak.", ChallengeMetric.BlackjackBestWinStreak, 5, 200),
        C("bj-streak-10", ChallengeCategory.Blackjack, "Unbroken", "Reach a 10-round win streak.", ChallengeMetric.BlackjackBestWinStreak, 10, 600),

        // Midnight Reels — 10 challenges.
        C("reels-spins-25", ChallengeCategory.MidnightReels, "Warm Up the Reels", "Play 25 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 25, 60),
        C("reels-spins-100", ChallengeCategory.MidnightReels, "One More Spin", "Play 100 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 100, 150),
        C("reels-spins-500", ChallengeCategory.MidnightReels, "Reel Regular", "Play 500 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 500, 450),
        C("reels-wins-25", ChallengeCategory.MidnightReels, "Lights on the Line", "Win 25 Midnight Reels spins.", ChallengeMetric.ReelsWins, 25, 120),
        C("reels-wins-100", ChallengeCategory.MidnightReels, "Classic Luck", "Win 100 Midnight Reels spins.", ChallengeMetric.ReelsWins, 100, 350),
        C("reels-wager-5000", ChallengeCategory.MidnightReels, "Coin Churn", "Wager $5,000 on Midnight Reels.", ChallengeMetric.ReelsWagered, 5000, 180),
        C("reels-return-10000", ChallengeCategory.MidnightReels, "Back Through the Tray", "Return $10,000 from Midnight Reels spins.", ChallengeMetric.ReelsReturned, 10000, 250),
        C("reels-payout-500", ChallengeCategory.MidnightReels, "Bell Ringer", "Land a $500+ Midnight Reels payout.", ChallengeMetric.ReelsBiggestPayout, 500, 250),
        C("reels-profit-1000", ChallengeCategory.MidnightReels, "Old-School Edge", "Reach $1,000 Midnight Reels net profit.", ChallengeMetric.ReelsNetProfit, 1000, 350),
        C("reels-spins-1500", ChallengeCategory.MidnightReels, "Mechanical Memory", "Play 1,500 Midnight Reels spins.", ChallengeMetric.ReelsSpins, 1500, 900),

        // Midnight Manor — 10 challenges.
        C("manor-spins-25", ChallengeCategory.MidnightManor, "Invitation Accepted", "Play 25 Midnight Manor paid spins.", ChallengeMetric.ManorPaidSpins, 25, 60),
        C("manor-spins-100", ChallengeCategory.MidnightManor, "The House Knows You", "Play 100 Midnight Manor paid spins.", ChallengeMetric.ManorPaidSpins, 100, 160),
        C("manor-free-50", ChallengeCategory.MidnightManor, "Haunted Night", "Play 50 Manor Free Spins.", ChallengeMetric.ManorFreeSpins, 50, 180),
        C("manor-free-sessions-5", ChallengeCategory.MidnightManor, "Moonlit", "Trigger 5 Free Spin sessions.", ChallengeMetric.ManorFreeSessions, 5, 150),
        C("manor-bonus-5", ChallengeCategory.MidnightManor, "Open the Door", "Trigger Escape the Manor 5 times.", ChallengeMetric.ManorBonuses, 5, 180),
        C("manor-exit-1", ChallengeCategory.MidnightManor, "Made It Out", "Reach the Moonlit Gate once.", ChallengeMetric.ManorExits, 1, 250),
        C("manor-exit-10", ChallengeCategory.MidnightManor, "Escape Artist", "Reach the Moonlit Gate 10 times.", ChallengeMetric.ManorExits, 10, 700),
        C("manor-max-exit-1", ChallengeCategory.MidnightManor, "Impossible Door", "Escape through a 1,000× Moonlit Gate.", ChallengeMetric.ManorMaximumExits, 1, 1000),
        C("manor-caught-5", ChallengeCategory.MidnightManor, "The Manor Found You", "Get caught by the ghost 5 times.", ChallengeMetric.ManorCatches, 5, 100),
        C("manor-best-250", ChallengeCategory.MidnightManor, "Deep into the Manor", "Finish a Manor run at 250× or more.", ChallengeMetric.ManorBestMultiplier, 250, 500),
    });

    public static IReadOnlyList<ChallengeDefinition> All => Challenges;

    public static IReadOnlyList<ChallengeDefinition> For(ChallengeCategory category) =>
        Challenges.Where(x => x.Category == category).ToArray();

    public static ChallengeDefinition? TryGet(string id) =>
        Challenges.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));

    public static decimal Progress(PlayerProfileState profile, ChallengeDefinition challenge)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(challenge);
        StatisticsSnapshot blackjack = profile.LifetimeStatistics;
        SlotStatistics reels = profile.Slots;
        Manor.ManorStatistics manor = profile.Manor;

        return challenge.Metric switch
        {
            ChallengeMetric.BlackjackRounds => blackjack.RoundsPlayed,
            ChallengeMetric.BlackjackHands => blackjack.HandsPlayed,
            ChallengeMetric.BlackjackWins => blackjack.HandsWon,
            ChallengeMetric.BlackjackBlackjacks => blackjack.Blackjacks,
            ChallengeMetric.BlackjackDoubles => blackjack.Doubles,
            ChallengeMetric.BlackjackDoubleWins => blackjack.DoubleWins,
            ChallengeMetric.BlackjackSplits => blackjack.Splits,
            ChallengeMetric.BlackjackSurrenders => blackjack.Surrenders,
            ChallengeMetric.BlackjackInsurance => blackjack.InsuranceTaken,
            ChallengeMetric.BlackjackWagered => blackjack.TotalWagered,
            ChallengeMetric.BlackjackNetProfit => Math.Max(0, blackjack.NetProfit),
            ChallengeMetric.BlackjackBiggestWin => blackjack.BiggestWin,
            ChallengeMetric.BlackjackHighestBankroll => blackjack.HighestBankroll,
            ChallengeMetric.BlackjackBestWinStreak => blackjack.BestWinStreak,

            ChallengeMetric.ReelsSpins => reels.Spins,
            ChallengeMetric.ReelsWins => reels.Wins,
            ChallengeMetric.ReelsWagered => reels.TotalWagered,
            ChallengeMetric.ReelsReturned => reels.TotalReturned,
            ChallengeMetric.ReelsBiggestPayout => reels.BiggestPayout,
            ChallengeMetric.ReelsNetProfit => Math.Max(0, reels.NetProfit),

            ChallengeMetric.ManorPaidSpins => manor.PaidSpins,
            ChallengeMetric.ManorFreeSpins => manor.FreeSpins,
            ChallengeMetric.ManorWagered => manor.TotalWagered,
            ChallengeMetric.ManorReturned => manor.TotalReturned,
            ChallengeMetric.ManorBigWins => manor.BigWins,
            ChallengeMetric.ManorFreeSessions => manor.FreeSpinsTriggered,
            ChallengeMetric.ManorBonuses => manor.ManorBonuses,
            ChallengeMetric.ManorExits => manor.Exits,
            ChallengeMetric.ManorMaximumExits => manor.MaximumExits,
            ChallengeMetric.ManorCatches => manor.Catches,
            ChallengeMetric.ManorCashOuts => manor.CashOuts,
            ChallengeMetric.ManorBestMultiplier => manor.BestManorMultiplier,
            _ => 0,
        };
    }

    public static bool Complete(PlayerProfileState profile, ChallengeDefinition challenge) =>
        Progress(profile, challenge) >= challenge.Goal;

    private static ChallengeDefinition C(
        string id,
        ChallengeCategory category,
        string name,
        string description,
        ChallengeMetric metric,
        decimal goal,
        long coins) => new(id, category, name, description, metric, goal, coins);
}
