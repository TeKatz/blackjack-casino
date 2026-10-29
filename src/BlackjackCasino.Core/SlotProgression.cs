namespace BlackjackCasino.Core;

public class SlotStatistics
{
    public long Spins { get; set; }
    public long Wins { get; set; }
    public long Pushes { get; set; }
    public decimal TotalWagered { get; set; }
    public decimal TotalReturned { get; set; }
    public decimal BiggestPayout { get; set; }
    public decimal NetProfit => TotalReturned - TotalWagered;
}

public sealed class NeonVaultStatistics : SlotStatistics
{
    public long PaidSpins { get; set; }
    public long FreeSpins { get; set; }
    public long FreeSpinSessions { get; set; }
    public long FreeSpinsAwarded { get; set; }
    public long Jackpots { get; set; }
    public long GrandJackpots { get; set; }
    public long Blackouts { get; set; }
    public long TotalBlackouts { get; set; }
    public long SecurityOverrides { get; set; }
    public decimal BestPayoutMultiplier { get; set; }
}

public sealed class NineLivesStatistics : SlotStatistics
{
    public long PaidSpins { get; set; }
    public long BonusSpins { get; set; }
    public long BonusSessions { get; set; }
    public long BonusSpinsAwarded { get; set; }
    public long PounceWilds { get; set; }
    public long FullGridFrenzies { get; set; }
    public decimal BestPayoutMultiplier { get; set; }
}

/// <summary>Shares level/season rewards without recording slots as blackjack hands.</summary>
public sealed class SlotProgression(ProgressionService progression)
{
    private readonly HashSet<Guid> processed = [];

    public ProgressionAward Process(SlotSpinResult spin)
    {
        ArgumentNullException.ThrowIfNull(spin);
        return ProcessCore(progression.State.Slots, spin.SpinId, spin.CompletedAt, spin.Bet, spin.Payout, spin.Net);
    }

    public ProgressionAward Process(NeonVaultSpinResult spin)
    {
        ArgumentNullException.ThrowIfNull(spin);
        NeonVaultStatistics stats = progression.State.NeonVault;
        ProgressionAward award = ProcessCore(stats, spin.SpinId, spin.CompletedAt, spin.Stake, spin.Payout, spin.Net);
        if (award == ProgressionAward.None)
        {
            return award;
        }

        stats.PaidSpins += spin.IsFreeSpin ? 0 : 1;
        stats.FreeSpins += spin.IsFreeSpin ? 1 : 0;
        stats.FreeSpinSessions += !spin.IsFreeSpin && spin.FreeSpinsAwarded > 0 ? 1 : 0;
        stats.FreeSpinsAwarded += spin.FreeSpinsAwarded;
        stats.Jackpots += spin.Jackpot != NeonVaultJackpot.None ? 1 : 0;
        stats.GrandJackpots += spin.Jackpot == NeonVaultJackpot.Grand ? 1 : 0;
        stats.Blackouts += spin.Blackout != NeonVaultBlackout.None ? 1 : 0;
        stats.TotalBlackouts += spin.Blackout == NeonVaultBlackout.Total ? 1 : 0;
        stats.SecurityOverrides += spin.SecurityOverrideReel.HasValue ? 1 : 0;
        stats.BestPayoutMultiplier = Math.Max(stats.BestPayoutMultiplier, spin.Payout / spin.Bet);
        return award;
    }

    public ProgressionAward Process(NineLivesSpinResult spin)
    {
        ArgumentNullException.ThrowIfNull(spin);
        NineLivesStatistics stats = progression.State.NineLives;
        ProgressionAward award = ProcessCore(stats, spin.SpinId, spin.CompletedAt, spin.Stake, spin.Payout, spin.Net);
        if (award == ProgressionAward.None)
        {
            return award;
        }

        stats.PaidSpins += spin.IsBonusSpin ? 0 : 1;
        stats.BonusSpins += spin.IsBonusSpin ? 1 : 0;
        stats.BonusSessions += !spin.IsBonusSpin && spin.BonusSpinsAwarded > 0 ? 1 : 0;
        stats.BonusSpinsAwarded += spin.BonusSpinsAwarded;
        stats.PounceWilds += spin.PounceWildCell.HasValue ? 1 : 0;
        stats.FullGridFrenzies += spin.FullGridFrenzy ? 1 : 0;
        stats.BestPayoutMultiplier = Math.Max(stats.BestPayoutMultiplier, spin.TotalMultiplier);
        return award;
    }

    private ProgressionAward ProcessCore(SlotStatistics stats, Guid spinId, DateTimeOffset completedAt, decimal wagered, decimal returned, decimal net)
    {
        if (!processed.Add(spinId))
        {
            return ProgressionAward.None;
        }

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
