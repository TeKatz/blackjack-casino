using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class IndianRubyTests
{
    [Fact]
    public void PaylinesAreFiveReelsTenLeftToRightLines()
    {
        Assert.Equal(5, IndianRubyRules.Reels);
        Assert.Equal(3, IndianRubyRules.Rows);
        Assert.Equal(15, IndianRubyRules.Cells);
        Assert.Equal(10, IndianRubyRules.Lines.Count);
        Assert.All(IndianRubyRules.Lines, line =>
        {
            Assert.Equal(5, line.Count);
            for (int reel = 0; reel < line.Count; reel++)
                Assert.Equal(reel, line[reel] % IndianRubyRules.Reels);
        });
    }

    [Fact]
    public void RubyDoublesRegularPaylineButDoesNotTriggerElephantCount()
    {
        var board = Enumerable.Repeat(IndianRubySymbol.Empty, 15).ToArray();
        board[0] = IndianRubySymbol.Necklace;
        board[1] = IndianRubySymbol.Ruby;
        board[2] = IndianRubySymbol.Necklace;
        var (multiplier, lines) = IndianRubyRules.Evaluate(board);
        Assert.Equal(50m, multiplier);
        Assert.Contains(0, lines);
        Assert.Equal(0, board.Count(symbol => symbol == IndianRubySymbol.Elephant));
    }

    [Fact]
    public void TwoElephantsAnywhereTriggerExactlyOneRespinAndStayHeld()
    {
        // First two base draws are elephants; all remaining draws are non-elephants.
        int calls = 0;
        var game = new IndianRubySession(1000m, max =>
        {
            calls++;
            if (calls <= 2) return 0;
            return max - 1;
        });

        IndianRubySpinResult spin = game.Spin(10m);
        Assert.True(spin.ElephantBonus);
        Assert.Equal(2, spin.BaseGrid.Count(s => s == IndianRubySymbol.Elephant));
        Assert.Equal(2, spin.HeldCells.Count);
        Assert.All(spin.HeldCells, i => Assert.Equal(spin.BaseGrid[i], spin.FinalGrid[i]));
        Assert.All(spin.FinalGrid, symbol => Assert.Contains(symbol, new[]
        {
            IndianRubySymbol.Elephant, IndianRubySymbol.Ruby, IndianRubySymbol.Empty,
        }));
        Assert.Equal(10m, spin.Stake);
        Assert.Equal(spin.BankrollBefore - spin.Stake + spin.Payout, spin.BankrollAfter);
        Assert.Equal(spin.BankrollAfter, game.Bankroll);
    }

    [Fact]
    public void RespinRubyHelpsElephantLineOnlyInBonusEvaluation()
    {
        var grid = Enumerable.Repeat(IndianRubySymbol.Empty, 15).ToArray();
        grid[0] = IndianRubySymbol.Elephant;
        grid[1] = IndianRubySymbol.Ruby;
        grid[2] = IndianRubySymbol.Elephant;
        Assert.Equal(0m, IndianRubyRules.Evaluate(grid).Multiplier);
        // Two lines share the first elephant/ruby combination: x100 + x20.
        Assert.Equal(120m, IndianRubyRules.Evaluate(grid, elephantOnly:true).Multiplier);
    }

    [Fact]
    public void RejectsNonTableAndUnaffordableBetsWithoutDebitingWallet()
    {
        var game = new IndianRubySession(20m, _ => 0);
        Assert.Throws<InvalidOperationException>(() => game.Spin(7m));
        Assert.Throws<InvalidOperationException>(() => game.Spin(25m));
        Assert.Equal(20m, game.Bankroll);
        Assert.Throws<ArgumentOutOfRangeException>(() => game.SynchronizeBankroll(-1m));
    }

    [Theory]
    [InlineData("https://midnight-manor.local/indian-ruby.html",true)]
    [InlineData("https://midnight-manor.local/indian-ruby.html#bonus",false)]
    [InlineData("https://midnight-manor.local/indian-ruby.html?x=1",false)]
    [InlineData("http://midnight-manor.local/indian-ruby.html",false)]
    [InlineData("https://evil.example/indian-ruby.html",false)]
    [InlineData("https://midnight-manor.local/nine-lives.html",false)]
    public void ExactOfflineOriginRequired(string url,bool expected)
        => Assert.Equal(expected,ModernIndianRubyProtocol.Trusted(url));

    [Fact]
    public void RepeatedCommandReceiptDoesNotDebitTheWalletTwice()
    {
        var epoch = new ModernIndianRubySession();
        var machine = new IndianRubySession(1000m, _ => 799);
        var command = ModernIndianRubyProtocol.Parse(JsonSerializer.Serialize(new
        {
            version = 1, session = epoch.Id, expectedRevision = epoch.Revision,
            requestId = Guid.NewGuid().ToString("D"), action = "spin",
        }))!;
        Assert.NotNull(command);
        int paid = 0;
        (bool,string) Commit(ModernIndianRubyCommand request)
        {
            _ = machine.Spin(5m);
            paid++;
            return (true,"OK");
        }
        var first = epoch.Execute(command,Commit);
        decimal committedBalance = machine.Bankroll;
        Assert.True(first.Success);
        Assert.Equal(first, epoch.Execute(command, Commit));
        Assert.Equal(1, paid);
        Assert.Equal(committedBalance, machine.Bankroll);
        Assert.False(epoch.Execute(command with { RequestId = Guid.NewGuid().ToString("D") }, Commit).Success);
    }

    [Fact]
    public void SnapshotCarriesOnlyTheAuthoritativeOutcome()
    {
        var machine = new IndianRubySession(1000m, _ => 799);
        IndianRubySpinResult spin = machine.Spin(5m);
        var state = JsonSerializer.SerializeToElement(
            ModernIndianRubySnapshot.Create(machine,5m,spin,null,
                new PlayerProfileState(),false,false,"de"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Equal(spin.BankrollAfter.ToString(System.Globalization.CultureInfo.InvariantCulture),
            state.GetProperty("bankroll").GetString());
        Assert.Equal(15, state.GetProperty("lastSpin").GetProperty("baseGrid").GetArrayLength());
        Assert.Equal(10, state.GetProperty("paylines").GetArrayLength());
    }
}
