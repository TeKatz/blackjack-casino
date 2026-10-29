using System.Security.Cryptography;

namespace BlackjackCasino.Core;

public enum NineLivesSymbol
{
    Yarn,
    Fishbone,
    Bell,
    TabbyCat,
    WhiteCat,
    BlackCat,
    Wild,
    Bonus,
}

public sealed record NineLivesSymbolDefinition(
    NineLivesSymbol Symbol,
    string Name,
    int Weight,
    decimal LineMultiplier);

public static class NineLivesRules
{
    public const int Reels = 3;
    public const int Rows = 3;
    public const int Cells = Reels * Rows;
    public const decimal MinimumBet = 1m;
    public const decimal MaximumBet = 25_000m;
    public const decimal FullGridMultiplier = 5_000m;
    public const int BonusSpins = 9;

    private static readonly decimal[] BetSteps =
    [
        1m, 5m, 10m, 25m, 50m, 100m, 250m, 500m, 1_000m, 2_500m, 5_000m,
        10_000m, 25_000m,
    ];

    private static readonly NineLivesSymbolDefinition[] Definitions =
    [
        new(NineLivesSymbol.Yarn, "Yarn", 28, 0.75m),
        new(NineLivesSymbol.Fishbone, "Fishbone", 23, 1.25m),
        new(NineLivesSymbol.Bell, "Lucky Bell", 18, 2m),
        new(NineLivesSymbol.TabbyCat, "Tabby", 12, 4m),
        new(NineLivesSymbol.WhiteCat, "Moon Cat", 8, 7m),
        new(NineLivesSymbol.BlackCat, "Black Cat", 5, 12m),
        new(NineLivesSymbol.Wild, "Pounce Wild", 3, 25m),
        new(NineLivesSymbol.Bonus, "Nine Lives", 3, 0m),
    ];

    private static readonly int[][] Paylines =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 4, 8],
        [2, 4, 6],
    ];

    public static IReadOnlyList<decimal> Bets { get; } = Array.AsReadOnly(BetSteps);
    public static IReadOnlyList<NineLivesSymbolDefinition> Symbols { get; } = Array.AsReadOnly(Definitions);
    public static IReadOnlyList<IReadOnlyList<int>> Lines { get; } = Paylines.Select(Array.AsReadOnly).ToArray();
    public static int TotalWeight { get; } = Definitions.Sum(x => x.Weight);

    public static NineLivesSymbol SymbolForRoll(int roll)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(roll);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(roll, TotalWeight);
        int cursor = 0;
        foreach (NineLivesSymbolDefinition definition in Definitions)
        {
            cursor += definition.Weight;
            if (roll < cursor)
            {
                return definition.Symbol;
            }
        }
        throw new InvalidOperationException("Nine Lives symbol weights are inconsistent.");
    }

    public static NineLivesSymbol BonusSymbolForRoll(int roll)
    {
        // Bonus spins remain volatile but make Wilds visibly more threatening.
        ArgumentOutOfRangeException.ThrowIfNegative(roll);
        const int bonusWeight = 106;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(roll, bonusWeight);
        return roll switch
        {
            < 26 => NineLivesSymbol.Yarn,
            < 48 => NineLivesSymbol.Fishbone,
            < 65 => NineLivesSymbol.Bell,
            < 77 => NineLivesSymbol.TabbyCat,
            < 85 => NineLivesSymbol.WhiteCat,
            < 90 => NineLivesSymbol.BlackCat,
            < 100 => NineLivesSymbol.Wild,
            _ => NineLivesSymbol.Bonus,
        };
    }

    public static (decimal Multiplier, IReadOnlyList<int> WinningLines, IReadOnlySet<int> WinningCells) Evaluate(IReadOnlyList<NineLivesSymbol> grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.Count != Cells)
        {
            throw new ArgumentException($"Nine Lives requires exactly {Cells} symbols.", nameof(grid));
        }

        decimal multiplier = 0m;
        var lines = new List<int>();
        var cells = new HashSet<int>();
        for (int lineIndex = 0; lineIndex < Paylines.Length; lineIndex++)
        {
            int[] line = Paylines[lineIndex];
            NineLivesSymbol[] symbols = line.Select(index => grid[index]).ToArray();
            if (symbols.Any(symbol => symbol == NineLivesSymbol.Bonus))
            {
                continue;
            }

            NineLivesSymbol? paying = symbols.FirstOrDefault(symbol => symbol != NineLivesSymbol.Wild);
            bool allWild = symbols.All(symbol => symbol == NineLivesSymbol.Wild);
            if (!allWild && paying is null)
            {
                continue;
            }

            NineLivesSymbol target = allWild ? NineLivesSymbol.Wild : paying!.Value;
            if (!symbols.All(symbol => symbol == target || symbol == NineLivesSymbol.Wild))
            {
                continue;
            }

            NineLivesSymbolDefinition definition = Definitions.First(x => x.Symbol == target);
            multiplier += definition.LineMultiplier;
            lines.Add(lineIndex);
            foreach (int cell in line)
            {
                cells.Add(cell);
            }
        }

        return (multiplier, lines.AsReadOnly(), cells);
    }

    public static bool IsFullGridFrenzy(IReadOnlyList<NineLivesSymbol> grid)
    {
        if (grid.Count != Cells || grid.Any(symbol => symbol == NineLivesSymbol.Bonus))
        {
            return false;
        }
        NineLivesSymbol? target = grid.FirstOrDefault(symbol => symbol != NineLivesSymbol.Wild);
        if (grid.All(symbol => symbol == NineLivesSymbol.Wild))
        {
            return true;
        }
        return target.HasValue && grid.All(symbol => symbol == target.Value || symbol == NineLivesSymbol.Wild);
    }

    public static int Index(int row, int reel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        ArgumentOutOfRangeException.ThrowIfNegative(reel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(reel, Reels);
        return row * Reels + reel;
    }
}

