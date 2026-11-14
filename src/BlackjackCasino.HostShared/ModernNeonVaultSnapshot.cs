using System.Globalization;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

/// <summary>Every presented feature and monetary value is resolved by Core first.</summary>
internal static class ModernNeonVaultSnapshot
{
    private static string Money(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    internal static object Create(NeonVaultSession machine, decimal bet, NeonVaultSpinResult? last,
        ProgressionAward? award, PlayerProfileState profile, bool sound, bool animations, string language)
    {
        NeonVaultStatistics stats = profile.NeonVault;
        return new
        {
            Language = language,
            Bankroll = Money(machine.Bankroll),
            Bet = Money(bet),
            CanSpin = machine.BonusActive || (machine.Bankroll >= bet && NeonVaultRules.Bets.Contains(bet)),
            machine.BonusActive,
            machine.FreeSpinsRemaining,
            machine.VaultEnergy,
            EnergyTarget = NeonVaultRules.VaultEnergyTarget,
            BetTier = NeonVaultRules.BetTier(bet).ToString(),
            SoundEnabled = sound, AnimationsEnabled = animations,
            Bets = NeonVaultRules.Bets.Select(Money).ToArray(),
            Paytable = NeonVaultRules.Symbols.Select(definition => new
            {
                Id = definition.Symbol.ToString(), definition.Name, definition.Weight,
                ThreeReels = Money(definition.ThreeReels),
                FourReels = Money(definition.FourReels),
                FiveReels = Money(definition.FiveReels),
            }).ToArray(),
            Jackpots = Enum.GetValues<NeonVaultJackpot>()
                .Where(id => id != NeonVaultJackpot.None).Select(id => new
                {
                    Id = id.ToString(),
                    Multiplier = NeonVaultRules.JackpotMultiplier(id),
                    Amount = Money(bet * NeonVaultRules.JackpotMultiplier(id)),
                }).ToArray(),
            Profile = new { profile.Level, profile.Prestige, profile.Coins },
            Stats = new
            {
                stats.Spins, stats.Wins, stats.PaidSpins, stats.FreeSpins,
                stats.FreeSpinSessions, stats.FreeSpinsAwarded,
                stats.Jackpots, stats.GrandJackpots, stats.Blackouts,
                stats.TotalBlackouts, stats.SecurityOverrides,
                NetProfit = Money(stats.NetProfit),
                BiggestPayout = Money(stats.BiggestPayout),
                BestPayoutMultiplier = Money(stats.BestPayoutMultiplier),
            },
            LastSpin = last is null ? null : new
            {
                Id = last.SpinId.ToString("D"),
                Bet = Money(last.Bet), Stake = Money(last.Stake),
                BankrollBefore = Money(last.BankrollBefore),
                BankrollAfter = Money(last.BankrollAfter),
                BaseGrid = last.BaseGrid.Select(symbol => symbol.ToString()).ToArray(),
                FinalGrid = last.FinalGrid.Select(symbol => symbol.ToString()).ToArray(),
                Blackout = last.Blackout.ToString(),
                BlackoutWildCells = last.BlackoutWildCells.ToArray(),
                last.BlackoutFullReel,
                ExpandedReels = last.ExpandedReels.ToArray(),
                last.SecurityOverrideReel,
                NearMiss = last.NearMiss.ToString(),
                Jackpot = last.Jackpot.ToString(),
                WaysPayout = Money(last.WaysPayout),
                ScatterPayout = Money(last.ScatterPayout),
                JackpotPayout = Money(last.JackpotPayout),
                Payout = Money(last.Payout), Net = Money(last.Net),
                last.WaysWon, last.FreeSpinsAwarded, last.FreeSpinsRemaining,
                last.VaultEnergyAfter, last.IsFreeSpin,
                BetTier = last.BetTier.ToString(),
                TotalMultiplier = Money(last.TotalMultiplier),
                PlayerXp = award?.PlayerExperience ?? 0,
                SeasonXp = award?.SeasonExperience ?? 0,
                LevelUp = award?.HasLevelUp ?? false,
            },
        };
    }
}
