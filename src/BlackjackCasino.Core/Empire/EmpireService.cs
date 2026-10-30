namespace BlackjackCasino.Core.Empire;

public sealed record EmpireAccrual(decimal Credits, decimal Dividend, TimeSpan Elapsed, bool Capped, IReadOnlyDictionary<string, decimal>? RoomCredits = null)
{
    public IReadOnlyDictionary<string, decimal> Rooms { get; init; } = RoomCredits ?? new Dictionary<string, decimal>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, decimal> Properties { get; init; } = new Dictionary<string, decimal>(StringComparer.Ordinal);
}
public sealed record EmpireUpgradeQuote(string RoomId, int RequestedLevels, int Levels, int StartLevel, int TargetLevel, decimal Cost, bool Unlocked);
public sealed record EmpirePrestigeResult(bool Success, int Prestige, decimal PermanentProductionBonus, string Message);
public sealed record EmpireActionResult(bool Success, string Message);

/// <summary>Deterministic portfolio simulation. The only cash bridge is the bounded owner dividend.</summary>
public sealed partial class EmpireService
{
    private EmpireState state;
    private readonly HashSet<string> claimedAchievements = new(StringComparer.Ordinal);
    private EmpireActivitySnapshot activity = new();
    private EmpirePropertyState Estate => state.Properties[state.ActivePropertyId];
    public EmpireService(EmpireState? savedState = null) => state = EmpireState.Normalize(savedState);
    public EmpireState Snapshot => EmpireState.Normalize(state);
    public EmpirePropertyState PropertySnapshot(string id) => Snapshot.Properties[id];
    public bool Started => state.Started;
    public string ActivePropertyId => state.ActivePropertyId;
    public EmpirePropertyDefinition ActiveProperty => EmpirePropertyCatalog.Find(ActivePropertyId)!;
    public IReadOnlyList<EmpirePropertyDefinition> OwnedProperties => EmpirePropertyCatalog.All.Where(p => state.Properties.ContainsKey(p.Id)).ToArray();
    public decimal Credits => Estate.Credits;
    public decimal Dividend => state.Dividend;
    public decimal ClaimableDividend => decimal.Floor(state.Dividend * 100m) / 100m;
    public int Prestige => Estate.Prestige;
    public int TotalPrestige => state.Properties.Values.Sum(p => p.Prestige);
    public decimal PrestigeMultiplier => EmpireRules.PrestigeMultiplier(Prestige);
    public decimal PrestigeBonusPercent => PrestigeMultiplier - 1;
    public int TotalLevels => Estate.Levels.Values.Sum();
    public int MainFloorLevels => EmpireCatalog.Floor(1).Sum(room => Level(room.Id));
    public int OpenRooms => Estate.Levels.Values.Count(level => level > 0);
    public decimal CreditsPerSecond => ProductionForEstate(Estate, ActivePropertyId);
    public decimal TotalCreditsPerSecond => state.Properties.Sum(pair => ProductionForEstate(pair.Value, pair.Key));
    public decimal CashPerHour => Math.Min(60m, Math.Min(40m, 2m + state.Properties.Values.Sum(p => p.Levels.Values.Sum()) * .3m)
        * (1 + ResearchRank("guests") * .05m + state.Properties.Values.Count(p => p.Projects.Contains("penthouse-suites")) * .05m));
    public decimal DividendCapacity => Math.Min(640m, EmpireRules.MaximumDividend + ResearchRank("guests") * 40m + state.Properties.Values.Count(p => p.Projects.Contains("casino-vault")) * 160m);
    public TimeSpan OfflineCapacity => EmpireRules.MaximumCatchUp + TimeSpan.FromHours(ResearchRank("night-shift") * 2);
    public int Level(string id) => Estate.Levels.GetValueOrDefault(id);
    public bool FloorUnlocked(int floor) => FloorUnlockedFor(Estate, floor);
    public bool Floor2Unlocked => FloorUnlocked(2);
    public int OpenRoomsOnFloor(int floor) => EmpireCatalog.Floor(floor).Count(room => Level(room.Id) > 0);
    public IReadOnlyList<EmpireAchievementBoost> ActiveAchievementBoosts => EmpireAchievementBoostCatalog.Active(claimedAchievements);
    public int PrestigeMainFloorLevelRequirement => EmpireRules.PrestigeMainFloorLevelRequirement(Prestige);
    public decimal PrestigeRunCreditRequirement => EmpireRules.PrestigeRunCreditRequirement(Prestige);
    public bool CanPrestige => Started && Prestige < EmpireRules.MaximumPrestige && EmpireCatalog.Floor(1).All(r => Level(r.Id) > 0)
        && MainFloorLevels >= PrestigeMainFloorLevelRequirement && Estate.RunCredits >= PrestigeRunCreditRequirement;

