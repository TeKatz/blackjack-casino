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
        int outerTotal = IndianRubyRules.BaseSymbolWeights.Where(x => x.Symbol != IndianRubySymbol.Ruby)
            .Sum(x => x.Weight);
        int innerTotal = IndianRubyRules.BaseSymbolWeights.Sum(x => x.Weight);
        double elephantOuter = 30d / outerTotal;
        double elephantInner = 30d / innerTotal;
        double zero = Math.Pow(1d - elephantOuter, 6) * Math.Pow(1d - elephantInner, 9);
        double one = 6 * elephantOuter * Math.Pow(1d - elephantOuter, 5) * Math.Pow(1d - elephantInner, 9)
            + 9 * elephantInner * Math.Pow(1d - elephantInner, 8) * Math.Pow(1d - elephantOuter, 6);
        var paytable = IndianRubyRules.BaseSymbolWeights.Select(item => new
        {
            Symbol = item.Symbol.ToString(),
            Two = IndianRubyRules.LineMultiplier(item.Symbol, 2),
            Three = IndianRubyRules.LineMultiplier(item.Symbol, 3),
            Four = IndianRubyRules.LineMultiplier(item.Symbol, 4),
            Five = IndianRubyRules.LineMultiplier(item.Symbol, 5),
            OuterPercent = Math.Round(item.Symbol == IndianRubySymbol.Ruby ? 0d : item.Weight * 100d / outerTotal, 3),
            InnerPercent = Math.Round(item.Weight * 100d / innerTotal, 3),
        }).ToArray();
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
            Paytable = paytable,
            BonusTriggerPercent = Math.Round((1d - zero - one) * 100d, 3),
            BonusElephantPercent = 13d,
            BonusRubyInnerPercent = 9d,
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
