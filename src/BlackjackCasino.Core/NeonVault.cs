using System.Security.Cryptography;

namespace BlackjackCasino.Core;

public enum NeonVaultSymbol
{
    Pulse,
    Prism,
    Chip,
    Crown,
    Key,
    Wild,
    Scatter,
    VaultKey,
}

public enum NeonVaultBlackout
{
    None,
    Blackout,
    Overload,
    Total,
}

public enum NeonVaultJackpot
{
    None,
    Mini,
    Minor,
    Major,
    Grand,
}

public enum NeonVaultNearMiss
{
    None,
    Scatter,
    Jackpot,
    Blackout,
}

public enum NeonVaultBetTier
{
    Standard,
    HighRoller,
    VaultClass,
    MaxExposure,
}

public sealed record NeonVaultSymbolDefinition(
    NeonVaultSymbol Symbol,
    string Name,
    int Weight,
    decimal ThreeReels,
    decimal FourReels,
    decimal FiveReels);

public static class NeonVaultRules
{
    public const int Reels = 5;
    public const int Rows = 4;
    public const int Cells = Reels * Rows;
    public const int VaultEnergyTarget = 12;
    public const decimal MinimumBet = 1m;
    public const decimal MaximumBet = 250_000m;

    private static readonly decimal[] BetSteps =
    [
        1m, 5m, 10m, 25m, 50m, 100m, 250m, 500m, 1_000m, 2_500m, 5_000m,
        10_000m, 25_000m, 50_000m, 100_000m, 250_000m,
    ];

    private static readonly NeonVaultSymbolDefinition[] Definitions =
    [
        new(NeonVaultSymbol.Pulse, "Pulse", 28, 0.15m, 0.40m, 1.20m),
        new(NeonVaultSymbol.Prism, "Prism", 22, 0.20m, 0.60m, 1.80m),
        new(NeonVaultSymbol.Chip, "High Roller", 18, 0.25m, 0.75m, 2.50m),
        new(NeonVaultSymbol.Crown, "Crown", 14, 0.40m, 1.20m, 4.00m),
        new(NeonVaultSymbol.Key, "Vault Key", 10, 0.60m, 2.00m, 7.00m),
        new(NeonVaultSymbol.Wild, "Wild", 4, 0m, 0m, 0m),
        new(NeonVaultSymbol.Scatter, "Vault", 3, 0m, 0m, 0m),
        new(NeonVaultSymbol.VaultKey, "Access Key", 1, 0m, 0m, 0m),
    ];

    private static readonly NeonVaultSymbol[] PayingSymbols =
    [
        NeonVaultSymbol.Pulse,
        NeonVaultSymbol.Prism,
        NeonVaultSymbol.Chip,
        NeonVaultSymbol.Crown,
        NeonVaultSymbol.Key,
    ];

    public static IReadOnlyList<decimal> Bets { get; } = Array.AsReadOnly(BetSteps);
    public static IReadOnlyList<NeonVaultSymbolDefinition> Symbols { get; } = Array.AsReadOnly(Definitions);
    public static int TotalWeight { get; } = Definitions.Sum(x => x.Weight);