    private void Put(string id, EmpirePropertyState estate)
    {
        var properties = new Dictionary<string, EmpirePropertyState>(state.Properties, StringComparer.Ordinal) { [id] = estate };
        EmpirePropertyState active = properties[ActivePropertyId];
        state = state with { Properties = properties, Credits = active.Credits, RunCredits = active.RunCredits, Prestige = active.Prestige, Levels = active.Levels };
    }
    public void SetClaimedAchievements(IEnumerable<string>? ids)
    {
        claimedAchievements.Clear();
        if (ids is not null) claimedAchievements.UnionWith(ids);
    }
    public decimal AchievementBonusPercent(string roomId) => EmpireAchievementBoostCatalog.BonusForRoom(claimedAchievements, roomId);
    public decimal RoomCreditsPerSecond(string id) => RoomRate(Estate, ActivePropertyId, EmpireCatalog.Find(id), state.LastAccruedAt);
    private decimal ProductionForEstate(EmpirePropertyState estate, string propertyId) => EmpireCatalog.Departments.Sum(room => RoomRate(estate, propertyId, room, state.LastAccruedAt));
    private static bool FloorUnlockedFor(EmpirePropertyState estate, int floor) => EmpireFloorCatalog.Find(floor) is { } definition
        && estate.Prestige >= definition.Requirement.Prestige && (definition.Requirement.ProjectId is null || estate.Projects.Contains(definition.Requirement.ProjectId));
    public void SetActivitySnapshot(EmpireActivitySnapshot current) => activity = current;

