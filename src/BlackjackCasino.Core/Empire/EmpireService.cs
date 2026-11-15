namespace BlackjackCasino.Core.Empire;

public sealed record EmpireActionResult(bool Success, string Message);
public sealed record EmpireAccrual(decimal Credits, TimeSpan Elapsed, bool Capped);

/// <summary>Authoritative, intentionally compact incremental clicker economy.</summary>
public sealed class EmpireService
{
    private EmpireState state;
    public EmpireService(EmpireState? saved = null)
    {
        state = EmpireState.Normalize(saved);
        ReconcileProgression();
    }
    public EmpireState Snapshot => EmpireState.Normalize(state);
    public bool Started => state.Started;
    public decimal Credits => state.Credits;
    public decimal LifetimeCredits => state.LifetimeCredits;
    public decimal RunCredits => state.RunCredits;
    public long Clicks => state.Clicks;
    public int Prestige => state.Prestige;
    public int PrestigeTokens => state.PrestigeTokens;
    public int PeakBuildingLevels => state.PeakBuildingLevels;
    public IReadOnlyList<string> PermanentUpgrades => state.PermanentUpgrades;
    public IReadOnlyList<string> CompletedMilestones => state.CompletedMilestones;
    public bool OwnsLegacyUpgrade(string id) => state.PermanentUpgrades.Contains(id, StringComparer.Ordinal);
    public bool CanBuyLegacyUpgrade(EmpireLegacyUpgrade upgrade) =>
        Started && Prestige >= 1 && Prestige >= upgrade.MinimumPrestige
        && !OwnsLegacyUpgrade(upgrade.Id)
        && (upgrade.PreviousId is null || OwnsLegacyUpgrade(upgrade.PreviousId))
        && PrestigeTokens >= upgrade.Cost;
    public decimal MilestoneValue(EmpireLegacyMilestone milestone) => milestone.Kind switch
    {
        "clicks" => Clicks,
        "credits" => LifetimeCredits,
        "levels" => PeakBuildingLevels,
        "prestige" => Prestige,
        _ => 0m,
    };
    private decimal Bonus(params (string Id, decimal Value)[] values) =>
        values.Where(x => OwnsLegacyUpgrade(x.Id)).Sum(x => x.Value);
    public decimal LegacyProductionMultiplier => 1m + Bonus(
        ("passive-1", .15m), ("passive-2", .10m), ("passive-3", .22m),
        ("passive-4", .25m), ("passive-5", .40m),
        ("hybrid-1", .08m), ("hybrid-2", .12m), ("hybrid-3", .15m),
        ("hybrid-4", .18m), ("hybrid-5", .25m));
    public decimal LegacyClickMultiplier => 1m + Bonus(
        ("active-1", .15m), ("active-3", .25m), ("active-5", .50m),
        ("hybrid-1", .08m), ("hybrid-2", .12m), ("hybrid-3", .15m),
        ("hybrid-4", .18m), ("hybrid-5", .25m));
    public int CriticalClickInterval => OwnsLegacyUpgrade("active-4") ? 20 : 25;
    public decimal CriticalClickMultiplier => OwnsLegacyUpgrade("active-2") ? 4m : 3m;
    public int TotalLevels => state.Buildings.Values.Sum();
    public int Level(string id) => state.Buildings.GetValueOrDefault(id);
    public decimal PrestigeMultiplier => 1m + state.Prestige * .30m;

    public bool Unlocked(EmpireBuilding building) => building.RequiresId is null
        || Level(building.RequiresId) >= building.RequiresLevel;

    public bool Visible(EmpireBuilding building)
    {
        if (Unlocked(building)) return true;
        if (building.RequiresId is null) return true;
        EmpireBuilding? previous = EmpireCatalog.Find(building.RequiresId);
        return previous is not null && Unlocked(previous);
    }

