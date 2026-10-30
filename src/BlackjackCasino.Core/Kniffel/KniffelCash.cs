namespace BlackjackCasino.Core.Kniffel;

public sealed record KniffelCashChange(decimal Bankroll, KniffelTableState Table);

/// <summary>Caller commits bankroll and checkpoint together before adopting the returned state.</summary>
public static class KniffelCash
{
    public static KniffelCashChange Start(decimal bankroll, KniffelTableState? current, decimal stake,
        Action<KniffelCashChange> persist)
    {
        if (KniffelTable.IsActive(current)) throw new InvalidOperationException("Finish or forfeit the current table first.");
        if (bankroll < stake) throw new InvalidOperationException("Your bankroll does not cover this entry fee.");
        var change = new KniffelCashChange(checked(bankroll - stake), KniffelTable.Create(stake));
        persist(change);
        return change;
    }
    public static KniffelCashChange Apply(decimal bankroll, KniffelTableState current, KniffelTableState next,
        Action<KniffelCashChange> persist)
    {
        KniffelTable.Validate(current);
        KniffelTable.Validate(next);
        if (current.Player.GameId != next.Player.GameId) throw new InvalidOperationException("This result belongs to another table.");
        if (current.Settled) return new(bankroll, current);
        if (!current.Player.Entries.SequenceEqual(next.Player.Entries)
            || current.Player.RollsUsed != next.Player.RollsUsed
            || !current.Player.Dice.SequenceEqual(next.Player.Dice)
            || current.Opponents.Zip(next.Opponents).Any(pair => pair.First.GameId != pair.Second.GameId
                || pair.Second.Entries.Length < pair.First.Entries.Length || pair.Second.Entries.Length > pair.First.Entries.Length + 1
                || pair.First.Entries.Any(entry => !pair.Second.Entries.Contains(entry))))
            throw new InvalidOperationException("The result is stale.");
        var change = new KniffelCashChange(checked(bankroll + (next.Settled ? next.Payout : 0)), next);
        persist(change);
        return change;
    }
}
