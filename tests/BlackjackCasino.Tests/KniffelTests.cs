using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Desktop;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Tests;

public sealed class KniffelTests
{
    [Theory]
    [InlineData(KniffelCategory.FullHouse, 2, 2, 5, 5, 5, 25)]
    [InlineData(KniffelCategory.FullHouse, 5, 5, 5, 5, 5, 0)]
    [InlineData(KniffelCategory.FullHouse, 2, 2, 2, 2, 5, 0)]
    [InlineData(KniffelCategory.SmallStraight, 1, 2, 3, 4, 3, 30)]
    [InlineData(KniffelCategory.SmallStraight, 2, 3, 4, 5, 6, 30)]
    [InlineData(KniffelCategory.SmallStraight, 1, 2, 3, 5, 6, 0)]
    [InlineData(KniffelCategory.LargeStraight, 6, 2, 4, 3, 5, 40)]
    [InlineData(KniffelCategory.LargeStraight, 1, 2, 3, 4, 4, 0)]
    [InlineData(KniffelCategory.ThreeOfAKind, 4, 4, 4, 1, 6, 19)]
    [InlineData(KniffelCategory.FourOfAKind, 4, 4, 4, 4, 6, 22)]
    [InlineData(KniffelCategory.FourOfAKind, 4, 4, 4, 1, 6, 0)]
    [InlineData(KniffelCategory.Kniffel, 6, 6, 6, 6, 6, 50)]
    [InlineData(KniffelCategory.Chance, 1, 2, 3, 4, 6, 16)]
    [InlineData(KniffelCategory.Threes, 3, 3, 3, 1, 6, 9)]
    public void EvaluatesKnownHands(KniffelCategory category, int a, int b, int c, int d, int e, int expected)
        => Assert.Equal(expected, KniffelScoring.Evaluate(category, [a, b, c, d, e]));

    [Fact]
    public void ExhaustiveOrderedDiceMatchCombinatorialFrequencies()
    {
        var positive = KniffelScoring.Categories.ToDictionary(category => category, _ => 0);
        var totals = KniffelScoring.Categories.ToDictionary(category => category, _ => 0);
        for (int number = 0; number < 7776; number++)
        {
            int remaining = number;
            int[] dice = new int[5];
            for (int i = 0; i < 5; i++) { dice[i] = remaining % 6 + 1; remaining /= 6; }
            foreach (KniffelCategory category in KniffelScoring.Categories)
            {
                int score = KniffelScoring.Evaluate(category, dice);
                Assert.InRange(score, 0, KniffelScoring.Maximum(category));
                if (score > 0) positive[category]++;
                totals[category] += score;
                Assert.Equal(score, KniffelScoring.Evaluate(category, dice.Reverse().ToArray()));
            }
        }
        Assert.Equal(6, positive[KniffelCategory.Kniffel]);
        Assert.Equal(300, positive[KniffelCategory.FullHouse]);
        Assert.Equal(240, positive[KniffelCategory.LargeStraight]);
        Assert.Equal(1200, positive[KniffelCategory.SmallStraight]);
        Assert.Equal(156, positive[KniffelCategory.FourOfAKind]);
        Assert.Equal(1656, positive[KniffelCategory.ThreeOfAKind]);
        Assert.Equal(136080, totals[KniffelCategory.Chance]);
        for (int face = 1; face <= 6; face++) Assert.Equal(6480 * face, totals[(KniffelCategory)(face - 1)]);
    }