    public EmpireActionResult Transact(Func<EmpireService, EmpireActionResult> change, Action<EmpireState> commit)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(commit);
        var candidate = new EmpireService(Snapshot);
        candidate.SetClaimedAchievements(claimedAchievements);
        candidate.activity = activity;
        EmpireActionResult result = change(candidate);
        if (!result.Success) return result;
        commit(candidate.Snapshot);
        state = candidate.state;
        activity = candidate.activity;
        return result;
    }

    public void Start(DateTimeOffset now)
    {
        if (!Started) state = state with { Started = true, LastAccruedAt = now };
    }

    public EmpireAccrual Accrue(DateTimeOffset now)
    {
        if (!Started) return EmptyAccrual();
        if (state.LastAccruedAt == default) { state = state with { LastAccruedAt = now }; return EmptyAccrual(); }
        if (now <= state.LastAccruedAt) return EmptyAccrual();
        TimeSpan absence = now - state.LastAccruedAt;
        TimeSpan elapsed = absence > OfflineCapacity ? OfflineCapacity : absence;
        DateTimeOffset cursor = state.LastAccruedAt, end = cursor + elapsed;
        var rooms = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var properties = new Dictionary<string, decimal>(StringComparer.Ordinal);
        decimal paid = 0, cash = 0;
        while (cursor < end)
        {
            // At most one automation decision per property per 30 seconds. Online and
            // offline calls visit the same boundaries; tick frequency cannot increase spending.
            foreach (string id in state.Properties.Keys.ToArray())
            {
                EmpirePropertyState estate = ExpireEffects(state.Properties[id], cursor);
                if (AutomationUnlockedFor(estate) && estate.Automation.Enabled)
                {
                    if (estate.NextAutomationAt == default) estate = estate with { NextAutomationAt = cursor.AddSeconds(30) };
                    if (estate.NextAutomationAt <= cursor)
                        estate = Automate(estate) with { NextAutomationAt = cursor.AddSeconds(30) };
                }
                Put(id, estate);
            }
            DateTimeOffset boundary = end;
            foreach (EmpirePropertyState estate in state.Properties.Values)
            {
                if (estate.Event is { } ev && ev.EndsAt > cursor && ev.EndsAt < boundary) boundary = ev.EndsAt;
                if (estate.ContractBuff is { } buff && cursor.AddSeconds((double)buff.RemainingSeconds) < boundary)
                    boundary = cursor.AddSeconds((double)buff.RemainingSeconds);
                if (AutomationUnlockedFor(estate) && estate.Automation.Enabled && estate.NextAutomationAt > cursor && estate.NextAutomationAt < boundary)
                    boundary = estate.NextAutomationAt;
            }
            decimal seconds = (decimal)(boundary - cursor).Ticks / TimeSpan.TicksPerSecond;
            decimal cashRate = CashPerHour;
            foreach (string id in state.Properties.Keys.ToArray())
            {
                EmpirePropertyState estate = state.Properties[id];
                var earnedByRoom = EmpireCatalog.Departments.ToDictionary(room => room.Id, room => RoomRate(estate, id, room, cursor) * seconds, StringComparer.Ordinal);
                decimal potential = earnedByRoom.Values.Sum();
                decimal earned = Math.Min(Math.Max(0, EmpireRules.MaximumCredits - estate.Credits), potential);
                decimal factor = potential > 0 ? earned / potential : 0;
                var tags = new Dictionary<EmpireTag, decimal>(estate.ProducedByTag);
                foreach (EmpireDepartment room in EmpireCatalog.Departments)
                {
                    decimal value = earnedByRoom[room.Id] * factor;
                    if (value <= 0) continue;
                    if (id == ActivePropertyId) rooms[room.Id] = rooms.GetValueOrDefault(room.Id) + value;
                    foreach (EmpireTag tag in Enum.GetValues<EmpireTag>())
                        if (tag != EmpireTag.None && room.Tags.HasFlag(tag)) tags[tag] = Math.Min(EmpireRules.MaximumCredits, tags.GetValueOrDefault(tag) + value);
                }
                Put(id, estate with
                {
                    Credits = estate.Credits + earned, LifetimeCredits = Math.Min(EmpireRules.MaximumCredits, estate.LifetimeCredits + earned),
                    RunCredits = Math.Min(EmpireRules.MaximumCredits, estate.RunCredits + earned), ProducedByTag = tags,
                    ContractBuff = estate.ContractBuff is { } activeBuff ? activeBuff with { RemainingSeconds = Math.Max(0, activeBuff.RemainingSeconds - seconds) } : null,
                });
                properties[id] = properties.GetValueOrDefault(id) + earned;
                paid += earned;
            }
            decimal rateSeconds = cashRate * seconds + state.DividendRemainder;
            decimal earnedCash = decimal.Truncate(rateSeconds / 36m) / 100m;
            decimal dividend = Math.Min(DividendCapacity - state.Dividend, earnedCash);
            cash += dividend;
            state = state with
            {
                Dividend = state.Dividend + dividend,
                DividendRemainder = state.Dividend + dividend >= DividendCapacity ? 0 : rateSeconds - earnedCash * 3600m,
            };
            cursor = boundary;
        }
        foreach (string id in state.Properties.Keys.ToArray())
        {
            EmpirePropertyState estate = ExpireEffects(state.Properties[id], now);
            // Execute a decision exactly on the end boundary, not one tick later.
            if (AutomationUnlockedFor(estate) && estate.Automation.Enabled && estate.NextAutomationAt <= now)
                estate = (absence <= OfflineCapacity ? Automate(estate) : estate) with { NextAutomationAt = now.AddSeconds(30) };
            Put(id, estate);
        }
        state = state with { LastAccruedAt = now, LifetimeCredits = Math.Min(EmpireRules.MaximumCredits, state.LifetimeCredits + paid) };
        return new(paid, cash, elapsed, absence > OfflineCapacity, rooms) { Properties = properties };
    }

    public EmpireUpgradeQuote QuoteUpgrade(string id, int requestedLevels) => QuoteFor(Estate, id, requestedLevels);
    private EmpireUpgradeQuote QuoteFor(EmpirePropertyState estate, string id, int requestedLevels)
    {
        EmpireDepartment? room = EmpireCatalog.Find(id);
        int start = estate.Levels.GetValueOrDefault(id);
        bool unlocked = room is not null && FloorUnlockedFor(estate, room.Floor) && RequirementsMet(estate, room.Requirements ?? new());
        if (!unlocked || room is null || requestedLevels <= 0 || start >= EmpireRules.MaximumLevel) return new(id, requestedLevels, 0, start, start, 0, unlocked);
        int levels = Math.Min(requestedLevels, EmpireRules.MaximumLevel - start);
        decimal discount = ConstructionDiscountFor(estate, id);
        decimal cost = Enumerable.Range(start, levels).Sum(level => decimal.Ceiling(EmpireRules.UpgradeCost(room, level) * (1 - discount)));
        return new(id, requestedLevels, levels, start, start + levels, cost, true);
    }
    public bool Upgrade(string id, DateTimeOffset now) => UpgradeMany(id, 1, now);
    public bool UpgradeMany(string id, int requestedLevels, DateTimeOffset now)
    {
        Accrue(now);
        EmpireUpgradeQuote quote = QuoteUpgrade(id, requestedLevels);
        if (!Started || !quote.Unlocked || quote.Levels <= 0 || Credits < quote.Cost) return false;
        Put(ActivePropertyId, BuyLevels(Estate, quote));
        return true;
    }
    private static EmpirePropertyState BuyLevels(EmpirePropertyState estate, EmpireUpgradeQuote quote) => estate with
    {
        Credits = estate.Credits - quote.Cost,
        Levels = new(estate.Levels, StringComparer.Ordinal) { [quote.RoomId] = quote.TargetLevel },
        LevelsPurchased = Math.Min(long.MaxValue - quote.Levels, estate.LevelsPurchased) + quote.Levels,
    };
    public EmpirePrestigeResult PrestigeEmpire(DateTimeOffset now)
    {
        Accrue(now);
        if (!CanPrestige) return new(false, Prestige, PrestigeBonusPercent, PrestigeRequirementText());
        int next = Prestige + 1;
        var levels = EmpireCatalog.Departments.ToDictionary(room => room.Id, _ => 0, StringComparer.Ordinal);
        levels["slots"] = 1;
        Put(ActivePropertyId, Estate with { Prestige = next, Credits = 150, RunCredits = 0, Levels = levels, Event = null, NextAutomationAt = (now > state.LastAccruedAt ? now : state.LastAccruedAt).AddSeconds(30) });
        return new(true, next, PrestigeBonusPercent, $"Empire Prestige {next}. Production +{PrestigeBonusPercent:P0}. +1 Legacy Point. Research and capital projects retained.");
    }
    public string PrestigeRequirementText()
    {
        if (Prestige >= EmpireRules.MaximumPrestige) return "Maximum Empire prestige reached.";
        int missing = EmpireCatalog.Floor(1).Count(room => Level(room.Id) == 0);
        if (missing > 0) return $"Open every Main Floor room first ({missing} remaining).";
        if (MainFloorLevels < PrestigeMainFloorLevelRequirement) return $"Reach {PrestigeMainFloorLevelRequirement:N0} Main Floor levels ({MainFloorLevels:N0}/{PrestigeMainFloorLevelRequirement:N0}).";
        if (Estate.RunCredits < PrestigeRunCreditRequirement) return $"Produce {PrestigeRunCreditRequirement:N0} credits this run ({Estate.RunCredits:N0}/{PrestigeRunCreditRequirement:N0}).";
        return "Ready for Empire Prestige. Keep projects and research; reset this property's levels, credits and active event.";
    }
    public decimal ClaimDividend(DateTimeOffset now, Action<EmpireState, decimal> commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        Accrue(now);
        decimal amount = ClaimableDividend;
        if (amount <= 0) return 0;
        EmpireState proposed = Snapshot with { Dividend = state.Dividend - amount, LifetimeCashClaimed = Math.Min(EmpireRules.MaximumCredits, state.LifetimeCashClaimed + amount) };
        commit(proposed, amount);
        state = proposed;
        return amount;
    }
    private static EmpireAccrual EmptyAccrual() => new(0, 0, TimeSpan.Zero, false);
}
