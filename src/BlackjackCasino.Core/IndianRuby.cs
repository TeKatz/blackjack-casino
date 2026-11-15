using System.Security.Cryptography;

namespace BlackjackCasino.Core;

public enum IndianRubySymbol
{
    Elephant, Ruby, Necklace, Vase, Spices, Ace, King, Queen, Jack, Ten, Empty,
}

/// <summary>
/// First-playable Indian Ruby ruleset: five reels, ten left-to-right lines,
/// ruby substitution/2x and a single elephant hold-and-respin feature.
/// Reels/weights are original play-money tuning, not the manufacturer's undisclosed math.
/// </summary>
public static class IndianRubyRules
{
    public const int Reels = 5;
    public const int Rows = 3;
    public const int Cells = Reels * Rows;
    public static IReadOnlyList<decimal> Bets { get; } =
        Array.AsReadOnly(new[] { 1m, 5m, 10m, 25m, 50m, 100m, 250m, 500m, 1000m });

    private static readonly int[][] LineCells =
    [
        [0, 1, 2, 3, 4], [5, 6, 7, 8, 9], [10, 11, 12, 13, 14],
        [0, 6, 12, 8, 4], [10, 6, 2, 8, 14],
        [0, 1, 7, 3, 4], [10, 11, 7, 13, 14],
        [5, 1, 2, 3, 9], [5, 11, 12, 13, 9], [0, 6, 7, 8, 14],
    ];
    public static IReadOnlyList<IReadOnlyList<int>> Lines { get; } =
        LineCells.Select(line => (IReadOnlyList<int>)Array.AsReadOnly(line)).ToArray();

    // A transparent original probability model for the prototype (not Merkur RTP).
    private static readonly (IndianRubySymbol Symbol, int Weight)[] Symbols =
    [
        (IndianRubySymbol.Elephant, 30), (IndianRubySymbol.Ruby, 26),
        (IndianRubySymbol.Necklace, 54), (IndianRubySymbol.Vase, 68),
        (IndianRubySymbol.Spices, 80), (IndianRubySymbol.Ace, 106),
        (IndianRubySymbol.King, 115), (IndianRubySymbol.Queen, 125),
        (IndianRubySymbol.Jack, 135), (IndianRubySymbol.Ten, 145),
    ];

    // Public, immutable view of the prototype's actual symbol weights for the rules dialog.
    public static IReadOnlyList<(IndianRubySymbol Symbol, int Weight)> BaseSymbolWeights { get; }
        = Array.AsReadOnly(Symbols);

    public static int Index(int row, int reel) => row * Reels + reel;

    public static IndianRubySymbol RandomBase(int reel, Func<int, int> next)
    {
        if (reel is < 0 or >= Reels) throw new ArgumentOutOfRangeException(nameof(reel));
        int total = Symbols.Where(x => x.Symbol != IndianRubySymbol.Ruby || reel is >= 1 and <= 3)
            .Sum(x => x.Weight);
        int value = next(total);
        if (value < 0 || value >= total) throw new InvalidOperationException("Invalid RNG draw.");
        foreach (var (symbol, weight) in Symbols)
        {
            if (symbol == IndianRubySymbol.Ruby && reel is not (>= 1 and <= 3)) continue;
            if (value < weight) return symbol;
            value -= weight;
        }
        throw new InvalidOperationException("Indian Ruby symbol weights are inconsistent.");
    }

    // The special respin contains no ordinary paying symbols in its newly drawn cells.
    public static IndianRubySymbol RandomBonus(int reel, Func<int, int> next)
    {
        if (reel is < 0 or >= Reels) throw new ArgumentOutOfRangeException(nameof(reel));
        int roll = next(100);
        if (roll < 13) return IndianRubySymbol.Elephant;
        if (reel is >= 1 and <= 3 && roll < 22) return IndianRubySymbol.Ruby;
        return IndianRubySymbol.Empty;
    }

    public static decimal LineMultiplier(IndianRubySymbol symbol, int count) => symbol switch
    {
        IndianRubySymbol.Elephant => count switch { 2 => 10m, 3 => 50m, 4 => 200m, 5 => 1000m, _ => 0m },
        IndianRubySymbol.Necklace => count switch { 3 => 25m, 4 => 100m, 5 => 500m, _ => 0m },
        IndianRubySymbol.Spices or IndianRubySymbol.Vase =>
            count switch { 3 => 20m, 4 => 75m, 5 => 250m, _ => 0m },
        IndianRubySymbol.Ace or IndianRubySymbol.King =>
            count switch { 3 => 10m, 4 => 25m, 5 => 100m, _ => 0m },
        IndianRubySymbol.Queen or IndianRubySymbol.Jack or IndianRubySymbol.Ten =>
            count switch { 3 => 5m, 4 => 25m, 5 => 100m, _ => 0m },
        _ => 0m,
    };

