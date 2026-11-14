using System.Globalization;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class ModernReelsSnapshot
{
    private static string Money(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);

    internal static object Create(SlotMachineSession machine, decimal bet, SlotSpinResult? last,
        ProgressionAward? reward, PlayerProfileState profile, bool sounds, bool animations, string language)
    {
        SlotStatistics stats = profile.Slots;
        return new
        {
            Language = language,
            Bankroll = Money(machine.Bankroll),
            Bet = Money(bet),
            CanSpin = machine.Bankroll >= bet && bet >= SlotMachineRules.MinimumBet,
            SoundEnabled = sounds,
            AnimationsEnabled = animations,
            ReelStrip = Enumerable.Range(0, SlotMachineRules.ReelStops)
                .Select(index => SlotMachineRules.SymbolAt(index).ToString()).ToArray(),
            Paytable = SlotMachineRules.Symbols.Select(definition => new
            {
                Id = definition.Symbol.ToString(),
                definition.Name,
                definition.Stops,
                definition.TripleMultiplier,
            }).ToArray(),
            Profile = new { profile.Level, profile.Prestige, profile.Coins },
            Stats = new
            {
                stats.Spins, stats.Wins, stats.Pushes,
                NetProfit = Money(stats.NetProfit),
                BiggestPayout = Money(stats.BiggestPayout),
            },
            LastSpin = last is null ? null : new
            {
                Id = last.SpinId.ToString("D"),
                Bet = Money(last.Bet),
                BankrollBefore = Money(last.BankrollBefore),
                BankrollAfter = Money(last.BankrollAfter),
                Payout = Money(last.Payout),
                Net = Money(last.Net),
                last.Multiplier,
                Stops = last.Stops.ToArray(),
                Symbols = last.Symbols.Select(symbol => symbol.ToString()).ToArray(),
                PlayerXp = reward?.PlayerExperience ?? 0,
                SeasonXp = reward?.SeasonExperience ?? 0,
                LevelUp = reward?.HasLevelUp ?? false,
            },
        };
    }
}