    public static NeonVaultSymbol SymbolForRoll(int roll)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(roll);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(roll, TotalWeight);
        int cursor = 0;
        foreach (NeonVaultSymbolDefinition definition in Definitions)
        {
            cursor += definition.Weight;
            if (roll < cursor)
            {
                return definition.Symbol;
            }
        }
        throw new InvalidOperationException("Neon Vault symbol strip is inconsistent.");
    }

    public static int Index(int row, int reel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        ArgumentOutOfRangeException.ThrowIfNegative(reel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(reel, Reels);
        return row * Reels + reel;
    }

    public static decimal ScatterMultiplier(int count) => count switch
    {
        >= 5 => 50m,
        4 => 10m,
        3 => 2m,
        _ => 0m,
    };

    public static int FreeSpinsFor(int count) => count switch
    {
        >= 5 => 16,
        4 => 12,
        3 => 8,
        _ => 0,
    };

    public static decimal JackpotMultiplier(NeonVaultJackpot jackpot) => jackpot switch
    {
        NeonVaultJackpot.Mini => 25m,
        NeonVaultJackpot.Minor => 75m,
        NeonVaultJackpot.Major => 250m,
        NeonVaultJackpot.Grand => 1_000m,
        _ => 0m,
    };

    public static NeonVaultBetTier BetTier(decimal bet) => bet switch
    {
        >= 250_000m => NeonVaultBetTier.MaxExposure,
        >= 100_000m => NeonVaultBetTier.VaultClass,
        >= 10_000m => NeonVaultBetTier.HighRoller,
        _ => NeonVaultBetTier.Standard,
    };

    internal static (decimal Multiplier, int Ways) EvaluateWays(IReadOnlyList<NeonVaultSymbol> grid)
    {
        decimal total = 0m;
        int totalWays = 0;
        foreach (NeonVaultSymbol symbol in PayingSymbols)
        {
            int consecutive = 0;
            int ways = 1;
            for (int reel = 0; reel < Reels; reel++)
            {
                int matches = 0;
                for (int row = 0; row < Rows; row++)
                {
                    NeonVaultSymbol cell = grid[Index(row, reel)];
                    if (cell == symbol || cell == NeonVaultSymbol.Wild)
                    {
                        matches++;
                    }
                }
                if (matches == 0)
                {
                    break;
                }
                consecutive++;
                ways *= matches;
            }
            if (consecutive < 3)
            {
                continue;
            }

            NeonVaultSymbolDefinition definition = Definitions.Single(x => x.Symbol == symbol);
            decimal perWay = consecutive switch
            {
                3 => definition.ThreeReels,
                4 => definition.FourReels,
                _ => definition.FiveReels,
            };
            // The total wager is distributed across the 1,024 all-ways combinations.
            // This normalization keeps a four-row layout from multiplying returns by 1,024.
            total += ways * perWay * 0.019m;
            totalWays += ways;
        }
        return (total, totalWays);
    }

    internal static NeonVaultJackpot ResolveJackpot(IReadOnlyList<NeonVaultSymbol> grid)
    {
        int keys = grid.Count(x => x == NeonVaultSymbol.VaultKey);
        if (keys < 3)
        {
            return NeonVaultJackpot.None;
        }

        bool everyReel = Enumerable.Range(0, Reels)
            .All(reel => Enumerable.Range(0, Rows).Any(row => grid[Index(row, reel)] == NeonVaultSymbol.VaultKey));
        if (keys >= 5 && everyReel)
        {
            return NeonVaultJackpot.Grand;
        }
        return keys switch
        {
            >= 5 => NeonVaultJackpot.Major,
            4 => NeonVaultJackpot.Minor,
            _ => NeonVaultJackpot.Mini,
        };
    }
}

public sealed class NeonVaultSpinResult
{
    internal NeonVaultSpinResult(
        decimal bet,
        decimal stake,
        decimal bankrollBefore,
        IReadOnlyList<NeonVaultSymbol> baseGrid,
        IReadOnlyList<NeonVaultSymbol> finalGrid,
        NeonVaultBlackout blackout,
        IReadOnlyList<int> blackoutWildCells,
        int? blackoutFullReel,
        IReadOnlyList<int> expandedReels,
        int? securityOverrideReel,
        NeonVaultNearMiss nearMiss,
        NeonVaultJackpot jackpot,
        decimal waysPayout,
        decimal scatterPayout,
        decimal jackpotPayout,
        int waysWon,
        int freeSpinsAwarded,
        int freeSpinsRemaining,
        int vaultEnergyAfter,
        bool freeSpin)
    {
        SpinId = Guid.NewGuid();
        CompletedAt = DateTimeOffset.UtcNow;
        Bet = bet;
        Stake = stake;
        BankrollBefore = bankrollBefore;
        BaseGrid = baseGrid;
        FinalGrid = finalGrid;
        Blackout = blackout;
        BlackoutWildCells = blackoutWildCells;
        BlackoutFullReel = blackoutFullReel;
        ExpandedReels = expandedReels;
        SecurityOverrideReel = securityOverrideReel;
        NearMiss = nearMiss;
        Jackpot = jackpot;
        WaysPayout = waysPayout;
        ScatterPayout = scatterPayout;
        JackpotPayout = jackpotPayout;
        WaysWon = waysWon;
        FreeSpinsAwarded = freeSpinsAwarded;
        FreeSpinsRemaining = freeSpinsRemaining;
        VaultEnergyAfter = vaultEnergyAfter;
        IsFreeSpin = freeSpin;
        Payout = checked(waysPayout + scatterPayout + jackpotPayout);
        BankrollAfter = checked(bankrollBefore - stake + Payout);
    }

