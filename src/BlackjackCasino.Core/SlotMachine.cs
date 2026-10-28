using System.Security.Cryptography;

namespace BlackjackCasino.Core;

public enum SlotSymbol
{
    Cherry,
    Lemon,
    Bell,
    Bar,
    Seven,
}

public sealed record SlotSymbolDefinition(SlotSymbol Symbol, string Name, int Stops, int TripleMultiplier);

/// <summary>One centre payline. Multipliers include the returned stake.</summary>
public static class SlotMachineRules
{
    private static readonly SlotSymbolDefinition[] Definitions =
    [
        new(SlotSymbol.Cherry, "Cherries", 12, 5),
        new(SlotSymbol.Lemon, "Lemon", 8, 10),
        new(SlotSymbol.Bell, "Bell", 6, 25),
        new(SlotSymbol.Bar, "BAR", 4, 50),
        new(SlotSymbol.Seven, "Seven", 2, 100),
    ];

    public static IReadOnlyList<SlotSymbolDefinition> Symbols { get; } = Array.AsReadOnly(Definitions);
    public const int ReelStops = 32;
    public static decimal MinimumBet => TablePresets.Get(TablePresetId.Casual).Rules.MinimumBet;
    public static decimal MaximumBet => TablePresets.Get(TablePresetId.Casual).Rules.MaximumBet;

    // Identical independent weighted strips: 97.0458984375% theoretical return.
    // Interleaving symbols makes the actual scrolling strip match the sampled stops.
    private static readonly SlotSymbol[] Strip = Enumerable.Range(0, 12)
        .SelectMany(i => Definitions.Where(x => i < x.Stops).Select(x => x.Symbol))
        .ToArray();

    public static SlotSymbol SymbolAt(int stop)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stop);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(stop, ReelStops);
        return Strip[stop];
    }

    public static int PayoutMultiplier(SlotSymbol first, SlotSymbol second, SlotSymbol third)
    {
        if (!Enum.IsDefined(first) || !Enum.IsDefined(second) || !Enum.IsDefined(third))
        {
            throw new ArgumentOutOfRangeException(nameof(first), "Unknown slot symbol.");
        }

        if (first == second && second == third)
        {
            return Definitions.Single(x => x.Symbol == first).TripleMultiplier;
        }

        int cherries = (first == SlotSymbol.Cherry ? 1 : 0)
            + (second == SlotSymbol.Cherry ? 1 : 0) + (third == SlotSymbol.Cherry ? 1 : 0);
        return cherries == 2 ? 1 : 0;
    }
}

public sealed class SlotSpinResult
{
    internal SlotSpinResult(decimal bet, decimal before, int first, int second, int third)
    {
        SpinId = Guid.NewGuid();
        CompletedAt = DateTimeOffset.UtcNow;
        Bet = bet;
        BankrollBefore = before;
        Stops = Array.AsReadOnly(new[] { first, second, third });
        Symbols = Array.AsReadOnly(Stops.Select(SlotMachineRules.SymbolAt).ToArray());
        Multiplier = SlotMachineRules.PayoutMultiplier(Symbols[0], Symbols[1], Symbols[2]);
        Payout = checked(bet * Multiplier);
        BankrollAfter = checked(before - bet + Payout);
    }

    public Guid SpinId { get; }
    public DateTimeOffset CompletedAt { get; }
    public decimal Bet { get; }
    public decimal BankrollBefore { get; }
    public decimal BankrollAfter { get; }
    public IReadOnlyList<int> Stops { get; }
    public IReadOnlyList<SlotSymbol> Symbols { get; }
    public int Multiplier { get; }
    public decimal Payout { get; }
    public decimal Net => Payout - Bet;
}

/// <summary>Separate from the blackjack session, shoe, rule engine and settlement.</summary>
public sealed class SlotMachineSession
{
    private readonly Func<int, int> nextStop;

    public SlotMachineSession(decimal bankroll, Func<int, int>? nextStop = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bankroll);
        Bankroll = bankroll;
        this.nextStop = nextStop ?? RandomNumberGenerator.GetInt32;
    }

    public decimal Bankroll { get; private set; }

    public SlotSpinResult Spin(decimal bet)
    {
        if (bet < SlotMachineRules.MinimumBet || bet > SlotMachineRules.MaximumBet || decimal.Round(bet, 2) != bet)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "Bet must be $1–$1,000 in whole cents.");
        }
        if (bet > Bankroll)
        {
            throw new InvalidOperationException("Not enough play money for this bet.");
        }

        // Resolve everything before mutating the balance. Animation cannot settle twice.
        int[] stops = Enumerable.Range(0, 3).Select(_ => nextStop(SlotMachineRules.ReelStops)).ToArray();
        foreach (int stop in stops)
        {
            _ = SlotMachineRules.SymbolAt(stop);
        }
        var result = new SlotSpinResult(bet, Bankroll, stops[0], stops[1], stops[2]);
        Bankroll = result.BankrollAfter;
        return result;
    }
}
