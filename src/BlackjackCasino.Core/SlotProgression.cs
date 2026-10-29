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
        return ProcessCore(spin.SpinId, spin.CompletedAt, spin.Bet, spin.Payout, spin.Net);
    }

    public ProgressionAward Process(NeonVaultSpinResult spin)
    {
        ArgumentNullException.ThrowIfNull(spin);
        return ProcessCore(spin.SpinId, spin.CompletedAt, spin.Stake, spin.Payout, spin.Net);
    }

    private ProgressionAward ProcessCore(Guid spinId, DateTimeOffset completedAt, decimal wagered, decimal returned, decimal net)
    {
        if (!processed.Add(spinId))
        {
            return ProgressionAward.None;
        }

        SlotStatistics stats = progression.State.Slots;
        stats.Spins++;
        stats.Wins += net > 0 ? 1 : 0;
        stats.Pushes += net == 0 ? 1 : 0;
        stats.TotalWagered += wagered;
        stats.TotalReturned += returned;
        stats.BiggestPayout = Math.Max(stats.BiggestPayout, returned);

        // Same base and win bonus as a normal completed blackjack hand; neither wager size,
        // jackpot size nor a free spin buys faster progression.
        long playerXp = 20 + (net > 0 ? 5 : 0);
        long seasonXp = 15 + (net > 0 ? 5 : 0);
        ProgressionAward player = progression.GrantPlayerExperience(playerXp);
        ProgressionAward season = progression.GrantSeasonExperience(progression.State.ActiveSeasonId, seasonXp);
        progression.State.LastPlayedAt = completedAt;
        return new ProgressionAward(playerXp, seasonXp, player.LevelsGained, season.SeasonTiersGained,
            player.CoinsEarned + season.CoinsEarned, player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray());
    }
}
