using BlackjackCasino.Core;
using BlackjackCasino.Core.Scratchcards;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class ScratchcardTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(),
        "casino-scratch-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(499, 0)]
    [InlineData(500, 10)]
    [InlineData(699, 10)]
    [InlineData(700, 20)]
    [InlineData(849, 20)]
    [InlineData(850, 50)]
    [InlineData(949, 50)]
    [InlineData(950, 100)]
    [InlineData(989, 100)]
    [InlineData(990, 250)]
    [InlineData(998, 250)]
    [InlineData(999, 1000)]
    public void PrizeBandsAreExplicit(int roll, int expected) =>
        Assert.Equal((decimal)expected, ScratchcardRules.PrizeForRoll(roll));

    [Fact]
    public void GeneratedTicketsShowExactlyOneTripleOnlyWhenWinning()
    {
        for (int roll = 0; roll < ScratchcardRules.DrawRange; roll++)
        {
            var ticket = ScratchcardRules.Draw(Guid.NewGuid(), 100m,
                ceiling => ceiling == ScratchcardRules.DrawRange ? roll : 0);
            Assert.True(ScratchcardRules.Valid(ticket));
            int triples = ticket.Symbols.GroupBy(s => s).Count(g => g.Count() == 3);
            Assert.Equal(ticket.Prize > 0m ? 1 : 0, triples);
            Assert.Equal(9, ticket.Symbols.Length);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScratchcardRules.PrizeForRoll(1000));
    }

    [Fact]
    public void PurchaseAndCollectPersistWithoutPayingTwice()
    {
        var ledger = new NativeScratchcardJournal(folder);
        Guid ticketId = Guid.NewGuid();
        var wallet = new AppPreferences { Bankroll = 100m };
        ScratchcardTicket bought = ledger.Purchase(ticketId, wallet);
        Assert.Equal(80m, PlayerStateStore.Load(folder).Bankroll);
        Assert.False(bought.Collected);
        Assert.Equal(bought, ledger.Purchase(ticketId, wallet));

        var reopened = new NativeScratchcardJournal(folder);
        Assert.Equal(bought.TicketId, reopened.Latest!.TicketId);
        ScratchcardTicket claimed = reopened.Collect(ticketId,
            new AppPreferences { Bankroll = 80m });
        Assert.True(claimed.Collected);
        Assert.Equal(80m + bought.Prize, PlayerStateStore.Load(folder).Bankroll);

        var reopenedAgain = new NativeScratchcardJournal(folder);
        var once = reopenedAgain.Collect(ticketId,
            new AppPreferences { Bankroll = 80m + bought.Prize });
        Assert.Equal(claimed.TicketId, once.TicketId);
        Assert.True(once.Collected);
        Assert.Equal(claimed.Prize, once.Prize);
        Assert.Equal(80m + bought.Prize, PlayerStateStore.Load(folder).Bankroll);
        Assert.True(reopenedAgain.Contains(ticketId));
        Assert.NotNull(reopenedAgain.Purchase(Guid.NewGuid(),
            new AppPreferences { Bankroll = 80m + bought.Prize }));
    }

    [Fact]
    public void PendingPurchaseAndPayoutRecoverTheirExactWallet()
    {
        var ledger = new NativeScratchcardJournal(folder);
        Guid id = Guid.NewGuid();
        Assert.Throws<IOException>(() => ledger.Purchase(id,
            new AppPreferences { Bankroll = 100m },
            _ => throw new IOException("Simulated interrupted purchase.")));
        var reopened = new NativeScratchcardJournal(folder);
        reopened.Recover();
        Assert.Equal(80m, PlayerStateStore.Load(folder).Bankroll);
        Assert.True(reopened.Contains(id));
        ScratchcardTicket ticket = reopened.Latest!;
        Assert.Throws<IOException>(() => reopened.Collect(id,
            new AppPreferences { Bankroll = 80m },
            _ => throw new IOException("Simulated interrupted payout.")));

        var payoutRecovery = new NativeScratchcardJournal(folder);
        payoutRecovery.Recover();
        Assert.True(payoutRecovery.Latest!.Collected);
        Assert.Equal(80m + ticket.Prize, PlayerStateStore.Load(folder).Bankroll);
        payoutRecovery.Recover();
        Assert.Equal(80m + ticket.Prize, PlayerStateStore.Load(folder).Bankroll);
    }

    [Fact]
    public void CannotBuyTwoUncollectedTicketsOrSpendMoreThanWallet()
    {
        var ledger = new NativeScratchcardJournal(folder);
        ledger.Purchase(Guid.NewGuid(), new AppPreferences { Bankroll = 20m });
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Purchase(Guid.NewGuid(), new AppPreferences { Bankroll = 0m }));
        ledger.Collect(ledger.Latest!.TicketId, new AppPreferences { Bankroll = 0m });
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Purchase(Guid.NewGuid(), new AppPreferences { Bankroll = 0m }));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(500, 50)]
    [InlineData(700, 100)]
    [InlineData(850, 250)]
    [InlineData(950, 500)]
    [InlineData(990, 1250)]
    [InlineData(999, 5000)]
    public void GoldRushPrizeBandsScaleWithStake(int roll, int expected)
    {
        Assert.Equal((decimal)expected,
            ScratchcardRules.PrizeForRoll(roll, ScratchcardRules.GoldRushId));
        var ticket = ScratchcardRules.Draw(Guid.NewGuid(), 900m,
            ceiling => ceiling == ScratchcardRules.DrawRange ? roll : 0,
            gameId: ScratchcardRules.GoldRushId);
        Assert.True(ScratchcardRules.Valid(ticket));
        Assert.Equal(ScratchcardRules.GoldRushId, ticket.GameId);
        Assert.Equal((decimal)expected, ticket.Prize);
    }

    [Fact]
    public void GoldRushPurchasesAndClaimsUseItsOwnStakeAndCannotDoublePay()
    {
        var ledger = new NativeScratchcardJournal(folder);
        Guid id = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Purchase(id, new AppPreferences { Bankroll = 99m },
                gameId: ScratchcardRules.GoldRushId));
        Assert.Equal(0, ledger.Purchased);
        var purchased = ledger.Purchase(id, new AppPreferences { Bankroll = 200m },
            gameId: ScratchcardRules.GoldRushId);
        Assert.Equal(100m, PlayerStateStore.Load(folder).Bankroll);
        Assert.Equal(ScratchcardRules.GoldRushId, purchased.GameId);
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Purchase(id, new AppPreferences { Bankroll = 100m },
                gameId: ScratchcardRules.LuckySevensId));
        var restored = new NativeScratchcardJournal(folder);
        Assert.Equal(ScratchcardRules.GoldRushId, restored.Latest!.GameId);
        var collected = restored.Collect(id, new AppPreferences { Bankroll = 100m });
        Assert.True(collected.Collected);
        Assert.Equal(100m + purchased.Prize, PlayerStateStore.Load(folder).Bankroll);
        restored.Collect(id, new AppPreferences { Bankroll = 100m + purchased.Prize });
        Assert.Equal(100m + purchased.Prize, PlayerStateStore.Load(folder).Bankroll);
    }

    [Theory]
    [InlineData(ScratchcardRules.PlatinumVaultId, 500, 25, 25000)]
    [InlineData(ScratchcardRules.DiamondRoyaleId, 2500, 125, 125000)]
    public void PremiumTierPrizeTableAndSymbolPatternStayAuthoritative(
        string gameId, int price, int multiplier, int jackpot)
    {
        ScratchcardGame game = ScratchcardRules.GetGame(gameId);
        Assert.Equal(price, game.Price);
        Assert.Equal(multiplier, game.PrizeMultiplier);
        Assert.Equal(jackpot, ScratchcardRules.PrizeForRoll(999, gameId));
        Assert.Equal(0m, ScratchcardRules.PrizeForRoll(499, gameId));
        Assert.Equal(10m * multiplier, ScratchcardRules.PrizeForRoll(500, gameId));
        Assert.Equal(250m * multiplier, ScratchcardRules.PrizeForRoll(990, gameId));
        Assert.True(ScratchcardRules.IsPrize(jackpot, gameId));
        Assert.False(ScratchcardRules.IsPrize(jackpot + 1, gameId));
        for (int roll = 0; roll < ScratchcardRules.DrawRange; roll++)
        {
            var ticket = ScratchcardRules.Draw(Guid.NewGuid(), 10_000m,
                ceiling => ceiling == ScratchcardRules.DrawRange ? roll : 0,
                gameId: gameId);
            Assert.Equal(gameId, ticket.GameId);
            Assert.Equal(ScratchcardRules.PrizeForRoll(roll, gameId), ticket.Prize);
            Assert.True(ScratchcardRules.Valid(ticket));
            Assert.Equal(ticket.Prize > 0m ? 1 : 0,
                ticket.Symbols.GroupBy(s => s).Count(group => group.Count() == 3));
        }
    }

    [Theory]
    [InlineData(ScratchcardRules.PlatinumVaultId, 500)]
    [InlineData(ScratchcardRules.DiamondRoyaleId, 2500)]
    public void PremiumTicketPurchaseRecoverAndClaimStayIdempotent(string gameId, int price)
    {
        string directory = Path.Combine(folder, gameId);
        var ledger = new NativeScratchcardJournal(directory);
        Guid receipt = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Purchase(receipt, new AppPreferences { Bankroll = price - 1 },
                gameId: gameId));
        Assert.Equal(0, ledger.Purchased);
        var bought = ledger.Purchase(receipt,
            new AppPreferences { Bankroll = price + 100m }, gameId: gameId);
        Assert.Equal(gameId, bought.GameId);
        Assert.Equal(100m, PlayerStateStore.Load(directory).Bankroll);
        Assert.Equal(bought, ledger.Purchase(receipt,
            new AppPreferences { Bankroll = 100m }, gameId: gameId));
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Purchase(receipt, new AppPreferences { Bankroll = 100m },
                gameId: ScratchcardRules.LuckySevensId));
        var reloaded = new NativeScratchcardJournal(directory);
        Assert.Equal(gameId, reloaded.Latest!.GameId);
        var claimed = reloaded.Collect(receipt, new AppPreferences { Bankroll = 100m });
        Assert.True(claimed.Collected);
        Assert.Equal(100m + bought.Prize, PlayerStateStore.Load(directory).Bankroll);
        reloaded.Collect(receipt, new AppPreferences { Bankroll = 100m + bought.Prize });
        Assert.Equal(100m + bought.Prize, PlayerStateStore.Load(directory).Bankroll);
    }

    [Fact]
    public void AllSeriesKeepOriginalTheoreticalReturn()
    {
        Assert.Equal(4, ScratchcardRules.Games.Count);
        var ids = new HashSet<string>();
        foreach (ScratchcardGame game in ScratchcardRules.Games)
        {
            Assert.True(ids.Add(game.Id));
            decimal weightedPrize = Enumerable.Range(0, ScratchcardRules.DrawRange)
                .Sum(roll => ScratchcardRules.PrizeForRoll(roll, game.Id));
            Assert.Equal(0.8625m, weightedPrize / ScratchcardRules.DrawRange / game.Price);
        }
        Assert.Null(ScratchcardRules.FindGame("invalid-tier"));
        Assert.Throws<ArgumentException>(() =>
            ScratchcardRules.GetGame("invalid-tier"));
    }

    [Fact]
    public void LegacyTicketWithoutGameIdRestoresAsLuckySevens()
    {
        var ticket = ScratchcardRules.Draw(Guid.NewGuid(), 80m, ceiling => 0);
        string json = System.Text.Json.JsonSerializer.Serialize(ticket);
        const string idProperty = ",\"GameId\":\"lucky-sevens\"";
        Assert.Contains(idProperty, json);
        string legacyJson = json.Replace(idProperty, string.Empty, StringComparison.Ordinal);
        ScratchcardTicket? restored =
            System.Text.Json.JsonSerializer.Deserialize<ScratchcardTicket>(legacyJson);
        Assert.NotNull(restored);
        Assert.Equal(ScratchcardRules.LuckySevensId, restored.GameId);
        Assert.True(ScratchcardRules.Valid(restored));
    }

    public void Dispose()
    {
        try { Directory.Delete(folder, recursive:true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