    public decimal Production(EmpireBuilding building)
    {
        int level = Level(building.Id);
        if (level <= 0) return 0m;
        decimal tier = level >= 8 ? 2.6m : level >= 5 ? 1.75m : level >= 3 ? 1.3m : 1m;
        decimal synergy = 1m;
        if (building.Id is "slots" or "roulette" && Level("roulette") >= 3) synergy += .15m;
        if (building.Id is "tables" or "poker" && Level("poker") >= 3) synergy += .15m;
        if (building.Id is "bar" or "stage" && Level("stage") >= 3) synergy += .15m;
        if (Level("hotel") >= 5) synergy += .10m;
        return decimal.Floor(building.BaseRate * level * (1m + (level - 1) * .30m)
            * tier * synergy * PrestigeMultiplier * LegacyProductionMultiplier * 100m) / 100m;
    }

    public decimal CreditsPerSecond => EmpireCatalog.Buildings.Sum(Production);

    public decimal CreditsPerClick
    {
        get
        {
            decimal baseValue = 1m + EmpireCatalog.Buildings.Sum(b => b.ClickPower * Level(b.Id));
            decimal passiveContribution = decimal.Floor(CreditsPerSecond * (.60m + Level("bar") * .025m));
            return Math.Max(1m, decimal.Floor((baseValue + passiveContribution) * PrestigeMultiplier * LegacyClickMultiplier));
        }
    }

    public decimal NextCost(EmpireBuilding building) => EmpireCatalog.NextCost(building, Level(building.Id));
    public bool CanBuy(EmpireBuilding building) =>
        Started && Unlocked(building) && Level(building.Id) < EmpireCatalog.MaximumLevel
        && Credits >= NextCost(building);

    public decimal PrestigeRequirement => 250_000m * (Prestige + 1m) * (Prestige + 1m);
    public bool CanPrestige => Started && Prestige < EmpireCatalog.MaximumPrestige
        && TotalLevels >= 20 && RunCredits >= PrestigeRequirement;

    private void ReconcileProgression()
    {
        if (!state.Started) return;

        // Retroactively grant resets from pre-update v4 saves. The granted
        // counter makes this idempotent across reloads and repeated snapshots.
        int missingResets = Math.Max(0, state.Prestige - state.GrantedPrestigeLevels);
        int balance = Math.Min(EmpireLegacyCatalog.MaximumTokens,
            state.PrestigeTokens + missingResets * EmpireLegacyCatalog.PrestigeTokensPerReset);
        int peak = Math.Max(state.PeakBuildingLevels, TotalLevels);
        var earned = new List<string>(state.CompletedMilestones);
        foreach (EmpireLegacyMilestone milestone in EmpireLegacyCatalog.Milestones)
        {
            decimal value = milestone.Kind switch
            {
                "clicks" => Clicks,
                "credits" => LifetimeCredits,
                "levels" => peak,
                "prestige" => Prestige,
                _ => 0m,
            };
            if (value < milestone.Target || earned.Contains(milestone.Id, StringComparer.Ordinal))
                continue;
            earned.Add(milestone.Id);
            balance = Math.Min(EmpireLegacyCatalog.MaximumTokens, balance + 1);
        }
        state = state with
        {
            PrestigeTokens = balance,
            GrantedPrestigeLevels = state.Prestige,
            PeakBuildingLevels = peak,
            CompletedMilestones = earned,
        };
    }

    private static decimal Add(decimal a, decimal b) =>
        Math.Min(EmpireCatalog.MaximumCredits, a + Math.Min(Math.Max(0m, b), EmpireCatalog.MaximumCredits - a));

    public void Start(DateTimeOffset now)
    {
        if (!Started) state = state with { Started = true, LastAccruedAt = now };
        ReconcileProgression();
    }

