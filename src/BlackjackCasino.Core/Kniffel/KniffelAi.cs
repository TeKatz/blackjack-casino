namespace BlackjackCasino.Core.Kniffel;

public enum KniffelAiStyle { Balanced, Collector, Adventurous }
public sealed record KniffelAiChoice(int HoldMask, bool ScoreNow, KniffelCategory Category);

/// <summary>Bounded expected-value search using only this seat's public dice and sheet.</summary>
public static class KniffelAi
{
    private static readonly double[] Costs = [2.5, 5, 7.5, 10, 12.5, 15, 17, 13, 21, 25, 31, 19, 22];
    public static KniffelAiChoice Choose(KniffelGame game, KniffelAiStyle style)
    {
        if (!game.CanScore) throw new InvalidOperationException("Roll before choosing an action.");
        KniffelCategory[] free = KniffelScoring.Categories.Where(c => !game.ScoreFor(c).HasValue).ToArray();
        var best = Best(game.Dice, game, free, style);
        if (game.RollsUsed == 3 || game.Dice.Distinct().Count() == 1)
            return new(31, true, best.Category);
        double bestExpectation = best.Utility;
        int bestMask = 31;
        // Common random samples make comparisons stable, without inspecting future rolls.
        const int samples = 96;
        int seed = game.Dice.Aggregate(1729 + game.FilledCount * 113 + (int)style * 7919, (hash, die) => unchecked(hash * 31 + die));
        for (int mask = 0; mask < 31; mask++)
        {
            var random = new Random(seed);
            double expected = 0;
            int[] trial = new int[5];
            for (int sample = 0; sample < samples; sample++)
            {
                for (int i = 0; i < 5; i++) trial[i] = (mask & (1 << i)) != 0 ? game.Dice[i] : random.Next(1, 7);
                expected += Best(trial, game, free, style).Utility;
            }
            expected /= samples;
            // On the first roll, retain a little option value for another attempt.
            if (game.RollsUsed == 1) expected += .55;
            if (expected > bestExpectation + .12) { bestExpectation = expected; bestMask = mask; }
        }
        return new(bestMask, bestMask == 31, best.Category);
    }

    private static (KniffelCategory Category, double Utility) Best(IReadOnlyList<int> dice, KniffelGame game,
        KniffelCategory[] free, KniffelAiStyle style)
    {
        bool extra = game.KniffelsScored > 0 && dice.All(die => die == dice[0]);
        KniffelCategory best = free[0];
        double maximum = double.NegativeInfinity;
        foreach (KniffelCategory category in free)
        {
            int score = extra ? KniffelScoring.Maximum(category) : KniffelScoring.Evaluate(category, dice);
            double utility = score - Costs[(int)category];
            if (category <= KniffelCategory.Sixes && game.UpperSubtotal < 63)
            {
                // Values upper-section progress and the attainable 35-point bonus.
                double weight = style == KniffelAiStyle.Collector ? .42 : .23;
                utility += Math.Min(score, 63 - game.UpperSubtotal) * weight;
                if (game.UpperSubtotal + score >= 63) utility += 12;
            }
            if (category == KniffelCategory.Kniffel && style == KniffelAiStyle.Adventurous && score > 0) utility += 7;
            if (extra) utility += 50;
            if (utility > maximum) { maximum = utility; best = category; }
        }
        return (best, maximum);
    }

    public static string PlayTurn(KniffelGame game, KniffelAiStyle style)
    {
        game.Roll();
        while (true)
        {
            KniffelAiChoice choice = Choose(game, style);
            if (choice.ScoreNow)
            {
                int score = game.Commit(choice.Category);
                return $"{choice.Category}: {score}";
            }
            for (int i = 0; i < 5; i++)
                if (game.Held[i] != ((choice.HoldMask & (1 << i)) != 0)) game.ToggleHold(i);
            game.Roll();
        }
    }
}
