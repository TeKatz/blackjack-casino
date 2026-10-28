namespace BlackjackCasino.Core;

public sealed class SlotStatistics
{
    public long Spins { get; set; }
    public long Wins { get; set; }
    public long Pushes { get; set; }
    public decimal TotalWagered { get; set; }
    public decimal TotalReturned { get; set; }
    public decimal BiggestPayout { get; set; }
    public decimal NetProfit => TotalReturned - TotalWagered;
}

/// <summary>Shares level/season rewards without recording slots as blackjack hands.</summary>
public sealed class SlotProgression(ProgressionService progression)
{
    private readonly HashSet<Guid> processed = [];

    public ProgressionAward Process(SlotSpinResult spin)
    {
        ArgumentNullException.ThrowIfNull(spin);
        if (!processed.Add(spin.SpinId))
        {
            return ProgressionAward.None;
        }

        SlotStatistics stats = progression.State.Slots;
        stats.Spins++;
        stats.Wins += spin.Net > 0 ? 1 : 0;
        stats.Pushes += spin.Net == 0 ? 1 : 0;
        stats.TotalWagered += spin.Bet;
        stats.TotalReturned += spin.Payout;
        stats.BiggestPayout = Math.Max(stats.BiggestPayout, spin.Payout);

        // Same base and win bonus as a normal completed blackjack hand;
        // neither wager size nor jackpot size buys faster progression.
        long playerXp = 20 + (spin.Net > 0 ? 5 : 0);
        long seasonXp = 15 + (spin.Net > 0 ? 5 : 0);
        ProgressionAward player = progression.GrantPlayerExperience(playerXp);
        ProgressionAward season = progression.GrantSeasonExperience(progression.State.ActiveSeasonId, seasonXp);
        progression.State.LastPlayedAt = spin.CompletedAt;
        return new ProgressionAward(playerXp, seasonXp, player.LevelsGained, season.SeasonTiersGained,
            player.CoinsEarned + season.CoinsEarned, player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray());
    }
}
