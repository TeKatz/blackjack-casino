namespace BlackjackCasino.Core.Empire;

public sealed record EmpireAccrual(decimal Credits, decimal Dividend, TimeSpan Elapsed, bool Capped);

/// <summary>Clock-driven simulation shared by active play, other game modes and offline catch-up.</summary>
public sealed class EmpireService
{
    private EmpireState state;

    public EmpireService(EmpireState? savedState = null) => state = EmpireState.Normalize(savedState);
    // Keep snapshots detached: a slot controller may retain an older preferences record.
    public EmpireState Snapshot => state with { Levels = new Dictionary<string, int>(state.Levels, StringComparer.Ordinal) };
    public bool Started => state.Started;
    public decimal Credits => state.Credits;
    public decimal Dividend => state.Dividend;
    public decimal ClaimableDividend => decimal.Floor(state.Dividend * 100m) / 100m;
    public int TotalLevels => state.Levels.Values.Sum();
    public int OpenRooms => state.Levels.Values.Count(level => level > 0);
    public decimal CreditsPerSecond => EmpireCatalog.Departments.Sum(room => EmpireRules.Production(room, Level(room.Id)));
    public decimal CashPerHour => Math.Min(40m, 2m + TotalLevels * .3m);
    public int Level(string id) => state.Levels.GetValueOrDefault(id);

    public void Start(DateTimeOffset now)
    {
        if (!state.Started) state = state with { Started = true, LastAccruedAt = now };
    }

    public EmpireAccrual Accrue(DateTimeOffset now)
    {
        if (!state.Started) return new(0, 0, TimeSpan.Zero, false);
        if (state.LastAccruedAt == default)
        {
            state = state with { LastAccruedAt = now };
            return new(0, 0, TimeSpan.Zero, false);
        }
        // Retain the watermark on clock rollback. Repeated calls cannot farm the same interval.
        if (now <= state.LastAccruedAt) return new(0, 0, TimeSpan.Zero, false);
        TimeSpan elapsed = now - state.LastAccruedAt;
        bool capped = elapsed > EmpireRules.MaximumCatchUp;
        if (capped) elapsed = EmpireRules.MaximumCatchUp;
        decimal seconds = (decimal)elapsed.Ticks / TimeSpan.TicksPerSecond;
        decimal credits = Math.Min(EmpireRules.MaximumCredits - state.Credits, CreditsPerSecond * seconds);
        // Accumulate exact rate-seconds and pay whole cents. Dividing each tick into a
        // repeating decimal would make frequent online ticks earn less than one offline tick.
        decimal cashRateSeconds = CashPerHour * seconds + state.DividendRemainder;
        decimal earnedCash = decimal.Truncate(cashRateSeconds / 36m) / 100m;
        decimal dividend = Math.Min(EmpireRules.MaximumDividend - state.Dividend, earnedCash);
        state = state with
        {
            Credits = state.Credits + credits,
            LifetimeCredits = Math.Min(EmpireRules.MaximumCredits, state.LifetimeCredits + credits),
            Dividend = state.Dividend + dividend,
            DividendRemainder = state.Dividend + dividend >= EmpireRules.MaximumDividend ? 0 : cashRateSeconds - earnedCash * 3600m,
            // Consume all absence, including discarded time beyond the cap.
            LastAccruedAt = now,
        };
        return new(credits, dividend, elapsed, capped);
    }

    public bool Upgrade(string id, DateTimeOffset now)
    {
        Accrue(now); // Earn at the old production rate up to the moment of purchase.
        EmpireDepartment? room = EmpireCatalog.Find(id);
        if (!state.Started || room is null || Level(id) >= EmpireRules.MaximumLevel) return false;
        decimal cost = EmpireRules.UpgradeCost(room, Level(id));
        if (state.Credits < cost) return false;
        var levels = new Dictionary<string, int>(state.Levels, StringComparer.Ordinal) { [id] = Level(id) + 1 };
        state = state with { Credits = state.Credits - cost, Levels = levels };
        return true;
    }

    /// <summary>Persist the reduced reserve and increased wallet together before changing memory.</summary>
    public decimal ClaimDividend(DateTimeOffset now, Action<EmpireState, decimal> commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        Accrue(now);
        decimal amount = ClaimableDividend;
        if (amount <= 0) return 0;
        EmpireState proposed = Snapshot with
        {
            Dividend = state.Dividend - amount,
            LifetimeCashClaimed = Math.Min(EmpireRules.MaximumCredits, state.LifetimeCashClaimed + amount),
        };
        commit(proposed, amount); // Throws on failure; the reserve remains available for retry.
        state = proposed;
        return amount;
    }
}