    public EmpireAccrual Accrue(DateTimeOffset now)
    {
        if (!Started) return new(0m, TimeSpan.Zero, false);
        if (state.LastAccruedAt == default)
        {
            state = state with { LastAccruedAt = now };
            return new(0m, TimeSpan.Zero, false);
        }
        if (now <= state.LastAccruedAt) return new(0m, TimeSpan.Zero, false);
        TimeSpan absence = now - state.LastAccruedAt;
        TimeSpan elapsed = absence > EmpireCatalog.MaximumOfflineTime ? EmpireCatalog.MaximumOfflineTime : absence;
        decimal income = Math.Min(EmpireCatalog.MaximumCredits - Credits,
            CreditsPerSecond * (decimal)elapsed.Ticks / TimeSpan.TicksPerSecond);
        state = state with
        {
            Credits = Add(Credits, income),
            LifetimeCredits = Add(LifetimeCredits, income),
            RunCredits = Add(RunCredits, income),
            LastAccruedAt = now,
        };
        ReconcileProgression();
        return new(income, elapsed, absence > elapsed);
    }

    public EmpireActionResult Tap(int count, DateTimeOffset now)
    {
        if (!Started || count is < 1 or > 20) return new(false, "Invalid click batch.");
        Accrue(now);
        decimal value = CreditsPerClick, income = 0m;
        // Every 25th click gives a deterministic bonus, avoiding random payout replay.
        for (int i = 1; i <= count; i++)
        {
            bool critical = (state.Clicks + i) % CriticalClickInterval == 0;
            income = Add(income, value * (critical ? CriticalClickMultiplier : 1m));
        }
        state = state with
        {
            Credits = Add(Credits, income),
            LifetimeCredits = Add(LifetimeCredits, income),
            RunCredits = Add(RunCredits, income),
            Clicks = state.Clicks > long.MaxValue - count ? long.MaxValue : state.Clicks + count,
        };
        ReconcileProgression();
        return new(true, "Clicks collected.");
    }

    public EmpireActionResult Upgrade(string? id, DateTimeOffset now)
    {
        if (EmpireCatalog.Find(id) is not { } building) return new(false, "Unknown building.");
        Accrue(now);
        if (!CanBuy(building)) return new(false, "Building locked or insufficient credits.");
        var levels = new Dictionary<string, int>(state.Buildings, StringComparer.Ordinal)
        {
            [building.Id] = Level(building.Id) + 1,
        };
        state = state with { Buildings = levels, Credits = Credits - NextCost(building) };
        ReconcileProgression();
        return new(true, building.Name + " upgraded.");
    }

    public EmpireActionResult ResetForPrestige(DateTimeOffset now)
    {
        Accrue(now);
        if (!CanPrestige) return new(false, "Prestige requirements not met.");
        state = state with
        {
            Prestige = Prestige + 1,
            Credits = 0m,
            RunCredits = 0m,
            Buildings = new Dictionary<string, int>(StringComparer.Ordinal),
            LastAccruedAt = now,
        };
        ReconcileProgression();
        return new(true, "New prestige unlocked.");
    }

    public EmpireActionResult BuyLegacyUpgrade(string? id, DateTimeOffset now)
    {
        if (EmpireLegacyCatalog.FindUpgrade(id) is not { } upgrade)
            return new(false, "Unknown permanent upgrade.");
        Accrue(now);
        if (!CanBuyLegacyUpgrade(upgrade))
            return new(false, "Permanent upgrade locked, already owned or insufficient tokens.");
        state = state with
        {
            PrestigeTokens = state.PrestigeTokens - upgrade.Cost,
            PermanentUpgrades = [.. state.PermanentUpgrades, upgrade.Id],
        };
        return new(true, upgrade.Name + " unlocked permanently.");
    }

    /// <summary>Commit the candidate to disk before making it visible in this process.</summary>
    public EmpireActionResult Transact(Func<EmpireService, EmpireActionResult> action, Action<EmpireState> persist)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(persist);
        var candidate = new EmpireService(Snapshot);
        EmpireActionResult result = action(candidate);
        if (!result.Success) return result;
        persist(candidate.Snapshot);
        state = candidate.state;
        return result;
    }
}
