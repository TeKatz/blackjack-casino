namespace BlackjackCasino.Core.Empire;

public sealed record EmpireAccrual(
    decimal Credits,
    decimal Dividend,
    TimeSpan Elapsed,
    bool Capped,
    IReadOnlyDictionary<string, decimal>? RoomCredits = null)
{
    public IReadOnlyDictionary<string, decimal> Rooms { get; init; } = RoomCredits ?? new Dictionary<string, decimal>(StringComparer.Ordinal);
}

public sealed record EmpireUpgradeQuote(string RoomId, int RequestedLevels, int Levels, int StartLevel, int TargetLevel, decimal Cost, bool Unlocked);
public sealed record EmpirePrestigeResult(bool Success, int Prestige, decimal PermanentProductionBonus, string Message);

/// <summary>Clock-driven simulation shared by active play, other game modes and offline catch-up.</summary>
public sealed class EmpireService
{
    private EmpireState state;
    private readonly HashSet<string> claimedAchievements = new(StringComparer.Ordinal);

    public EmpireService(EmpireState? savedState = null) => state = EmpireState.Normalize(savedState);

    // Keep snapshots detached: a slot controller may retain an older preferences record.
    public EmpireState Snapshot => state with { Levels = new Dictionary<string, int>(state.Levels, StringComparer.Ordinal) };
    public bool Started => state.Started;
    public decimal Credits => state.Credits;
    public decimal Dividend => state.Dividend;
    public decimal ClaimableDividend => decimal.Floor(state.Dividend * 100m) / 100m;
    public int Prestige => state.Prestige;
    public decimal PrestigeMultiplier => EmpireRules.PrestigeMultiplier(state.Prestige);
    public decimal PrestigeBonusPercent => PrestigeMultiplier - 1m;
    public int TotalLevels => state.Levels.Values.Sum();
    public int MainFloorLevels => EmpireCatalog.Floor(1).Sum(room => Level(room.Id));
    public int OpenRooms => EmpireCatalog.Departments.Count(room => Level(room.Id) > 0);
    public decimal CreditsPerSecond => EmpireCatalog.Departments.Sum(room => RoomCreditsPerSecond(room.Id));
    public decimal CashPerHour => Math.Min(40m, 2m + TotalLevels * .3m);
    public int Level(string id) => state.Levels.GetValueOrDefault(id);
    public bool FloorUnlocked(int floor) => state.Prestige >= EmpireRules.RequiredPrestigeForFloor(floor);
    public bool Floor2Unlocked => FloorUnlocked(2);
    public int OpenRoomsOnFloor(int floor) => EmpireCatalog.Floor(floor).Count(room => Level(room.Id) > 0);
    public IReadOnlyList<EmpireAchievementBoost> ActiveAchievementBoosts => EmpireAchievementBoostCatalog.Active(claimedAchievements);
    public int PrestigeMainFloorLevelRequirement => EmpireRules.PrestigeMainFloorLevelRequirement(state.Prestige);
    public decimal PrestigeRunCreditRequirement => EmpireRules.PrestigeRunCreditRequirement(state.Prestige);
    public bool CanPrestige => state.Started
        && state.Prestige < EmpireRules.MaximumPrestige
        && EmpireCatalog.Floor(1).All(room => Level(room.Id) > 0)
        && MainFloorLevels >= PrestigeMainFloorLevelRequirement
        && state.RunCredits >= PrestigeRunCreditRequirement;

    public void SetClaimedAchievements(IEnumerable<string>? claimedChallengeIds)
    {
        claimedAchievements.Clear();
        if (claimedChallengeIds is not null) claimedAchievements.UnionWith(claimedChallengeIds);
    }

    public decimal AchievementBonusPercent(string roomId)
        => EmpireAchievementBoostCatalog.BonusForRoom(claimedAchievements, roomId);

    public decimal RoomCreditsPerSecond(string id)
    {
        EmpireDepartment? room = EmpireCatalog.Find(id);
        if (room is null || !FloorUnlocked(room.Floor)) return 0;
        decimal baseProduction = EmpireRules.Production(room, Level(id));
        return baseProduction * PrestigeMultiplier * (1m + AchievementBonusPercent(id));
    }

    public void Start(DateTimeOffset now)
    {
        if (!state.Started) state = state with { Started = true, LastAccruedAt = now };
    }