    [Fact]
    public void HoldsOnlySelectedDiceAndLimitsRolls()
    {
        int calls = 0;
        var game = new KniffelGame(() => calls++ % 6 + 1);
        Assert.False(game.CanScore);
        Assert.Throws<InvalidOperationException>(() => game.ToggleHold(0));
        Assert.Throws<InvalidOperationException>(() => game.Commit(KniffelCategory.Chance));
        game.Roll();
        game.ToggleHold(0);
        game.ToggleHold(3);
        int[] before = game.Dice.ToArray();
        game.Roll();
        Assert.Equal(8, calls);
        Assert.Equal(before[0], game.Dice[0]);
        Assert.Equal(before[3], game.Dice[3]);
        game.ToggleHold(0); // Releasing a previously held die is legal.
        game.Roll();
        Assert.Equal(12, calls);
        Assert.False(game.CanRoll);
        Assert.Throws<InvalidOperationException>(game.Roll);
        Assert.Throws<InvalidOperationException>(() => game.ToggleHold(0));
    }

    [Fact]
    public void AllHeldDoesNotConsumeRollAndCanBeReleased()
    {
        var game = new KniffelGame(() => 4);
        game.Roll();
        for (int i = 0; i < 5; i++) game.ToggleHold(i);
        Assert.False(game.CanRoll);
        Assert.Throws<InvalidOperationException>(game.Roll);
        Assert.Equal(1, game.RollsUsed);
        game.ToggleHold(2);
        Assert.True(game.CanRoll);
    }