    public static (decimal Multiplier, int[] Lines) Evaluate(IReadOnlyList<IndianRubySymbol> grid,
        bool elephantOnly = false)
    {
        if (grid.Count != Cells) throw new ArgumentException("A 5x3 grid is required.", nameof(grid));
        decimal total = 0m;
        var winners = new List<int>();
        for (int lineId = 0; lineId < LineCells.Length; lineId++)
        {
            decimal best = 0m;
            foreach (IndianRubySymbol target in Enum.GetValues<IndianRubySymbol>())
            {
                if (target is IndianRubySymbol.Empty or IndianRubySymbol.Ruby) continue;
                if (elephantOnly && target != IndianRubySymbol.Elephant) continue;
                int count = 0;
                bool usesRuby = false;
                foreach (int cell in LineCells[lineId])
                {
                    IndianRubySymbol actual = grid[cell];
                    if (actual == target) { count++; continue; }
                    // A ruby does not trigger an elephant bonus. During the feature it can
                    // help complete an elephant line; the held trigger is already settled.
                    if (actual == IndianRubySymbol.Ruby && (target != IndianRubySymbol.Elephant || elephantOnly))
                    {
                        count++; usesRuby = true; continue;
                    }
                    break;
                }
                decimal payout = LineMultiplier(target, count) * (usesRuby ? 2m : 1m);
                best = Math.Max(best, payout);
            }
            if (best <= 0m) continue;
            total += best;
            winners.Add(lineId);
        }
        return (total, winners.ToArray());
    }
}

public sealed record IndianRubySpinResult(
    Guid SpinId, DateTimeOffset CompletedAt, decimal Bet,
    decimal BankrollBefore, decimal BankrollAfter,
    IReadOnlyList<IndianRubySymbol> BaseGrid,
    IReadOnlyList<IndianRubySymbol> FinalGrid,
    IReadOnlyList<int> HeldCells,
    IReadOnlyList<int> WinningLines,
    bool ElephantBonus, decimal BasePayout, decimal BonusPayout)
{
    public decimal Stake => Bet;
    public decimal Payout => BasePayout + BonusPayout;
    public decimal Net => Payout - Bet;
}

/// <summary>One paid spin settles the base result and optional bonus atomically in memory,
/// before the browser is told the result. A browser animation never calls RNG.</summary>
public sealed class IndianRubySession
{
    private readonly Func<int, int> next;
    public decimal Bankroll { get; private set; }

    public IndianRubySession(decimal bankroll, Func<int, int>? nextInt = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bankroll);
        Bankroll = bankroll;
        next = nextInt ?? RandomNumberGenerator.GetInt32;
    }

    public void SynchronizeBankroll(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Bankroll = amount;
    }

    public IndianRubySpinResult Spin(decimal bet)
    {
        if (!IndianRubyRules.Bets.Contains(bet) || bet > Bankroll)
            throw new InvalidOperationException("Select an affordable Sunset Ruby wager.");
        var baseGrid = new IndianRubySymbol[IndianRubyRules.Cells];
        for (int row = 0; row < IndianRubyRules.Rows; row++)
            for (int reel = 0; reel < IndianRubyRules.Reels; reel++)
                baseGrid[IndianRubyRules.Index(row, reel)] = IndianRubyRules.RandomBase(reel, next);

        bool bonus = baseGrid.Count(s => s == IndianRubySymbol.Elephant) >= 2;
        var final = (IndianRubySymbol[])baseGrid.Clone();
        var held = new List<int>();
        if (bonus)
        {
            for (int index = 0; index < baseGrid.Length; index++)
            {
                if (baseGrid[index] is IndianRubySymbol.Elephant or IndianRubySymbol.Ruby)
                    held.Add(index);
                else
                    final[index] = IndianRubyRules.RandomBonus(index % IndianRubyRules.Reels, next);
            }
        }
        var (baseMultiplier, baseLines) = IndianRubyRules.Evaluate(baseGrid);
        var (bonusMultiplier, bonusLines) = bonus
            ? IndianRubyRules.Evaluate(final, elephantOnly: true)
            : (0m, Array.Empty<int>());
        decimal basePayout = decimal.Round((bet / 10m) * baseMultiplier, 2, MidpointRounding.AwayFromZero);
        decimal bonusPayout = decimal.Round((bet / 10m) * bonusMultiplier, 2, MidpointRounding.AwayFromZero);
        decimal before = Bankroll;
        decimal after = checked(before - bet + basePayout + bonusPayout);
        Bankroll = after;
        return new IndianRubySpinResult(Guid.NewGuid(), DateTimeOffset.UtcNow, bet,
            before, after, baseGrid, final, held, baseLines.Concat(bonusLines).Distinct().ToArray(),
            bonus, basePayout, bonusPayout);
    }
}