public sealed record NineLivesSpinResult(
    Guid SpinId,
    DateTimeOffset CompletedAt,
    decimal Bet,
    decimal Stake,
    decimal BankrollBefore,
    decimal BankrollAfter,
    IReadOnlyList<NineLivesSymbol> Grid,
    IReadOnlyList<int> WinningLines,
    IReadOnlySet<int> WinningCells,
    int? PounceWildCell,
    int BonusCount,
    int BonusSpinsAwarded,
    int BonusSpinsRemaining,
    bool IsBonusSpin,
    bool FullGridFrenzy,
    decimal Payout)
{
    public decimal Net => Payout - Stake;
    public decimal TotalMultiplier => Bet == 0m ? 0m : Payout / Bet;
}

/// <summary>
/// Compact high-volatility 3x3 slot. Outcomes settle before presentation; Pounce Wild and
/// Nine Lives anticipation are visualized afterwards and never depend on animation timing.
/// </summary>
public sealed class NineLivesSession
{
    private readonly Func<int, int> nextInt;
    private decimal bonusBet;

    public NineLivesSession(decimal bankroll, Func<int, int>? nextInt = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bankroll);
        Bankroll = bankroll;
        this.nextInt = nextInt ?? RandomNumberGenerator.GetInt32;
    }

    public decimal Bankroll { get; private set; }
    public int BonusSpinsRemaining { get; private set; }
    public bool BonusActive => BonusSpinsRemaining > 0;

    public NineLivesSpinResult Spin(decimal requestedBet)
    {
        bool bonusSpin = BonusSpinsRemaining > 0;
        decimal bet = bonusSpin ? bonusBet : requestedBet;
        ValidateBet(bet);
        if (!bonusSpin && bet > Bankroll)
        {
            throw new InvalidOperationException("Not enough play money for this Nine Lives bet.");
        }

        decimal before = Bankroll;
        decimal stake = bonusSpin ? 0m : bet;
        if (bonusSpin)
        {
            BonusSpinsRemaining--;
        }

        var grid = new NineLivesSymbol[NineLivesRules.Cells];
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = bonusSpin
                ? NineLivesRules.BonusSymbolForRoll(Next(106))
                : NineLivesRules.SymbolForRoll(Next(NineLivesRules.TotalWeight));
        }

        int? pounceCell = null;
        if (grid.Contains(NineLivesSymbol.Wild) && Next(100) < (bonusSpin ? 65 : 40))
        {
            int[] candidates = Enumerable.Range(0, grid.Length)
                .Where(index => grid[index] is not NineLivesSymbol.Wild and not NineLivesSymbol.Bonus)
                .ToArray();
            if (candidates.Length > 0)
            {
                pounceCell = candidates[Next(candidates.Length)];
                grid[pounceCell.Value] = NineLivesSymbol.Wild;
            }
        }

        int bonusCount = grid.Count(symbol => symbol == NineLivesSymbol.Bonus);
        int awarded = bonusCount >= 3 ? NineLivesRules.BonusSpins : 0;
        if (awarded > 0)
        {
            if (!bonusSpin)
            {
                bonusBet = bet;
            }
            BonusSpinsRemaining += awarded;
        }

        (decimal lineMultiplier, IReadOnlyList<int> winningLines, IReadOnlySet<int> winningCells) = NineLivesRules.Evaluate(grid);
        bool fullGrid = NineLivesRules.IsFullGridFrenzy(grid);
        decimal multiplier = fullGrid ? Math.Max(NineLivesRules.FullGridMultiplier, lineMultiplier) : lineMultiplier;
        decimal payout = checked(bet * multiplier);
        Bankroll = checked(before - stake + payout);

        return new NineLivesSpinResult(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            bet,
            stake,
            before,
            Bankroll,
            Array.AsReadOnly(grid),
            winningLines,
            winningCells,
            pounceCell,
            bonusCount,
            awarded,
            BonusSpinsRemaining,
            bonusSpin,
            fullGrid,
            payout);
    }

    private static void ValidateBet(decimal bet)
    {
        if (bet < NineLivesRules.MinimumBet || bet > NineLivesRules.MaximumBet || !NineLivesRules.Bets.Contains(bet))
        {
            throw new ArgumentOutOfRangeException(nameof(bet), bet, "Nine Lives bet is not an available stake.");
        }
    }

    private int Next(int maxExclusive)
    {
        int value = nextInt(maxExclusive);
        if (value < 0 || value >= maxExclusive)
        {
            throw new InvalidOperationException("Random source returned an out-of-range value.");
        }
        return value;
    }
}
