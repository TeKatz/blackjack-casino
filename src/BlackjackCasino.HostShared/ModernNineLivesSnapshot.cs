using System.Globalization;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

/// <summary>One read-only, native-authoritative presentation projection.</summary>
internal static class ModernNineLivesSnapshot
{
    private static string Money(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);

    internal static object Create(NineLivesSession machine, decimal bet, NineLivesSpinResult? last,
        ProgressionAward? award, PlayerProfileState profile, bool sounds, bool animations, string language)
    {
        NineLivesStatistics stats = profile.NineLives;
        return new
        {
            Language = language,
            Bankroll = Money(machine.Bankroll),
            Bet = Money(bet),
            CanSpin = machine.BonusActive || (machine.Bankroll >= bet && NineLivesRules.Bets.Contains(bet)),
            machine.BonusActive,
            machine.BonusSpinsRemaining,
            SoundEnabled = sounds,
            AnimationsEnabled = animations,
            Bets = NineLivesRules.Bets.Select(Money).ToArray(),
            Paylines = NineLivesRules.Lines.Select(line => line.ToArray()).ToArray(),
            Paytable = NineLivesRules.Symbols.Select(item => new
            {
                Id = item.Symbol.ToString(), item.Name, item.Weight,
                LineMultiplier = item.LineMultiplier,
            }).ToArray(),
            Profile = new { profile.Level, profile.Prestige, profile.Coins },
            Stats = new
            {
                stats.Spins, stats.Wins, stats.PaidSpins, stats.BonusSpins,
                stats.BonusSessions, stats.PounceWilds, stats.FullGridFrenzies,
                NetProfit = Money(stats.NetProfit),
                BiggestPayout = Money(stats.BiggestPayout),
                stats.BestPayoutMultiplier,
            },
            LastSpin = last is null ? null : new
            {
                Id = last.SpinId.ToString("D"),
                Bet = Money(last.Bet),
                Stake = Money(last.Stake),
                BankrollBefore = Money(last.BankrollBefore),
                BankrollAfter = Money(last.BankrollAfter),
                Grid = last.Grid.Select(symbol => symbol.ToString()).ToArray(),
                WinningLines = last.WinningLines.ToArray(),
                WinningCells = last.WinningCells.OrderBy(index => index).ToArray(),
                last.PounceWildCell, last.BonusCount, last.BonusSpinsAwarded,
                last.BonusSpinsRemaining, last.IsBonusSpin, last.FullGridFrenzy,
                Payout = Money(last.Payout),
                Net = Money(last.Net), last.TotalMultiplier,
                PlayerXp = award?.PlayerExperience ?? 0,
                SeasonXp = award?.SeasonExperience ?? 0,
                LevelUp = award?.HasLevelUp ?? false,
            },
        };
    }
}
