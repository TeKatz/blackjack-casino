using System.Globalization;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

internal static class ModernIndianRubySnapshot
{
    private static string Money(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    internal static object Create(IndianRubySession machine, decimal bet, IndianRubySpinResult? last,
        ProgressionAward? award, PlayerProfileState profile, bool sound, bool animations, string language)
    {
        SlotStatistics stats = profile.IndianRuby;
        return new
        {
            Language = language,
            Bankroll = Money(machine.Bankroll),
            Bet = Money(bet),
            CanSpin = IndianRubyRules.Bets.Contains(bet) && bet <= machine.Bankroll,
            SoundEnabled = sound,
            AnimationsEnabled = animations,
            Bets = IndianRubyRules.Bets.Select(Money).ToArray(),
            Paylines = IndianRubyRules.Lines.Select(line => line.ToArray()).ToArray(),
            Profile = new { profile.Level, profile.Prestige, profile.Coins },
            Stats = new
            {
                stats.Spins, stats.Wins, NetProfit = Money(stats.NetProfit),
                BiggestPayout = Money(stats.BiggestPayout),
            },
            LastSpin = last is null ? null : new
            {
                Id = last.SpinId.ToString("D"),
                Bet = Money(last.Bet),
                BankrollBefore = Money(last.BankrollBefore),
                BankrollAfter = Money(last.BankrollAfter),
                BaseGrid = last.BaseGrid.Select(s => s.ToString()).ToArray(),
                FinalGrid = last.FinalGrid.Select(s => s.ToString()).ToArray(),
                HeldCells = last.HeldCells.ToArray(),
                WinningLines = last.WinningLines.ToArray(),
                last.ElephantBonus,
                BasePayout = Money(last.BasePayout),
                BonusPayout = Money(last.BonusPayout),
                Payout = Money(last.Payout),
                Net = Money(last.Net),
                PlayerXp = award?.PlayerExperience ?? 0L,
                SeasonXp = award?.SeasonExperience ?? 0L,
                LevelUp = award?.HasLevelUp ?? false,
            },
        };
    }
}