    public Guid SpinId { get; }
    public DateTimeOffset CompletedAt { get; }
    public decimal Bet { get; }
    public decimal Stake { get; }
    public decimal BankrollBefore { get; }
    public decimal BankrollAfter { get; }
    public IReadOnlyList<NeonVaultSymbol> BaseGrid { get; }
    public IReadOnlyList<NeonVaultSymbol> FinalGrid { get; }
    public NeonVaultBlackout Blackout { get; }
    public IReadOnlyList<int> BlackoutWildCells { get; }
    public int? BlackoutFullReel { get; }
    public IReadOnlyList<int> ExpandedReels { get; }
    public int? SecurityOverrideReel { get; }
    public NeonVaultNearMiss NearMiss { get; }
    public NeonVaultJackpot Jackpot { get; }
    public decimal WaysPayout { get; }
    public decimal ScatterPayout { get; }
    public decimal JackpotPayout { get; }
    public decimal Payout { get; }
    public decimal Net => Payout - Stake;
    public int WaysWon { get; }
    public int FreeSpinsAwarded { get; }
    public int FreeSpinsRemaining { get; }
    public int VaultEnergyAfter { get; }
    public bool IsFreeSpin { get; }
    public decimal TotalMultiplier => Bet == 0 ? 0 : Payout / Bet;
    public NeonVaultBetTier BetTier => NeonVaultRules.BetTier(Bet);
}

/// <summary>
/// Resolves Neon Vault outcomes before presentation starts. Animation may emphasize near misses
/// and feature transitions but never changes the already settled grid or bankroll result.
/// </summary>
public sealed class NeonVaultSession
{
    private readonly Func<int, int> nextInt;
    private decimal bonusBet;
    private bool securityOverrideArmed;

