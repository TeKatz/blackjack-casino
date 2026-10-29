using System.Globalization;
using BlackjackCasino.Desktop;

// No Windows runtime or test-package dependency: exercise the actual linked
// presentation helper on Linux as well as Windows.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var tests = new (string Name, Action Check)[]
{
    ("Exact denomination totals, including fractional-cent wagers", () =>
    {
        decimal[] cases = [0m, .005m, .01m, .99m, 1m, 25m, 37.50m, 999.995m, 12345.67m, 1000000000000.125m];
        foreach (decimal amount in cases) ExactChips(amount);
        var random = new Random(9127);
        for (int i = 0; i < 10_000; i++) ExactChips(random.Next(0, 100_000_000) / 100m + (i % 2 == 0 ? .005m : 0m));
        Equal(0, TableWagerPresentation.Chips(-1m).Count);
    }),
    ("Casino denominations and compact large stacks", () =>
    {
        var groups = TableWagerPresentation.Chips(138.25m);
        Equal(new TableChipGroup(100m, 1m), groups[0]);
        Equal(new TableChipGroup(25m, 1m), groups[1]);
        Equal(new TableChipGroup(5m, 2m), groups[2]);
        Equal(new TableChipGroup(1m, 3m), groups[3]);
        Equal(new TableChipGroup(.25m, 1m), groups[4]);
        var large = TableWagerPresentation.Chips(1_000_000m).Single();
        Equal(1000m, large.Count);
        Equal(6, large.VisibleCount);
        Equal("1K", large.Face);
        Equal("½¢", TableWagerPresentation.Chips(.005m).Single().Face);
    }),
    ("Amounts never round away an odd-cent settlement remainder", () =>
    {
        Equal("$0.00", TableWagerPresentation.Amount(0m));
        Equal("$1,234.50", TableWagerPresentation.Amount(1234.5m));
        Equal("$0.005", TableWagerPresentation.Amount(.005m));
        Equal("$37.505", TableWagerPresentation.Amount(37.505m));
    }),
    ("Deal places once; hits and refreshes do not replay", () =>
    {
        var history = new TableWagerHistory();
        object round = new(), hand = new();
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(round, hand, 25m, false, true));
        Equal(default(TableWagerTransition), history.Observe(round, hand, 25m, false, true));
    }),
    ("Double that settles reports both change and settlement once", () =>
    {
        var history = new TableWagerHistory();
        object round = new(), hand = new();
        history.Observe(round, hand, 25m, false, true);
        Equal(new TableWagerTransition(TableWagerChange.Change, true), history.Observe(round, hand, 50m, true, true));
        Equal(default(TableWagerTransition), history.Observe(round, hand, 50m, true, true));
    }),
    ("Split and resplit place each new hand; unaffected hands stay still", () =>
    {
        var history = new TableWagerHistory();
        object round = new(), original = new(), left = new(), right = new(), resplitLeft = new(), resplitRight = new();
        history.Observe(round, original, 25m, false, true);
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(round, left, 25m, false, true));
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(round, right, 25m, false, true));
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(round, resplitLeft, 25m, false, true));
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(round, resplitRight, 25m, false, true));
        Equal(default(TableWagerTransition), history.Observe(round, right, 25m, false, true));
        Equal(new TableWagerTransition(TableWagerChange.Change, false), history.Observe(round, right, 50m, false, true));
    }),
    ("Immediate blackjack settlement and new rounds do not lose animations", () =>
    {
        var history = new TableWagerHistory();
        object hand = new();
        Equal(new TableWagerTransition(TableWagerChange.Place, true), history.Observe(new object(), hand, 25m, true, true));
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(new object(), hand, 25m, false, true));
    }),
    ("Animations off records state and does not replay when enabled", () =>
    {
        var history = new TableWagerHistory();
        object round = new(), hand = new();
        Equal(default(TableWagerTransition), history.Observe(round, hand, 25m, false, false));
        Equal(default(TableWagerTransition), history.Observe(round, hand, 50m, true, false));
        Equal(default(TableWagerTransition), history.Observe(round, hand, 50m, true, true));
    }),
    ("Cleared preview has no placement; placing again does", () =>
    {
        var history = new TableWagerHistory();
        object round = new(), hand = new();
        Equal(default(TableWagerTransition), history.Observe(round, hand, 0m, false, true));
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(round, hand, 25m, false, true));
        Equal(default(TableWagerTransition), history.Observe(round, hand, 0m, false, true));
        Equal(new TableWagerTransition(TableWagerChange.Place, false), history.Observe(round, hand, 25m, false, true));
    }),
};

foreach (var test in tests)
{
    test.Check();
    Console.WriteLine("PASS " + test.Name);
}
Console.WriteLine($"PASS {tests.Length} table wager checks");

static void ExactChips(decimal amount)
{
    var groups = TableWagerPresentation.Chips(amount);
    Equal(amount, groups.Sum(group => group.Denomination * group.Count));
    if (groups.Any(group => group.Count <= 0m || group.Denomination <= 0m || group.VisibleCount is < 1 or > 6))
        throw new Exception("Invalid or unbounded chip stack");
    if (groups.Count > 10) throw new Exception("Unbounded chip groups");
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}");
}
