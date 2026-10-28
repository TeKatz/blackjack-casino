using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class SlotMachineTests
{
    [Theory]
    [InlineData(SlotSymbol.Cherry, 5)]
    [InlineData(SlotSymbol.Lemon, 10)]
    [InlineData(SlotSymbol.Bell, 25)]
    [InlineData(SlotSymbol.Bar, 50)]
    [InlineData(SlotSymbol.Seven, 100)]
    public void TripleReturnsPublishedMultiplierIncludingStake(SlotSymbol symbol, int multiplier)
    {
        var machine = Machine(1_000, symbol, symbol, symbol);
        SlotSpinResult spin = machine.Spin(25);
        Assert.Equal(multiplier, spin.Multiplier);
        Assert.Equal(25m * multiplier, spin.Payout);
        Assert.Equal(1_000 - 25 + spin.Payout, machine.Bankroll);
        Assert.Equal(spin.BankrollAfter, machine.Bankroll);
        Assert.Equal(spin.Payout - 25, spin.Net);
    }

    [Theory]
    [InlineData(SlotSymbol.Cherry, SlotSymbol.Cherry, SlotSymbol.Seven)]
    [InlineData(SlotSymbol.Cherry, SlotSymbol.Seven, SlotSymbol.Cherry)]
    [InlineData(SlotSymbol.Seven, SlotSymbol.Cherry, SlotSymbol.Cherry)]
    public void ExactlyTwoCherriesReturnStakeInAnyOrder(SlotSymbol a, SlotSymbol b, SlotSymbol c)
    {
        var machine = Machine(25, a, b, c);
        SlotSpinResult result = machine.Spin(25);
        Assert.Equal(1, result.Multiplier);
        Assert.Equal(0, result.Net);
        Assert.Equal(25, machine.Bankroll);
    }

    [Fact]
    public void OtherPairsAndMixedSymbolsLoseOnlyTheStake()
    {
        var machine = Machine(25, SlotSymbol.Bar, SlotSymbol.Bar, SlotSymbol.Seven);
        SlotSpinResult spin = machine.Spin(25);
        Assert.Equal(0, spin.Payout);
        Assert.Equal(-25, spin.Net);
        Assert.Equal(0, machine.Bankroll);
        Assert.Throws<InvalidOperationException>(() => machine.Spin(1));
        Assert.Equal(0, machine.Bankroll);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(0.99)]
    [InlineData(1.001)]
    [InlineData(1000.01)]
    public void InvalidBetNeverChangesWalletOrDraws(double amount)
    {
        var machine = new SlotMachineSession(1_000, _ => throw new Exception("Must not draw"));
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Spin((decimal)amount));
        Assert.Equal(1_000, machine.Bankroll);
    }

    [Fact]
    public void RejectsInsufficientFundsAndFailedRandomSourceWithoutDebiting()
    {
        var shortWallet = new SlotMachineSession(2, _ => throw new Exception("Must not draw"));
        Assert.Throws<InvalidOperationException>(() => shortWallet.Spin(5));
        Assert.Equal(2, shortWallet.Bankroll);
        var brokenSource = new SlotMachineSession(100, _ => 32);
        Assert.Throws<ArgumentOutOfRangeException>(() => brokenSource.Spin(25));
        Assert.Equal(100, brokenSource.Bankroll);
        var throwingSource = new SlotMachineSession(100, _ => throw new InvalidOperationException("RNG failure"));
        Assert.Throws<InvalidOperationException>(() => throwingSource.Spin(25));
        Assert.Equal(100, throwingSource.Bankroll);
    }

    [Fact]
    public void RepeatedSpinsConserveFundsAndProduceUniqueResults()
    {
        var machine = Machine(1_000, SlotSymbol.Bell, SlotSymbol.Bell, SlotSymbol.Bell);
        SlotSpinResult a = machine.Spin(5);
        SlotSpinResult b = machine.Spin(25);
        Assert.NotEqual(a.SpinId, b.SpinId);
        Assert.Equal(a.BankrollAfter, b.BankrollBefore);
        Assert.Equal(1_000 + a.Net + b.Net, machine.Bankroll);
        Assert.Equal(1_000, a.BankrollBefore);
    }

    [Fact]
    public void ExhaustiveReelsMatchPublishedWeightsPayoutsAndReturn()
    {
        foreach (SlotSymbolDefinition definition in SlotMachineRules.Symbols)
        {
            Assert.Equal(definition.Stops, Enumerable.Range(0, 32).Count(i => SlotMachineRules.SymbolAt(i) == definition.Symbol));
        }
        long total = 0;
        for (int a = 0; a < 32; a++)
        for (int b = 0; b < 32; b++)
        for (int c = 0; c < 32; c++)
        {
            total += SlotMachineRules.PayoutMultiplier(SlotMachineRules.SymbolAt(a), SlotMachineRules.SymbolAt(b), SlotMachineRules.SymbolAt(c));
        }
        Assert.Equal(0.970458984375m, total / (32m * 32m * 32m));
        Assert.Equal(TablePresets.Get(TablePresetId.Casual).Rules.MinimumBet, SlotMachineRules.MinimumBet);
        Assert.Equal(TablePresets.Get(TablePresetId.Casual).Rules.MaximumBet, SlotMachineRules.MaximumBet);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(1000)]
    public void LossXpAndSeasonXpIgnoreBetSizeAndNeverPolluteBlackjackStats(int bet)
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        var slots = new SlotProgression(service);
        SlotSpinResult spin = Machine(1_000, SlotSymbol.Bar, SlotSymbol.Lemon, SlotSymbol.Seven).Spin(bet);
        ProgressionAward award = slots.Process(spin);
        Assert.Equal(20, award.PlayerExperience);
        Assert.Equal(15, award.SeasonExperience);
        Assert.Equal(20, profile.LifetimeExperience);
        Assert.Equal(15, service.GetOrCreateSeasonProgress(profile.ActiveSeasonId).Experience);
        Assert.Equal(1, profile.Slots.Spins);
        Assert.Equal(-bet, profile.Slots.NetProfit);
        Assert.Equal(0, profile.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(0, service.SessionStatistics.RoundsPlayed);
        Assert.Empty(profile.TableStatistics);
        Assert.Equal(ProgressionAward.None, slots.Process(spin));
        Assert.Equal(1, profile.Slots.Spins);
        Assert.Equal(20, profile.LifetimeExperience);
    }

    [Fact]
    public void WinUsesSharedLevelCoinAndSeasonRewardsAndPersistsSlotStats()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        service.GrantPlayerExperience(ProgressionCurve.ExperienceForNextLevel(0) - 1);
        var slots = new SlotProgression(service);
        SlotSpinResult spin = Machine(1_000, SlotSymbol.Seven, SlotSymbol.Seven, SlotSymbol.Seven).Spin(25);
        ProgressionAward award = slots.Process(spin);
        Assert.Equal(25, award.PlayerExperience);
        Assert.Equal(20, award.SeasonExperience);
        Assert.True(award.HasLevelUp);
        Assert.Equal(ProgressionCurve.CoinsForLevel(1), award.CoinsEarned);
        Assert.Equal(1, profile.Slots.Wins);
        Assert.Equal(2_500, profile.Slots.BiggestPayout);
        var restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;
        Assert.Equal(profile.Slots.NetProfit, restored.Slots.NetProfit);
        Assert.Equal(profile.Level, restored.Level);
        Assert.Equal(profile.Coins, restored.Coins);
        Assert.NotNull(JsonSerializer.Deserialize<PlayerProfileState>("{}")!.Slots);
    }

    [Fact]
    public void PushEarnsBaseXpWithoutWinBonusAndPrestigeKeepsSlots()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        ProgressionAward award = new SlotProgression(service).Process(Machine(25, SlotSymbol.Cherry, SlotSymbol.Cherry, SlotSymbol.Lemon).Spin(25));
        Assert.Equal(20, award.PlayerExperience);
        Assert.Equal(15, award.SeasonExperience);
        Assert.Equal(1, profile.Slots.Pushes);
        Assert.Equal(0, profile.Slots.Wins);
        service.GrantPlayerExperience(ProgressionCurve.TotalExperienceToLevel(100));
        Assert.True(service.Prestige().Success);
        Assert.Equal(1, profile.Slots.Spins);
    }

    [Fact]
    public void SharedWalletTransferPreservesBlackjackShoeAndHistory()
    {
        var shoe = new FixedShoe([
            new(Rank.Ace, Suit.Spades), new(Rank.Nine, Suit.Clubs),
            new(Rank.King, Suit.Hearts), new(Rank.Seven, Suit.Diamonds),
            .. Enumerable.Repeat(new Card(Rank.Ten, Suit.Clubs), 40),
        ]);
        var blackjack = new CasinoSession(TablePresets.Get(TablePresetId.Casual).Rules, 1_000, () => shoe);
        blackjack.StartRound(25);
        Assert.False(blackjack.RoundInProgress);
        RoundHistoryEntry completed = Assert.Single(blackjack.History);
        int remaining = blackjack.CardsRemaining;
        SlotSpinResult spin = Machine(blackjack.Bankroll, SlotSymbol.Bar, SlotSymbol.Lemon, SlotSymbol.Seven).Spin(25);
        blackjack.ResetBankroll(spin.BankrollAfter);
        Assert.Equal(spin.BankrollAfter, blackjack.Bankroll);
        Assert.Equal(remaining, blackjack.CardsRemaining);
        Assert.Equal(1, blackjack.ShoeNumber);
        Assert.Same(completed, Assert.Single(blackjack.History));
        blackjack.StartRound(25);
        Assert.Equal(remaining - 4, blackjack.CardsRemaining);
    }

    [Fact]
    public void ExistingBlackjackWalletRejectsTransferDuringAnActiveHand()
    {
        var shoe = new FixedShoe([
            new(Rank.Ten, Suit.Spades), new(Rank.Nine, Suit.Clubs),
            new(Rank.Seven, Suit.Hearts), new(Rank.Seven, Suit.Diamonds),
            .. Enumerable.Repeat(new Card(Rank.Ten, Suit.Clubs), 40),
        ]);
        var blackjack = new CasinoSession(TablePresets.Get(TablePresetId.Casual).Rules, 1_000, () => shoe);
        blackjack.StartRound(25);
        Assert.True(blackjack.RoundInProgress);
        Assert.Throws<InvalidOperationException>(() => blackjack.ResetBankroll(2_000));
        Assert.Equal(975, blackjack.Bankroll);
    }

    private sealed class FixedShoe(IEnumerable<Card> cards) : ICardShoe
    {
        private readonly Queue<Card> remaining = new(cards);
        public int Remaining => remaining.Count;
        public Card Draw() => remaining.Dequeue();
    }

    private static SlotMachineSession Machine(decimal bankroll, params SlotSymbol[] symbols)
    {
        int index = 0;
        return new SlotMachineSession(bankroll, _ =>
        {
            SlotSymbol symbol = symbols[index++ % symbols.Length];
            return Enumerable.Range(0, 32).First(stop => SlotMachineRules.SymbolAt(stop) == symbol);
        });
    }
}