    [Fact]
    public void CanCommitEarlyButNeverOverwriteOrPlayAfterThirteenEntries()
    {
        int next = 0;
        var game = new KniffelGame(() => next++ % 6 + 1);
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            game.Roll();
            int preview = game.Preview(category);
            Assert.Equal(preview, game.Commit(category));
            Assert.Equal(preview, game.ScoreFor(category));
            Assert.All(game.Dice, die => Assert.Equal(0, die));
            Assert.All(game.Held, held => Assert.False(held));
            Assert.Equal(0, game.RollsUsed);
            Assert.Throws<InvalidOperationException>(() => game.Commit(category));
        }
        Assert.True(game.IsComplete);
        Assert.False(game.CanRoll);
        Assert.False(game.CanScore);
        Assert.Throws<InvalidOperationException>(game.Roll);
    }

    [Fact]
    public void FilledBoxCannotBeSelectedDuringLaterTurn()
    {
        var game = new KniffelGame(() => 2);
        game.Roll(); game.Commit(KniffelCategory.Twos);
        game.Roll();
        Assert.Throws<InvalidOperationException>(() => game.Preview(KniffelCategory.Twos));
        Assert.Throws<InvalidOperationException>(() => game.Commit(KniffelCategory.Twos));
        Assert.Equal(1, game.FilledCount);
    }

    [Fact]
    public void ExplicitScratchWritesZeroEvenWhenCombinationScores()
    {
        var game = new KniffelGame(() => 2);
        game.Roll();
        Assert.Equal(50, game.Preview(KniffelCategory.Kniffel));
        Assert.Equal(0, game.Commit(KniffelCategory.Kniffel, scratch: true));
        Assert.Equal(0, game.ScoreFor(KniffelCategory.Kniffel));
    }

    [Theory]
    [InlineData(2, 62, 0)]
    [InlineData(3, 63, 35)]
    public void UpperBonusStartsExactlyAtSixtyThree(int ones, int subtotal, int bonus)
    {
        var game = KniffelGame.Restore(new KniffelState
        {
            Entries = [new(KniffelCategory.Ones, ones), new(KniffelCategory.Twos, 4), new(KniffelCategory.Threes, 6),
                new(KniffelCategory.Fives, 20), new(KniffelCategory.Sixes, 30)],
        });
        Assert.Equal(subtotal, game.UpperSubtotal);
        Assert.Equal(bonus, game.UpperBonus);
        Assert.Equal(subtotal + bonus, game.Total);
    }

    [Fact]
    public void AdditionalKniffelOffersAllFreeMaximumsAndAwardsBonusOnlyOnCommit()
    {
        var game = new KniffelGame(() => 2);
        game.Roll(); game.Commit(KniffelCategory.Kniffel);
        game.Roll();
        Assert.True(game.IsAdditionalKniffel);
        Assert.Equal(0, game.AdditionalKniffelBonus);
        Assert.Equal(40, game.Preview(KniffelCategory.LargeStraight));
        Assert.Equal(30, game.Preview(KniffelCategory.Sixes));
        Assert.Equal(25, game.Preview(KniffelCategory.FullHouse));
        game.Commit(KniffelCategory.LargeStraight);
        Assert.Equal(50, game.AdditionalKniffelBonus);
        Assert.Equal(140, game.Total);
        game.Roll(); game.Commit(KniffelCategory.FullHouse);
        Assert.Equal(100, game.AdditionalKniffelBonus);
        Assert.Equal(215, game.Total);
    }

    [Fact]
    public void CheckpointRoundTripPreservesTurnHoldsAndPreviewWithoutFreeRoll()
    {
        int next = 0;
        var game = new KniffelGame(() => next++ % 6 + 1);
        game.Roll(); game.Commit(KniffelCategory.Chance);
        game.Roll(); game.ToggleHold(1); game.Roll();
        KniffelState detached = game.Snapshot;
        var restored = KniffelGame.Restore(JsonSerializer.Deserialize<KniffelState>(JsonSerializer.Serialize(detached)));
        Assert.Equal(game.Dice, restored.Dice);
        Assert.Equal(game.Held, restored.Held);
        Assert.Equal(2, restored.RollsUsed);
        Assert.Equal(game.Total, restored.Total);
        Assert.Equal(game.Preview(KniffelCategory.Ones), restored.Preview(KniffelCategory.Ones));
        detached.Dice[0] = 6;
        detached.Held[0] = true;
        Assert.NotEqual(detached.Held[0], game.Held[0]);
    }

    [Fact]
    public void InvalidCheckpointsAreRejectedAndNormalizedSafely()
    {
        KniffelState[] invalid = [new() { Dice = [1, 2] }, new() { RollsUsed = 4 },
            new() { RollsUsed = 1 }, new() { Held = [true, false, false, false, false] },
            new() { KniffelsScored = 1 }, new() { Entries = [new(KniffelCategory.Kniffel, 51)] },
            new() { Entries = [new(KniffelCategory.Chance, 20), new(KniffelCategory.Chance, 10)] }];
        foreach (KniffelState state in invalid)
        {
            Assert.Throws<ArgumentException>(() => KniffelGame.Restore(state));
            Assert.Empty(KniffelGame.Normalize(state).Entries);
        }
    }

    [Fact]
    public void PlayerSavePreservesKniffelAndRepairsOnlyMalformedGame()
    {
        string directory = Path.Combine(Path.GetTempPath(), "kniffel-save-" + Guid.NewGuid());
        try
        {
            var game = new KniffelGame(() => 3);
            game.Roll(); game.ToggleHold(2);
            PlayerStateStore.SaveOrThrow(new AppPreferences { Bankroll = 1234m, Kniffel = game.Snapshot }, directory);
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            AppPreferences saved = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(Path.Combine(directory, "player-state.json")), options)!;
            Assert.Equal(1234m, saved.Bankroll);
            var resumed = KniffelGame.Restore(saved.Kniffel);
            Assert.Equal(1, resumed.RollsUsed);
            Assert.True(resumed.Held[2]);
            Assert.Equal(50, resumed.Preview(KniffelCategory.Kniffel));
            PlayerStateStore.SaveOrThrow(saved with { Kniffel = new KniffelState { RollsUsed = 5 } }, directory);
            saved = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(Path.Combine(directory, "player-state.json")), options)!;
            Assert.Equal(1234m, saved.Bankroll);
            Assert.Equal(0, saved.Kniffel.RollsUsed);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void InvalidRandomSourceDoesNotPartiallyChangeTurn()
    {
        int count = 0;
        var game = new KniffelGame(() => ++count == 3 ? 7 : 2);
        Assert.Throws<InvalidOperationException>(game.Roll);
        Assert.Equal(0, game.RollsUsed);
        Assert.All(game.Dice, die => Assert.Equal(0, die));
    }
}
