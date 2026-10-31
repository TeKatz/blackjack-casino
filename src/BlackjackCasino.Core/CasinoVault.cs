namespace BlackjackCasino.Core;

public sealed class CasinoVaultState
{
    // Missing vault data in older JSON deserializes with SchemaVersion 0. Normalization then
    // snapshots the historical wager total without retroactively granting vault cash.
    public int SchemaVersion { get; set; }
    public string TierId { get; set; } = string.Empty;
    public decimal CurrentAmount { get; set; }
    public decimal ObservedEligibleWager { get; set; }
    public decimal LifetimeContributions { get; set; }
    public decimal LifetimePayouts { get; set; }
    public decimal LifetimeBonuses { get; set; }
    public long VaultsOpened { get; set; }
    public decimal BiggestPayout { get; set; }
    public DateTimeOffset? LastOpenedAt { get; set; }

    // Opening a vault is a two-store transaction: the profile owns the vault while the app
    // state owns the play-money bankroll. A durable pending receipt makes recovery idempotent.
    public Guid PendingPayoutId { get; set; }
    public decimal PendingPayout { get; set; }
    public decimal PendingBasePayout { get; set; }
    public decimal PendingBonus { get; set; }
}

public sealed record CasinoVaultTierDefinition(
    string Id,
    string Name,
    decimal Capacity,
    decimal FullBonusRate,
    decimal RequiredLifetimeWager,
    string Accent);

public sealed record CasinoVaultContribution(
    decimal EligibleWagerDelta,
    decimal RawContribution,
    decimal CreditedContribution,
    decimal DiscardedOverflow,
    decimal AmountAfter,
    bool IsFull)
{
    public static CasinoVaultContribution None(decimal current, bool full = false) =>
        new(0m, 0m, 0m, 0m, current, full);
}

public sealed record CasinoVaultOpenResult(
    Guid PayoutId,
    string TierId,
    decimal BaseAmount,
    decimal BonusAmount,
    decimal TotalPayout,
    decimal BonusRate,
    DateTimeOffset OpenedAt);

public sealed record CasinoVaultSnapshot(
    CasinoVaultTierDefinition Tier,
    CasinoVaultTierDefinition UnlockedTier,
    CasinoVaultTierDefinition? NextTier,
    decimal CurrentAmount,
    decimal Progress,
    decimal ContributionRate,
    bool IsFull,
    bool PayoutPending,
    decimal LifetimeContributions,
    decimal LifetimePayouts,
    decimal LifetimeBonuses,
    long VaultsOpened,
    decimal BiggestPayout,
    decimal LifetimeEligibleWager);

public static class CasinoVaultTierCatalog
{
    private static readonly CasinoVaultTierDefinition[] Tiers =
    [
        new("bronze", "Bronze Vault", 10_000m, 0.10m, 0m, "#B87947"),
        new("silver", "Silver Vault", 25_000m, 0.15m, 250_000m, "#BFC9D2"),
        new("gold", "Gold Vault", 50_000m, 0.20m, 1_000_000m, "#E5B84C"),
        new("platinum", "Platinum Vault", 100_000m, 0.20m, 5_000_000m, "#8FE0DA"),
        new("diamond", "Diamond Vault", 250_000m, 0.25m, 20_000_000m, "#8FC8FF"),
    ];

    public static IReadOnlyList<CasinoVaultTierDefinition> All { get; } = Array.AsReadOnly(Tiers);
    public static CasinoVaultTierDefinition Bronze => Tiers[0];

    public static CasinoVaultTierDefinition Get(string? id) =>
        Tiers.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Bronze;

    public static CasinoVaultTierDefinition ResolveUnlocked(PlayerProfileState profile)
    {
        decimal wager = CasinoVaultService.TotalEligibleWager(profile);
        return Tiers.Last(tier => wager >= tier.RequiredLifetimeWager);
    }

    public static CasinoVaultTierDefinition? Next(CasinoVaultTierDefinition tier)
    {
        int index = Array.FindIndex(Tiers, x => string.Equals(x.Id, tier.Id, StringComparison.Ordinal));
        return index >= 0 && index + 1 < Tiers.Length ? Tiers[index + 1] : null;
    }
}

public static class CasinoVaultService
{
    public const int CurrentSchemaVersion = 1;
    public const decimal ContributionRate = 0.02m;

