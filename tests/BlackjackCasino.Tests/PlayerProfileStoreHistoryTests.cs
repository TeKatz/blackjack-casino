using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class PlayerProfileStoreHistoryTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OrderedSnapshotKeepsNewestThousandWithoutResorting()
    {
        RoundHistoryEntry[] source = Enumerable.Range(0, 1_005)
            .Select(i => Entry(Epoch.AddMinutes(i)))
            .ToArray();

        RoundHistoryEntry[] snapshot = PlayerProfileStore.CreateHistorySnapshot(source);

        Assert.Equal(1_000, snapshot.Length);
        Assert.Equal(source[5].RoundId, snapshot[0].RoundId);
        Assert.Equal(source[^1].RoundId, snapshot[^1].RoundId);
        Assert.True(snapshot.Zip(snapshot.Skip(1), (left, right) => left.CompletedAt <= right.CompletedAt).All(x => x));
    }

    [Fact]
    public void UnsortedSnapshotPreservesLegacyChronologicalOrdering()
    {
        RoundHistoryEntry latest = Entry(Epoch.AddMinutes(3));
        RoundHistoryEntry earliest = Entry(Epoch.AddMinutes(1));
        RoundHistoryEntry middle = Entry(Epoch.AddMinutes(2));

        RoundHistoryEntry[] snapshot = PlayerProfileStore.CreateHistorySnapshot([latest, earliest, middle]);

        Assert.Equal(new[] { earliest.RoundId, middle.RoundId, latest.RoundId }, snapshot.Select(x => x.RoundId));
    }

    [Fact]
    public void SnapshotDeepCopiesNestedHistoryCollections()
    {
        var dealerCards = new List<Card> { new(Rank.Ace, Suit.Spades) };
        var playerCards = new List<Card> { new(Rank.King, Suit.Hearts) };
        var actions = new List<PlayerActionRecord> { new(0, default) };
        var playerHands = new List<HistoricalHand>
        {
            new(25m, false, false, false, playerCards, default, 25m),
        };
        var aiCards = new List<Card> { new(Rank.Queen, Suit.Diamonds) };
        var aiHands = new List<HistoricalHand>
        {
            new(10m, false, false, false, aiCards, default, -10m),
        };
        var aiSeats = new List<HistoricalAiSeat>
        {
            new("Dealer Bot", aiHands),
        };
        var rules = new GameRules { DeckCount = 8, MinimumBet = 5m, MaximumBet = 500m };
        var source = new RoundHistoryEntry(
            Guid.NewGuid(),
            Epoch,
            Epoch.AddMinutes(1),
            1_000m,
            1_025m,
            25m,
            0m,
            0m,
            25m,
            rules,
            dealerCards,
            playerHands,
            actions,
            aiSeats);

        RoundHistoryEntry snapshot = Assert.Single(PlayerProfileStore.CreateHistorySnapshot([source]));

        dealerCards.Clear();
        playerCards.Clear();
        playerHands.Clear();
        actions.Clear();
        aiCards.Clear();
        aiHands.Clear();
        aiSeats.Clear();

        Assert.NotSame(rules, snapshot.Rules);
        Assert.Equal(8, snapshot.Rules.DeckCount);
        Assert.Single(snapshot.DealerCards);
        HistoricalHand playerHand = Assert.Single(snapshot.PlayerHands);
        Assert.Single(playerHand.Cards);
        Assert.Single(snapshot.Actions);
        HistoricalAiSeat aiSeat = Assert.Single(snapshot.AiSeats);
        HistoricalHand aiHand = Assert.Single(aiSeat.Hands);
        Assert.Single(aiHand.Cards);
    }

    private static RoundHistoryEntry Entry(DateTimeOffset completedAt) => new(
        Guid.NewGuid(),
        completedAt.AddSeconds(-30),
        completedAt,
        1_000m,
        1_000m,
        10m,
        0m,
        0m,
        0m,
        new GameRules(),
        [],
        [],
        [],
        []);
}
