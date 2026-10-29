using System.Globalization;

namespace BlackjackCasino.Desktop;

internal readonly record struct TableChipGroup(decimal Denomination, decimal Count)
{
    // Large wagers use a quantity marker rather than an unbounded visual tree.
    internal int VisibleCount => (int)Math.Min(Count, 6m);
    internal string Face => Denomination switch
    {
        1000m => "1K",
        >= 1m => Denomination.ToString("0", CultureInfo.InvariantCulture),
        >= .01m => (Denomination * 100m).ToString("0", CultureInfo.InvariantCulture) + "¢",
        .005m => "½¢",
        _ => "•",
    };
}

internal enum TableWagerChange { None, Place, Change }

internal readonly record struct TableWagerTransition(TableWagerChange Change, bool Settle);

internal static class TableWagerPresentation
{
    private static readonly decimal[] Denominations = [1000m, 500m, 100m, 25m, 5m, 1m, .25m, .05m, .01m];

    internal static IReadOnlyList<TableChipGroup> Chips(decimal amount)
    {
        var groups = new List<TableChipGroup>();
        decimal remaining = Math.Max(0m, amount);
        foreach (decimal denomination in Denominations)
        {
            decimal count = decimal.Floor(remaining / denomination);
            if (count == 0m) continue;
            groups.Add(new TableChipGroup(denomination, count));
            remaining -= count * denomination;
        }

        // Odd-cent blackjack/surrender returns can fund a fractional-cent wager.
        // Keep that remainder exact rather than rounding the visual or game state.
        if (remaining > 0m) groups.Add(new TableChipGroup(remaining, 1m));
        return groups;
    }

    internal static string Amount(decimal amount) => "$" + amount.ToString("#,0.00##########################", CultureInfo.CurrentCulture);
}

// Tracks only previously displayed values, using object identity so resplits,
// session resets and consecutive equal wagers remain independent presentations.
internal sealed class TableWagerHistory
{
    private object? round;
    private readonly Dictionary<object, (decimal Amount, bool Settled)> hands = new(ReferenceEqualityComparer.Instance);

    internal TableWagerTransition Observe(object roundIdentity, object handIdentity, decimal amount, bool settled, bool animations)
    {
        if (!ReferenceEquals(round, roundIdentity))
        {
            round = roundIdentity;
            hands.Clear();
        }

        bool known = hands.TryGetValue(handIdentity, out var previous);
        TableWagerChange change = amount <= 0m ? TableWagerChange.None
            : !known || previous.Amount <= 0m ? TableWagerChange.Place
            : previous.Amount != amount ? TableWagerChange.Change : TableWagerChange.None;
        bool settle = settled && (!known || !previous.Settled);
        hands[handIdentity] = (amount, settled);
        return animations ? new(change, settle) : default;
    }
}