    public static CasinoVaultState Normalize(PlayerProfileState profile, CasinoVaultState? state)
    {
        ArgumentNullException.ThrowIfNull(profile);
        state ??= new CasinoVaultState();
        decimal lifetimeWager = TotalEligibleWager(profile);

        if (state.SchemaVersion < CurrentSchemaVersion)
        {
            // Existing players keep their career tier, but historical play is only the baseline:
            // enabling the feature never manufactures a retroactive cash payout.
            state.SchemaVersion = CurrentSchemaVersion;
            state.TierId = CasinoVaultTierCatalog.ResolveUnlocked(profile).Id;
            state.CurrentAmount = 0m;
            state.ObservedEligibleWager = lifetimeWager;
            state.PendingPayoutId = Guid.Empty;
            state.PendingPayout = 0m;
            state.PendingBasePayout = 0m;
            state.PendingBonus = 0m;
        }

        state.SchemaVersion = CurrentSchemaVersion;
        state.ObservedEligibleWager = Math.Max(0m, state.ObservedEligibleWager);
        state.LifetimeContributions = Math.Max(0m, state.LifetimeContributions);
        state.LifetimePayouts = Math.Max(0m, state.LifetimePayouts);
        state.LifetimeBonuses = Math.Max(0m, state.LifetimeBonuses);
        state.VaultsOpened = Math.Max(0L, state.VaultsOpened);
        state.BiggestPayout = Math.Max(0m, state.BiggestPayout);

        CasinoVaultTierDefinition tier = CasinoVaultTierCatalog.Get(state.TierId);
        state.TierId = tier.Id;
        state.CurrentAmount = Math.Clamp(state.CurrentAmount, 0m, tier.Capacity);

        if (state.PendingPayoutId == Guid.Empty || state.PendingPayout <= 0m)
        {
            state.PendingPayoutId = Guid.Empty;
            state.PendingPayout = 0m;
            state.PendingBasePayout = 0m;
            state.PendingBonus = 0m;
        }
        else
        {
            state.PendingBasePayout = Math.Max(0m, state.PendingBasePayout);
            state.PendingBonus = Math.Max(0m, state.PendingBonus);
            state.PendingPayout = Math.Max(0m, state.PendingPayout);
        }

        return state;
    }

    public static CasinoVaultContribution SyncFromStatistics(PlayerProfileState profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Vault = Normalize(profile, profile.Vault);
        CasinoVaultState state = profile.Vault;
        decimal eligibleWager = TotalEligibleWager(profile);

        if (eligibleWager <= state.ObservedEligibleWager)
        {
            return CasinoVaultContribution.None(state.CurrentAmount, IsFull(profile));
        }

        decimal delta = eligibleWager - state.ObservedEligibleWager;
        // Always advance the ledger, including while full. There is intentionally no hidden
        // overflow bank that can create a second payout after the player opens the vault.
        state.ObservedEligibleWager = eligibleWager;

        if (state.PendingPayoutId != Guid.Empty)
        {
            return new(delta, RoundCash(delta * ContributionRate), 0m, RoundCash(delta * ContributionRate), state.CurrentAmount, false);
        }

        if (state.CurrentAmount == 0m)
        {
            // Tier upgrades begin with a fresh vault cycle and never move the goalposts on a
            // partially filled vault.
            state.TierId = CasinoVaultTierCatalog.ResolveUnlocked(profile).Id;
        }

        CasinoVaultTierDefinition tier = CasinoVaultTierCatalog.Get(state.TierId);
        decimal raw = RoundCash(delta * ContributionRate);
        decimal room = Math.Max(0m, tier.Capacity - state.CurrentAmount);
        decimal credited = Math.Min(raw, room);
        decimal discarded = Math.Max(0m, raw - credited);
        state.CurrentAmount = Math.Min(tier.Capacity, state.CurrentAmount + credited);
        state.LifetimeContributions += credited;

        return new(delta, raw, credited, discarded, state.CurrentAmount, state.CurrentAmount >= tier.Capacity);
    }

