using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class NineLivesTests
{
    [Fact]
    public void Three_Bonus_Symbols_Award_Nine_Spins()
    {
        var random = new SequenceRandom([97, 97, 97, 0, 28, 51, 69, 81, 89]);
        var session = new NineLivesSession(10_000m, random.Next);

        NineLivesSpinResult result = session.Spin(10m);

        Assert.Equal(9, result.BonusSpinsAwarded);
        Assert.Equal(9, result.BonusSpinsRemaining);
        Assert.True(session.BonusActive);
    }

    [Fact]
    public void Bonus_Spins_Are_Free_And_Keep_Trigger_Bet()
    {
        var random = new SequenceRandom(
        [
            97, 97, 97, 0, 28, 51, 69, 81, 89,
            0, 26, 48, 65, 77, 85, 25, 47, 64,
        ]);
        var session = new NineLivesSession(10_000m, random.Next);
        _ = session.Spin(25m);
        decimal before = session.Bankroll;

        NineLivesSpinResult bonus = session.Spin(1m);

        Assert.True(bonus.IsBonusSpin);
        Assert.Equal(25m, bonus.Bet);
        Assert.Equal(0m, bonus.Stake);
        Assert.True(bonus.BankrollAfter >= before);
        Assert.Equal(8, bonus.BonusSpinsRemaining);
    }

    [Fact]
    public void Pounce_Wild_Strikes_A_Second_Cell()
    {
        var random = new SequenceRandom([94, 28, 51, 69, 81, 89, 0, 28, 51, 0, 0]);
        var session = new NineLivesSession(10_000m, random.Next);

        NineLivesSpinResult result = session.Spin(10m);

        Assert.Equal(1, result.PounceWildCell);
        Assert.Equal(NineLivesSymbol.Wild, result.Grid[0]);
        Assert.Equal(NineLivesSymbol.Wild, result.Grid[1]);
    }

    [Fact]
    public void Full_Grid_Frenzy_Pays_Five_Thousand_X()
    {
        var random = new SequenceRandom(Enumerable.Repeat(89, NineLivesRules.Cells));
        var session = new NineLivesSession(100_000m, random.Next);

        NineLivesSpinResult result = session.Spin(10m);

        Assert.True(result.FullGridFrenzy);
        Assert.Equal(50_000m, result.Payout);
        Assert.Equal(5_000m, result.TotalMultiplier);
    }

    [Fact]
    public void Five_Paylines_Include_Rows_And_Diagonals()
    {
        Assert.Equal(5, NineLivesRules.Lines.Count);
        Assert.Equal([0, 1, 2], NineLivesRules.Lines[0]);
        Assert.Equal([0, 4, 8], NineLivesRules.Lines[3]);
        Assert.Equal([2, 4, 6], NineLivesRules.Lines[4]);
    }

    [Fact]
    public void Rejects_Non_Paytable_Bets()
    {
        var session = new NineLivesSession(100_000m, _ => 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.Spin(7m));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Spin(25_001m));
    }

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