    public EmpireAccrual Accrue(DateTimeOffset now)
    {
        if (!state.Started) return EmptyAccrual();
        if (state.LastAccruedAt == default)
        {
            state = state with { LastAccruedAt = now };
            return EmptyAccrual();
        }
        // Retain the watermark on clock rollback. Repeated calls cannot farm the same interval.
        if (now <= state.LastAccruedAt) return EmptyAccrual();

        TimeSpan elapsed = now - state.LastAccruedAt;
        bool capped = elapsed > EmpireRules.MaximumCatchUp;
        if (capped) elapsed = EmpireRules.MaximumCatchUp;
        decimal seconds = (decimal)elapsed.Ticks / TimeSpan.TicksPerSecond;

        var roomCredits = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (EmpireDepartment room in EmpireCatalog.Departments)
        {
            decimal earned = RoomCreditsPerSecond(room.Id) * seconds;
            if (earned > 0) roomCredits[room.Id] = earned;
        }

        decimal potentialCredits = roomCredits.Values.Sum();
        decimal availableCapacity = Math.Max(0, EmpireRules.MaximumCredits - state.Credits);
        decimal credits = Math.Min(availableCapacity, potentialCredits);
        if (potentialCredits > credits && potentialCredits > 0)
        {
            decimal scale = credits / potentialCredits;
            foreach (string key in roomCredits.Keys.ToArray()) roomCredits[key] *= scale;
        }

        // Accumulate exact rate-seconds and pay whole cents. Dividing each tick into a
        // repeating decimal would make frequent online ticks earn less than one offline tick.
        decimal cashRateSeconds = CashPerHour * seconds + state.DividendRemainder;
        decimal earnedCash = decimal.Truncate(cashRateSeconds / 36m) / 100m;
        decimal dividend = Math.Min(EmpireRules.MaximumDividend - state.Dividend, earnedCash);
        state = state with
        {
            Credits = state.Credits + credits,
            LifetimeCredits = Math.Min(EmpireRules.MaximumCredits, state.LifetimeCredits + credits),
            RunCredits = Math.Min(EmpireRules.MaximumCredits, state.RunCredits + credits),
            Dividend = state.Dividend + dividend,
            DividendRemainder = state.Dividend + dividend >= EmpireRules.MaximumDividend ? 0 : cashRateSeconds - earnedCash * 3600m,
            // Consume all absence, including discarded time beyond the cap.
            LastAccruedAt = now,
        };
        return new EmpireAccrual(credits, dividend, elapsed, capped, roomCredits);
    }

    public EmpireUpgradeQuote QuoteUpgrade(string id, int requestedLevels)
    {
        EmpireDepartment? room = EmpireCatalog.Find(id);
        int start = Level(id);
        bool unlocked = room is not null && FloorUnlocked(room.Floor);
        if (room is null || !unlocked || requestedLevels <= 0 || start >= EmpireRules.MaximumLevel)
            return new(id, requestedLevels, 0, start, start, 0, unlocked);

        int levels = Math.Min(requestedLevels, EmpireRules.MaximumLevel - start);
        decimal cost = EmpireRules.UpgradeCost(room, start, levels);
        return new(id, requestedLevels, levels, start, start + levels, cost, true);
    }

    public bool Upgrade(string id, DateTimeOffset now) => UpgradeMany(id, 1, now);

    public bool UpgradeMany(string id, int requestedLevels, DateTimeOffset now)
    {
        Accrue(now); // Earn at the old production rate up to the moment of purchase.
        if (!state.Started) return false;
        EmpireUpgradeQuote quote = QuoteUpgrade(id, requestedLevels);
        if (!quote.Unlocked || quote.Levels <= 0 || state.Credits < quote.Cost) return false;

        var levels = new Dictionary<string, int>(state.Levels, StringComparer.Ordinal)
        {
            [id] = quote.TargetLevel,
        };
        state = state with { Credits = state.Credits - quote.Cost, Levels = levels };
        return true;
    }

    public EmpirePrestigeResult PrestigeEmpire(DateTimeOffset now)
    {
        Accrue(now);
        if (state.Prestige >= EmpireRules.MaximumPrestige)
            return new(false, state.Prestige, PrestigeBonusPercent, "Maximum Empire prestige reached.");
        if (!CanPrestige)
            return new(false, state.Prestige, PrestigeBonusPercent, PrestigeRequirementText());

        int nextPrestige = state.Prestige + 1;
        var resetLevels = EmpireCatalog.Departments.ToDictionary(room => room.Id, _ => 0, StringComparer.Ordinal);
        resetLevels["slots"] = 1;
        state = state with
        {
            Prestige = nextPrestige,
            Credits = 150m,
            RunCredits = 0,
            Levels = resetLevels,
            LastAccruedAt = now,
        };
        decimal bonus = EmpireRules.PrestigeMultiplier(nextPrestige) - 1m;
        string floor = nextPrestige == 1 ? " The Upper Floor is now permanently unlocked." : string.Empty;
        return new(true, nextPrestige, bonus, $"Empire Prestige {nextPrestige} reached. Permanent production bonus: +{bonus:P0}.{floor}");
    }

    public string PrestigeRequirementText()
    {
        if (state.Prestige >= EmpireRules.MaximumPrestige) return "Maximum Empire prestige reached.";
        int missingRooms = EmpireCatalog.Floor(1).Count(room => Level(room.Id) == 0);
        if (missingRooms > 0) return $"Open every Main Floor room first ({missingRooms} remaining).";
        if (MainFloorLevels < PrestigeMainFloorLevelRequirement)
            return $"Reach {PrestigeMainFloorLevelRequirement:N0} Main Floor levels ({MainFloorLevels:N0}/{PrestigeMainFloorLevelRequirement:N0}).";
        if (state.RunCredits < PrestigeRunCreditRequirement)
            return $"Produce {PrestigeRunCreditRequirement:N0} credits this run ({state.RunCredits:N0}/{PrestigeRunCreditRequirement:N0}).";
        return "Ready for Empire Prestige.";
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

    private static EmpireAccrual EmptyAccrual()
        => new(0, 0, TimeSpan.Zero, false, new Dictionary<string, decimal>(StringComparer.Ordinal));
}
