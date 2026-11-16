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

    public void Dispose()
    {
        try { Directory.Delete(folder, recursive:true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
