using System.Security.Cryptography;

namespace BlackjackCasino.Core.Kniffel;

public enum KniffelCategory
{
    Ones, Twos, Threes, Fours, Fives, Sixes,
    ThreeOfAKind, FourOfAKind, FullHouse, SmallStraight, LargeStraight, Kniffel, Chance,
}

public sealed record KniffelEntry(KniffelCategory Category, int Points);

/// <summary>A detached, serializable checkpoint. Dice are zero only before a turn's first roll.</summary>
public sealed record KniffelState
{
    public int Version { get; init; } = 1;
    public Guid GameId { get; init; } = Guid.NewGuid();
    public int[] Dice { get; init; } = new int[5];
    public bool[] Held { get; init; } = new bool[5];
    public int RollsUsed { get; init; }
    public KniffelEntry[] Entries { get; init; } = [];
    public int KniffelsScored { get; init; }
}

public static class KniffelScoring
{
    public static IReadOnlyList<KniffelCategory> Categories { get; } = Array.AsReadOnly(Enum.GetValues<KniffelCategory>());

    public static int Evaluate(KniffelCategory category, IReadOnlyList<int> dice)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        ArgumentNullException.ThrowIfNull(dice);
        if (dice.Count != 5 || dice.Any(value => value is < 1 or > 6))
            throw new ArgumentException("Exactly five dice with values 1–6 are required.", nameof(dice));
        int[] counts = new int[7];
        foreach (int value in dice) counts[value]++;
        int sum = dice.Sum();
        if (category <= KniffelCategory.Sixes) return counts[(int)category + 1] * ((int)category + 1);
        int run = 0, longest = 0;
        for (int face = 1; face <= 6; face++)
        {
            run = counts[face] > 0 ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }
        return category switch
        {
            KniffelCategory.ThreeOfAKind => counts.Max() >= 3 ? sum : 0,
            KniffelCategory.FourOfAKind => counts.Max() >= 4 ? sum : 0,
            KniffelCategory.FullHouse => counts.Contains(3) && counts.Contains(2) ? 25 : 0,
            KniffelCategory.SmallStraight => longest >= 4 ? 30 : 0,
            KniffelCategory.LargeStraight => longest >= 5 ? 40 : 0,
            KniffelCategory.Kniffel => counts.Max() == 5 ? 50 : 0,
            KniffelCategory.Chance => sum,
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };
    }

    // Current German classic rules: another committed five-of-a-kind grants +50 and
    // allows the maximum in any free box. This is deliberately not Yahtzee's +100 joker.
    public static int Maximum(KniffelCategory category)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        if (category <= KniffelCategory.Sixes) return ((int)category + 1) * 5;
        return category switch
        {
            KniffelCategory.FullHouse => 25,
            KniffelCategory.SmallStraight => 30,
            KniffelCategory.LargeStraight => 40,
            KniffelCategory.Kniffel => 50,
            _ => 30,
        };
    }
}

/// <summary>Owns turn transitions and scoring; presentation never decides dice or points.</summary>
public sealed class KniffelGame
{
    private readonly Func<int> nextDie;
    private readonly int[] dice = new int[5];
    private readonly bool[] held = new bool[5];
    private readonly Dictionary<KniffelCategory, int> entries = [];
    public KniffelGame(Func<int>? nextDie = null)
    {
        this.nextDie = nextDie ?? (() => RandomNumberGenerator.GetInt32(1, 7));
        Dice = Array.AsReadOnly(dice);
        Held = Array.AsReadOnly(held);
    }

    public Guid GameId { get; private set; } = Guid.NewGuid();
    public IReadOnlyList<int> Dice { get; }
    public IReadOnlyList<bool> Held { get; }
    public int RollsUsed { get; private set; }
    public int KniffelsScored { get; private set; }
    public int FilledCount => entries.Count;
    public bool IsComplete => FilledCount == 13;
    public bool CanScore => !IsComplete && RollsUsed > 0;
    public bool CanRoll => !IsComplete && RollsUsed < 3 && (RollsUsed == 0 || held.Any(value => !value));
    public bool IsAdditionalKniffel => CanScore && KniffelsScored > 0 && dice.All(value => value == dice[0]);
    public int UpperSubtotal => entries.Where(entry => entry.Key <= KniffelCategory.Sixes).Sum(entry => entry.Value);
    public int UpperBonus => UpperSubtotal >= 63 ? 35 : 0;
    public int LowerSubtotal => entries.Where(entry => entry.Key > KniffelCategory.Sixes).Sum(entry => entry.Value);
    public int AdditionalKniffelBonus => Math.Max(0, KniffelsScored - 1) * 50;
    public int Total => UpperSubtotal + UpperBonus + LowerSubtotal + AdditionalKniffelBonus;
    public int? ScoreFor(KniffelCategory category) => entries.TryGetValue(category, out int score) ? score : null;

