using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class NeonVaultTests
{
    [Fact]
    public void Supports_Max_Vault_Bet()
    {
        var random = new SequenceRandom(NoMatchGrid().Concat([5_000]));
        var session = new NeonVaultSession(1_000_000m, random.Next);

        NeonVaultSpinResult result = session.Spin(NeonVaultRules.MaximumBet);

        Assert.Equal(250_000m, result.Stake);
        Assert.Equal(750_000m, result.BankrollAfter);
        Assert.Equal(NeonVaultBetTier.MaxExposure, result.BetTier);
    }

    [Fact]
    public void Three_Scatters_Start_Eight_Free_Spins()
    {
        int[] grid = NoMatchGrid();
        grid[0] = 96;
        grid[1] = 96;
        grid[2] = 96;
        var random = new SequenceRandom(grid.Concat([5_000]));
        var session = new NeonVaultSession(10_000m, random.Next);

        NeonVaultSpinResult result = session.Spin(100m);

        Assert.Equal(8, result.FreeSpinsAwarded);
        Assert.Equal(8, result.FreeSpinsRemaining);
        Assert.True(session.BonusActive);
    }

    [Fact]
    public void Grand_Requires_Access_Key_On_Every_Reel()
    {
        int[] grid = NoMatchGrid();
        for (int reel = 0; reel < NeonVaultRules.Reels; reel++)
        {
            grid[reel] = 99;
        }
        var random = new SequenceRandom(grid.Concat([5_000]));
        var session = new NeonVaultSession(1_000_000m, random.Next);

        NeonVaultSpinResult result = session.Spin(100m);

        Assert.Equal(NeonVaultJackpot.Grand, result.Jackpot);
        Assert.Equal(100_000m, result.JackpotPayout);
    }

    [Fact]
    public void Wilds_Expand_Guaranteed_During_Free_Spins()
    {
        int[] trigger = NoMatchGrid();
        trigger[0] = 96;
        trigger[1] = 96;
        trigger[2] = 96;

        int[] bonus = NoMatchGrid();
        bonus[1] = 92;
        var sequence = trigger.Concat([5_000]).Concat(bonus).Concat([5_000]);
        var random = new SequenceRandom(sequence);
        var session = new NeonVaultSession(10_000m, random.Next);
        _ = session.Spin(100m);

        NeonVaultSpinResult result = session.Spin(100m);

        Assert.True(result.IsFreeSpin);
        Assert.Equal(0m, result.Stake);
        Assert.Contains(1, result.ExpandedReels);
        for (int row = 0; row < NeonVaultRules.Rows; row++)
        {
            Assert.Equal(NeonVaultSymbol.Wild, result.FinalGrid[NeonVaultRules.Index(row, 1)]);
        }
    }

    [Fact]
    public void Scatter_Near_Miss_Only_Changes_Presentation_Metadata()
    {
        int[] grid = NoMatchGrid();
        grid[0] = 96;
        grid[1] = 96;
        var random = new SequenceRandom(grid.Concat([5_000]));
        var session = new NeonVaultSession(10_000m, random.Next);

        NeonVaultSpinResult result = session.Spin(100m);

        Assert.Equal(NeonVaultNearMiss.Scatter, result.NearMiss);
        Assert.Equal(0, result.FreeSpinsAwarded);
        Assert.Equal(result.BaseGrid, result.FinalGrid);
    }

    [Fact]
    public void Blackout_Transforms_Settled_Grid_Into_Wilds()
    {
        var sequence = NoMatchGrid().Concat([100, 0, 0, 0]);
        var random = new SequenceRandom(sequence);
        var session = new NeonVaultSession(10_000m, random.Next);

        NeonVaultSpinResult result = session.Spin(100m);

        Assert.Equal(NeonVaultBlackout.Blackout, result.Blackout);
        Assert.DoesNotContain(NeonVaultSymbol.Wild, result.BaseGrid);
        Assert.True(result.FinalGrid.Count(x => x == NeonVaultSymbol.Wild) >= 3);
        Assert.True(result.BlackoutWildCells.Count >= 3);
    }

    [Fact]
    public void Rejects_Bets_Above_High_Roller_Maximum()
    {
        var session = new NeonVaultSession(1_000_000m, _ => 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Spin(250_000.01m));
    }

    private static int[] NoMatchGrid() =>
    [
        0, 28, 50, 68, 82,
        0, 28, 50, 68, 82,
        0, 28, 50, 68, 82,
        0, 28, 50, 68, 82,
    ];

    private sealed class SequenceRandom(IEnumerable<int> values)
    {
        private readonly Queue<int> values = new(values);

        internal int Next(int maximum)
        {
            int value = values.Count > 0 ? values.Dequeue() : maximum - 1;
            if (value < 0 || value >= maximum)
            {
                throw new InvalidOperationException($"Test random value {value} is outside 0..{maximum - 1}.");
            }
            return value;
        }
    }
}
