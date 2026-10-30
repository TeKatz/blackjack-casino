namespace BlackjackCasino.Core.Kniffel;

public sealed record KniffelTableState
{
    public int Version { get; init; } = 1;
    public KniffelState Player { get; init; } = new();
    public KniffelState[] Opponents { get; init; } = [new(), new(), new()];
    public decimal Stake { get; init; }
    public bool Settled { get; init; }
    public bool Forfeited { get; init; }
    public decimal Payout { get; init; }
    public int Placement { get; init; }
    public string[] LastMoves { get; init; } = [];
}

/// <summary>Four independent score sheets; persisted together with the shared bankroll.</summary>
public static class KniffelTable
{
    public static IReadOnlyList<decimal> Stakes { get; } = Array.AsReadOnly<decimal>([100, 500, 2500, 10000, 50000, 100000]);
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(["Mara", "Felix", "June"]);
    public static bool IsActive(KniffelTableState? state) => state is not null && !state.Settled;
    public static KniffelTableState Create(decimal stake)
    {
        if (!Stakes.Contains(stake)) throw new ArgumentOutOfRangeException(nameof(stake));
        return new() { Stake = stake };
    }

    public static void Validate(KniffelTableState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Version != 1 || !Stakes.Contains(state.Stake) || state.Opponents is not { Length: 3 })
            throw new ArgumentException("Invalid casino table checkpoint.");
        KniffelGame player = KniffelGame.Restore(state.Player);
        KniffelGame[] opponents = state.Opponents.Select(s => KniffelGame.Restore(s)).ToArray();
        if (state.Player.GameId == Guid.Empty || state.Opponents.Any(s => s.GameId == Guid.Empty)
            || state.Opponents.Select(s => s.GameId).Append(player.GameId).Distinct().Count() != 4
            || opponents.Select(g => g.FilledCount).Distinct().Count() != 1
            || opponents.Any(g => g.RollsUsed != 0 || g.FilledCount > player.FilledCount || player.FilledCount - g.FilledCount > 1)
            || state.Forfeited && (!state.Settled || state.Payout != 0 || state.Placement != 0)
            || !state.Settled && (state.Payout != 0 || state.Placement != 0)
            || state.Settled && !state.Forfeited && (!player.IsComplete || opponents.Any(g => !g.IsComplete)
                || state.Placement != Rank(state) || state.Payout != Prize(state)))
            throw new ArgumentException("Inconsistent casino table checkpoint.");
    }

    public static KniffelTableState WithPlayer(KniffelTableState state, KniffelState player)
    {
        Validate(state);
        if (state.Settled || state.Player.GameId != player.GameId) throw new InvalidOperationException("This table is not active.");
        KniffelGame before = KniffelGame.Restore(state.Player), after = KniffelGame.Restore(player);
        if (after.FilledCount < before.FilledCount || after.FilledCount > before.FilledCount + 1
            || state.Player.Entries.Any(entry => !player.Entries.Contains(entry)))
            throw new InvalidOperationException("A recorded table score cannot be replaced.");
        KniffelTableState next = state with { Player = after.Snapshot };
        Validate(next);
        return next;
    }

    public static bool NeedsOpponents(KniffelTableState state) => !state.Settled && state.Opponents[0].Entries.Length < state.Player.Entries.Length;

    public static KniffelTableState Advance(KniffelTableState state)
    {
        Validate(state);
        if (!NeedsOpponents(state)) return state;
        var result = new KniffelState[3];
        var moves = new string[3];
        for (int i = 0; i < 3; i++)
        {
            // Separate deterministic seat/turn streams make recovery repeat the same AI turn.
            // The strategy sees neither the player's dice nor any future random values.
            byte[] id = state.Opponents[i].GameId.ToByteArray();
            int seed = BitConverter.ToInt32(id, 0) ^ BitConverter.ToInt32(id, 8) ^ state.Opponents[i].Entries.Length * 7919;
            var random = new Random(seed);
            KniffelGame game = KniffelGame.Restore(state.Opponents[i], () => random.Next(1, 7));
            moves[i] = Names[i] + ": " + KniffelAi.PlayTurn(game, (KniffelAiStyle)i);
            result[i] = game.Snapshot;
        }
        KniffelTableState next = state with { Opponents = result, LastMoves = moves };
        if (KniffelGame.Restore(next.Player).IsComplete)
            next = next with { Settled = true, Placement = Rank(next), Payout = Prize(next) };
        Validate(next);
        return next;
    }

    public static int Rank(KniffelTableState state)
    {
        int score = KniffelGame.Restore(state.Player).Total;
        return 1 + state.Opponents.Count(s => KniffelGame.Restore(s).Total > score);
    }
    public static decimal PrizeForPlace(decimal stake, int place) => place switch
    {
        1 => stake * 2.5m + 5000,
        2 => stake * 1.1m + 2000,
        3 => 500,
        4 => 250,
        _ => throw new ArgumentOutOfRangeException(nameof(place)),
    };
    public static decimal Prize(KniffelTableState state)
    {
        int score = KniffelGame.Restore(state.Player).Total, place = Rank(state);
        int tied = 1 + state.Opponents.Count(s => KniffelGame.Restore(s).Total == score);
        decimal prize = Enumerable.Range(place, tied).Sum(p => PrizeForPlace(state.Stake, p)) / tied;
        return decimal.Floor(prize * 100) / 100;
    }
    public static KniffelTableState Forfeit(KniffelTableState state)
    {
        Validate(state);
        if (state.Settled) throw new InvalidOperationException("This table has already settled.");
        return state with { Settled = true, Forfeited = true, Payout = 0, Placement = 0 };
    }
}