    public int Preview(KniffelCategory category)
    {
        if (!CanScore || entries.ContainsKey(category)) throw new InvalidOperationException("This box is not available.");
        return IsAdditionalKniffel ? KniffelScoring.Maximum(category) : KniffelScoring.Evaluate(category, dice);
    }

    public void Roll()
    {
        if (!CanRoll) throw new InvalidOperationException("No dice can be rolled now.");
        int[] result = (int[])dice.Clone();
        for (int i = 0; i < 5; i++)
        {
            if (held[i]) continue;
            int value = nextDie();
            if (value is < 1 or > 6) throw new InvalidOperationException("The dice source returned an invalid value.");
            result[i] = value;
        }
        result.CopyTo(dice, 0);
        RollsUsed++;
    }

    public void ToggleHold(int index)
    {
        if (index is < 0 or >= 5) throw new ArgumentOutOfRangeException(nameof(index));
        if (!CanScore || RollsUsed == 3) throw new InvalidOperationException("Dice can only be held between rolls.");
        held[index] = !held[index];
    }

    public int Commit(KniffelCategory category, bool scratch = false)
    {
        int points = Preview(category);
        if (scratch) points = 0;
        bool fiveOfAKind = dice.All(value => value == dice[0]);
        entries.Add(category, points);
        if (fiveOfAKind) KniffelsScored++;
        RollsUsed = 0;
        Array.Clear(dice);
        Array.Clear(held);
        return points;
    }

    public KniffelState Snapshot => new()
    {
        GameId = GameId, Dice = (int[])dice.Clone(), Held = (bool[])held.Clone(), RollsUsed = RollsUsed,
        Entries = entries.Select(entry => new KniffelEntry(entry.Key, entry.Value)).ToArray(),
        KniffelsScored = KniffelsScored,
    };

    public static KniffelGame Restore(KniffelState? state, Func<int>? nextDie = null)
    {
        var game = new KniffelGame(nextDie);
        if (state is null) return game;
        if (state.Version != 1 || state.Dice is not { Length: 5 } || state.Held is not { Length: 5 }
            || state.Entries is null || state.Entries.Length > 13 || state.RollsUsed is < 0 or > 3
            || state.KniffelsScored < 0 || state.KniffelsScored > state.Entries.Length
            || state.Entries.Any(entry => entry is null || !Enum.IsDefined(entry.Category) || entry.Points < 0
                || entry.Points > KniffelScoring.Maximum(entry.Category)
                || entry.Category <= KniffelCategory.Sixes && entry.Points % ((int)entry.Category + 1) != 0
                || entry.Category is KniffelCategory.FullHouse or KniffelCategory.SmallStraight or KniffelCategory.LargeStraight or KniffelCategory.Kniffel
                    && entry.Points != 0 && entry.Points != KniffelScoring.Maximum(entry.Category))
            || state.Entries.Select(entry => entry.Category).Distinct().Count() != state.Entries.Length
            || state.Dice.Any(value => state.RollsUsed == 0 ? value != 0 : value is < 1 or > 6)
            || state.RollsUsed == 0 && state.Held.Any(value => value)
            || state.Entries.Length == 13 && state.RollsUsed != 0)
            throw new ArgumentException("Invalid Kniffel checkpoint.", nameof(state));
        game.GameId = state.GameId == Guid.Empty ? Guid.NewGuid() : state.GameId;
        state.Dice.CopyTo(game.dice, 0);
        state.Held.CopyTo(game.held, 0);
        foreach (KniffelEntry entry in state.Entries) game.entries.Add(entry.Category, entry.Points);
        game.RollsUsed = state.RollsUsed;
        game.KniffelsScored = state.KniffelsScored;
        return game;
    }

    public static KniffelState Normalize(KniffelState? state)
    {
        try { return Restore(state).Snapshot; }
        catch (ArgumentException) { return new KniffelState(); }
    }
}