    public NeonVaultSession(decimal bankroll, Func<int, int>? nextInt = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bankroll);
        Bankroll = bankroll;
        this.nextInt = nextInt ?? RandomNumberGenerator.GetInt32;
    }

    public decimal Bankroll { get; private set; }
    public int FreeSpinsRemaining { get; private set; }
    public int VaultEnergy { get; private set; }
    public bool BonusActive => FreeSpinsRemaining > 0;

    public NeonVaultSpinResult Spin(decimal requestedBet)
    {
        bool freeSpin = FreeSpinsRemaining > 0;
        decimal bet = freeSpin ? bonusBet : requestedBet;
        ValidateBet(bet);
        if (!freeSpin && bet > Bankroll)
        {
            throw new InvalidOperationException("Not enough play money for this Neon Vault bet.");
        }

        decimal before = Bankroll;
        decimal stake = freeSpin ? 0m : bet;
        if (freeSpin)
        {
            FreeSpinsRemaining--;
        }

        var baseGrid = new NeonVaultSymbol[NeonVaultRules.Cells];
        for (int i = 0; i < baseGrid.Length; i++)
        {
            baseGrid[i] = NeonVaultRules.SymbolForRoll(Next(NeonVaultRules.TotalWeight));
        }

        int scatters = baseGrid.Count(x => x == NeonVaultSymbol.Scatter);
        int awarded = NeonVaultRules.FreeSpinsFor(scatters);
        if (awarded > 0)
        {
            if (!freeSpin)
            {
                bonusBet = bet;
                VaultEnergy = 0;
                securityOverrideArmed = false;
            }
            FreeSpinsRemaining += awarded;
        }

        int eventRoll = Next(10_000);
        NeonVaultBlackout blackout = ResolveBlackout(eventRoll, freeSpin);
        NeonVaultNearMiss nearMiss = ResolveNearMiss(baseGrid, blackout, eventRoll, freeSpin);

        var working = baseGrid.ToArray();
        int? securityOverrideReel = null;
        if (freeSpin && securityOverrideArmed)
        {
            securityOverrideArmed = false;
            securityOverrideReel = Next(NeonVaultRules.Reels);
            FillReelWithWilds(working, securityOverrideReel.Value);
        }

        var blackoutWildCells = new List<int>();
        int? blackoutFullReel = null;
        ApplyBlackout(working, blackout, blackoutWildCells, ref blackoutFullReel);

        var expanded = new List<int>();
        for (int reel = 1; reel <= 3; reel++)
        {
            bool hasWild = Enumerable.Range(0, NeonVaultRules.Rows)
                .Any(row => working[NeonVaultRules.Index(row, reel)] == NeonVaultSymbol.Wild);
            if (!hasWild)
            {
                continue;
            }
            if (freeSpin || Next(100) < 35)
            {
                FillReelWithWilds(working, reel);
                expanded.Add(reel);
            }
        }

        (decimal waysMultiplier, int waysWon) = NeonVaultRules.EvaluateWays(working);
        NeonVaultJackpot jackpot = NeonVaultRules.ResolveJackpot(baseGrid);
        decimal waysPayout = checked(bet * waysMultiplier);
        decimal scatterPayout = checked(bet * NeonVaultRules.ScatterMultiplier(scatters));
        decimal jackpotPayout = checked(bet * NeonVaultRules.JackpotMultiplier(jackpot));

        decimal payout = checked(waysPayout + scatterPayout + jackpotPayout);
        Bankroll = checked(before - stake + payout);

        if (freeSpin)
        {
            int wilds = working.Count(x => x == NeonVaultSymbol.Wild);
            VaultEnergy += wilds;
            if (VaultEnergy >= NeonVaultRules.VaultEnergyTarget && FreeSpinsRemaining > 0)
            {
                VaultEnergy -= NeonVaultRules.VaultEnergyTarget;
                securityOverrideArmed = true;
            }
        }
        else if (awarded == 0)
        {
            VaultEnergy = 0;
            securityOverrideArmed = false;
        }

        return new NeonVaultSpinResult(
            bet,
            stake,
            before,
            Array.AsReadOnly(baseGrid),
            Array.AsReadOnly(working),
            blackout,
            blackoutWildCells.AsReadOnly(),
            blackoutFullReel,
            expanded.AsReadOnly(),
            securityOverrideReel,
            nearMiss,
            jackpot,
            waysPayout,
            scatterPayout,
            jackpotPayout,
            waysWon,
            awarded,
            FreeSpinsRemaining,
            VaultEnergy,
            freeSpin);
    }

    private void ApplyBlackout(NeonVaultSymbol[] grid, NeonVaultBlackout blackout, List<int> changed, ref int? fullReel)
    {
        int targets = blackout switch
        {
            NeonVaultBlackout.Blackout => 3,
            NeonVaultBlackout.Overload => 6,
            NeonVaultBlackout.Total => 3,
            _ => 0,
        };
        if (blackout == NeonVaultBlackout.Total)
        {
            fullReel = Next(NeonVaultRules.Reels);
            for (int row = 0; row < NeonVaultRules.Rows; row++)
            {
                int index = NeonVaultRules.Index(row, fullReel.Value);
                if (CanBecomeWild(grid[index]))
                {
                    grid[index] = NeonVaultSymbol.Wild;
                    changed.Add(index);
                }
            }
        }

        var candidates = Enumerable.Range(0, grid.Length)
            .Where(index => CanBecomeWild(grid[index]) && grid[index] != NeonVaultSymbol.Wild)
            .ToList();
        while (targets > 0 && candidates.Count > 0)
        {
            int pick = Next(candidates.Count);
            int index = candidates[pick];
            candidates.RemoveAt(pick);
            grid[index] = NeonVaultSymbol.Wild;
            changed.Add(index);
            targets--;
        }
    }

    private static bool CanBecomeWild(NeonVaultSymbol symbol) => symbol is not NeonVaultSymbol.Scatter and not NeonVaultSymbol.VaultKey;

    private static void FillReelWithWilds(NeonVaultSymbol[] grid, int reel)
    {
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            int index = NeonVaultRules.Index(row, reel);
            if (CanBecomeWild(grid[index]))
            {
                grid[index] = NeonVaultSymbol.Wild;
            }
        }
    }

    private static NeonVaultBlackout ResolveBlackout(int roll, bool freeSpin)
    {
        if (freeSpin)
        {
            return roll switch
            {
                < 30 => NeonVaultBlackout.Total,
                < 180 => NeonVaultBlackout.Overload,
                < 980 => NeonVaultBlackout.Blackout,
                _ => NeonVaultBlackout.None,
            };
        }
        return roll switch
        {
            < 15 => NeonVaultBlackout.Total,
            < 95 => NeonVaultBlackout.Overload,
            < 495 => NeonVaultBlackout.Blackout,
            _ => NeonVaultBlackout.None,
        };
    }

    private static NeonVaultNearMiss ResolveNearMiss(
        IReadOnlyList<NeonVaultSymbol> grid,
        NeonVaultBlackout blackout,
        int eventRoll,
        bool freeSpin)
    {
        int firstFourScatters = CountOnReels(grid, NeonVaultSymbol.Scatter, 0, 4);
        int lastScatters = CountOnReels(grid, NeonVaultSymbol.Scatter, 4, 5);
        if (firstFourScatters == 2 && lastScatters == 0)
        {
            return NeonVaultNearMiss.Scatter;
        }

        int firstFourKeys = CountOnReels(grid, NeonVaultSymbol.VaultKey, 0, 4);
        int lastKeys = CountOnReels(grid, NeonVaultSymbol.VaultKey, 4, 5);
        if (firstFourKeys >= 2 && lastKeys == 0)
        {
            return NeonVaultNearMiss.Jackpot;
        }

        int blackoutLimit = freeSpin ? 980 : 495;
        if (blackout == NeonVaultBlackout.None && eventRoll >= blackoutLimit && eventRoll < blackoutLimit + 200)
        {
            return NeonVaultNearMiss.Blackout;
        }
        return NeonVaultNearMiss.None;
    }

    private static int CountOnReels(IReadOnlyList<NeonVaultSymbol> grid, NeonVaultSymbol symbol, int start, int end)
    {
        int count = 0;
        for (int reel = start; reel < end; reel++)
        {
            for (int row = 0; row < NeonVaultRules.Rows; row++)
            {
                if (grid[NeonVaultRules.Index(row, reel)] == symbol)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private int Next(int maximum)
    {
        int value = nextInt(maximum);
        if (value < 0 || value >= maximum)
        {
            throw new InvalidOperationException("Neon Vault random source returned an invalid value.");
        }
        return value;
    }

    private static void ValidateBet(decimal bet)
    {
        if (bet < NeonVaultRules.MinimumBet || bet > NeonVaultRules.MaximumBet || decimal.Round(bet, 2) != bet)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "Bet must be $1–$250,000 in whole cents.");
        }
    }
}