    public static CasinoVaultOpenResult PrepareOpen(PlayerProfileState profile, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Vault = Normalize(profile, profile.Vault);
        CasinoVaultState state = profile.Vault;
        if (state.PendingPayoutId != Guid.Empty)
        {
            throw new InvalidOperationException("A Casino Vault payout is already pending recovery.");
        }

        CasinoVaultTierDefinition tier = CasinoVaultTierCatalog.Get(state.TierId);
        if (state.CurrentAmount < tier.Capacity)
        {
            throw new InvalidOperationException("The Casino Vault must be completely full before it can be opened.");
        }

        decimal baseAmount = tier.Capacity;
        decimal bonus = RoundCash(baseAmount * tier.FullBonusRate);
        decimal payout = checked(baseAmount + bonus);
        Guid id = Guid.NewGuid();
        DateTimeOffset openedAt = now ?? DateTimeOffset.UtcNow;

        state.PendingPayoutId = id;
        state.PendingBasePayout = baseAmount;
        state.PendingBonus = bonus;
        state.PendingPayout = payout;
        state.CurrentAmount = 0m;
        state.VaultsOpened++;
        state.LifetimePayouts = checked(state.LifetimePayouts + payout);
        state.LifetimeBonuses = checked(state.LifetimeBonuses + bonus);
        state.BiggestPayout = Math.Max(state.BiggestPayout, payout);
        state.LastOpenedAt = openedAt;

        return new(id, tier.Id, baseAmount, bonus, payout, tier.FullBonusRate, openedAt);
    }

    public static bool CompletePendingPayout(PlayerProfileState profile, Guid payoutId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Vault = Normalize(profile, profile.Vault);
        CasinoVaultState state = profile.Vault;
        if (payoutId == Guid.Empty || state.PendingPayoutId != payoutId)
        {
            return false;
        }

        state.PendingPayoutId = Guid.Empty;
        state.PendingPayout = 0m;
        state.PendingBasePayout = 0m;
        state.PendingBonus = 0m;
        state.TierId = CasinoVaultTierCatalog.ResolveUnlocked(profile).Id;
        return true;
    }

    public static CasinoVaultSnapshot Snapshot(PlayerProfileState profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Vault = Normalize(profile, profile.Vault);
        CasinoVaultState state = profile.Vault;
        CasinoVaultTierDefinition tier = CasinoVaultTierCatalog.Get(state.TierId);
        CasinoVaultTierDefinition unlocked = CasinoVaultTierCatalog.ResolveUnlocked(profile);
        decimal progress = tier.Capacity <= 0m ? 0m : Math.Clamp(state.CurrentAmount / tier.Capacity, 0m, 1m);
        return new(
            tier,
            unlocked,
            CasinoVaultTierCatalog.Next(unlocked),
            state.CurrentAmount,
            progress,
            ContributionRate,
            state.PendingPayoutId == Guid.Empty && state.CurrentAmount >= tier.Capacity,
            state.PendingPayoutId != Guid.Empty,
            state.LifetimeContributions,
            state.LifetimePayouts,
            state.LifetimeBonuses,
            state.VaultsOpened,
            state.BiggestPayout,
            TotalEligibleWager(profile));
    }

    public static bool IsFull(PlayerProfileState profile)
    {
        profile.Vault = Normalize(profile, profile.Vault);
        CasinoVaultTierDefinition tier = CasinoVaultTierCatalog.Get(profile.Vault.TierId);
        return profile.Vault.PendingPayoutId == Guid.Empty && profile.Vault.CurrentAmount >= tier.Capacity;
    }

    public static decimal TotalEligibleWager(PlayerProfileState profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        decimal total = 0m;
        Add(ref total, profile.LifetimeStatistics?.TotalWagered ?? 0m);
        Add(ref total, profile.Slots?.TotalWagered ?? 0m);
        Add(ref total, profile.LegacyPooledSlots?.TotalWagered ?? 0m);
        Add(ref total, profile.NeonVault?.TotalWagered ?? 0m);
        Add(ref total, profile.NineLives?.TotalWagered ?? 0m);
        Add(ref total, profile.Manor?.TotalWagered ?? 0m);
        Add(ref total, profile.Kniffel?.TotalWagered ?? 0m);
        return total;
    }

    private static decimal RoundCash(decimal value) =>
        decimal.Round(Math.Max(0m, value), 2, MidpointRounding.AwayFromZero);

    private static void Add(ref decimal total, decimal value)
    {
        value = Math.Max(0m, value);
        if (decimal.MaxValue - total < value)
        {
            total = decimal.MaxValue;
            return;
        }
        total += value;
    }
}
