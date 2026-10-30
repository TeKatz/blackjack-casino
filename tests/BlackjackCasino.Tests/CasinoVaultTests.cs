using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class CasinoVaultTests
{
    [Fact]
    public void ExistingProfileStartsAtCareerTierWithoutRetroactiveCash()
    {
        var profile = new PlayerProfileState();
        profile.LifetimeStatistics.TotalWagered = 1_200_000m;

        _ = new ProgressionService(profile);
        CasinoVaultSnapshot snapshot = CasinoVaultService.Snapshot(profile);

        Assert.Equal("gold", snapshot.Tier.Id);
        Assert.Equal(0m, snapshot.CurrentAmount);
        Assert.Equal(1_200_000m, profile.Vault.ObservedEligibleWager);
        Assert.Equal(0m, profile.Vault.LifetimeContributions);
    }

    [Fact]
    public void NewWagerCreditsTwoPercentExactlyOnce()
    {
        PlayerProfileState profile = FreshProfile();
        profile.LifetimeStatistics.TotalWagered += 1_000m;

        CasinoVaultContribution first = CasinoVaultService.SyncFromStatistics(profile);
        CasinoVaultContribution duplicate = CasinoVaultService.SyncFromStatistics(profile);

        Assert.Equal(1_000m, first.EligibleWagerDelta);
        Assert.Equal(20m, first.RawContribution);
        Assert.Equal(20m, first.CreditedContribution);
        Assert.Equal(20m, profile.Vault.CurrentAmount);
        Assert.Equal(0m, duplicate.CreditedContribution);
        Assert.Equal(20m, profile.Vault.LifetimeContributions);
    }

    [Fact]
    public void ContributionRoundsToPlayMoneyCents()
    {
        PlayerProfileState profile = FreshProfile();
        profile.LifetimeStatistics.TotalWagered += 1.25m;

        CasinoVaultContribution contribution = CasinoVaultService.SyncFromStatistics(profile);

        Assert.Equal(0.03m, contribution.CreditedContribution);
    }

    [Fact]
    public void FullVaultClampsAndDiscardsOverflow()
    {
        PlayerProfileState profile = FreshProfile();
        profile.Vault.CurrentAmount = 9_995m;
        profile.LifetimeStatistics.TotalWagered += 1_000m;

        CasinoVaultContribution contribution = CasinoVaultService.SyncFromStatistics(profile);

        Assert.Equal(20m, contribution.RawContribution);
        Assert.Equal(5m, contribution.CreditedContribution);
        Assert.Equal(15m, contribution.DiscardedOverflow);
        Assert.Equal(10_000m, profile.Vault.CurrentAmount);
        Assert.True(contribution.IsFull);

        profile.LifetimeStatistics.TotalWagered += 5_000m;
        CasinoVaultContribution whileFull = CasinoVaultService.SyncFromStatistics(profile);
        Assert.Equal(0m, whileFull.CreditedContribution);
        Assert.Equal(10_000m, profile.Vault.CurrentAmount);
    }

    [Fact]
    public void GoldVaultPaysReservePlusTwentyPercentBonus()
    {
        var profile = new PlayerProfileState();
        profile.LifetimeStatistics.TotalWagered = 1_000_000m;
        _ = new ProgressionService(profile);
        profile.Vault.CurrentAmount = 50_000m;

        CasinoVaultOpenResult result = CasinoVaultService.PrepareOpen(profile, DateTimeOffset.UnixEpoch);

        Assert.Equal("gold", result.TierId);
        Assert.Equal(50_000m, result.BaseAmount);
        Assert.Equal(10_000m, result.BonusAmount);
        Assert.Equal(60_000m, result.TotalPayout);
        Assert.Equal(0.20m, result.BonusRate);
        Assert.Equal(result.PayoutId, profile.Vault.PendingPayoutId);
        Assert.Equal(60_000m, profile.Vault.PendingPayout);
        Assert.Equal(1, profile.Vault.VaultsOpened);
        Assert.Equal(60_000m, profile.Vault.LifetimePayouts);
        Assert.Equal(10_000m, profile.Vault.LifetimeBonuses);
        Assert.Equal(0m, profile.Vault.CurrentAmount);
    }

    [Fact]
    public void PendingPayoutCannotBePreparedTwice()
    {
        PlayerProfileState profile = FreshProfile();
        profile.Vault.CurrentAmount = 10_000m;
        _ = CasinoVaultService.PrepareOpen(profile);

        Assert.Throws<InvalidOperationException>(() => CasinoVaultService.PrepareOpen(profile));
    }

    [Fact]
    public void CompletingReceiptClearsPendingPayout()
    {
        PlayerProfileState profile = FreshProfile();
        profile.Vault.CurrentAmount = 10_000m;
        CasinoVaultOpenResult result = CasinoVaultService.PrepareOpen(profile);

        Assert.True(CasinoVaultService.CompletePendingPayout(profile, result.PayoutId));
        Assert.Equal(Guid.Empty, profile.Vault.PendingPayoutId);
        Assert.Equal(0m, profile.Vault.PendingPayout);
        Assert.False(CasinoVaultService.CompletePendingPayout(profile, result.PayoutId));
    }

    [Fact]
    public void TierUpgradeDoesNotMoveGoalpostsOnPartialVault()
    {
        PlayerProfileState profile = FreshProfile();
        profile.Vault.CurrentAmount = 5_000m;
        profile.LifetimeStatistics.TotalWagered += 1_000_000m;

        CasinoVaultContribution contribution = CasinoVaultService.SyncFromStatistics(profile);
        CasinoVaultSnapshot snapshot = CasinoVaultService.Snapshot(profile);

        Assert.Equal("bronze", snapshot.Tier.Id);
        Assert.Equal("gold", snapshot.UnlockedTier.Id);
        Assert.Equal(10_000m, profile.Vault.CurrentAmount);
        Assert.True(contribution.IsFull);

        CasinoVaultOpenResult result = CasinoVaultService.PrepareOpen(profile);
        Assert.True(CasinoVaultService.CompletePendingPayout(profile, result.PayoutId));
        Assert.Equal("gold", CasinoVaultService.Snapshot(profile).Tier.Id);
    }

    [Fact]
    public void EligibleWagerAggregatesOnlyCasinoWagerBuckets()
    {
        var profile = new PlayerProfileState();
        profile.LifetimeStatistics.TotalWagered = 600m;
        profile.Slots.TotalWagered = 100m;
        profile.NeonVault.TotalWagered = 200m;
        profile.NineLives.TotalWagered = 300m;
        profile.Manor.TotalWagered = 400m;
        profile.Kniffel.TotalWagered = 500m;
        profile.LegacyPooledSlots.TotalWagered = 700m;

        Assert.Equal(2_800m, CasinoVaultService.TotalEligibleWager(profile));
    }

    [Fact]
    public void WagersWhilePayoutPendingAdvanceLedgerWithoutBankingOverflow()
    {
        PlayerProfileState profile = FreshProfile();
        profile.Vault.CurrentAmount = 10_000m;
        _ = CasinoVaultService.PrepareOpen(profile);
        decimal observed = profile.Vault.ObservedEligibleWager;
        profile.LifetimeStatistics.TotalWagered += 2_000m;

        CasinoVaultContribution contribution = CasinoVaultService.SyncFromStatistics(profile);

        Assert.Equal(2_000m, contribution.EligibleWagerDelta);
        Assert.Equal(40m, contribution.DiscardedOverflow);
        Assert.Equal(observed + 2_000m, profile.Vault.ObservedEligibleWager);
        Assert.Equal(0m, profile.Vault.CurrentAmount);
    }

    private static PlayerProfileState FreshProfile()
    {
        var profile = new PlayerProfileState();
        _ = new ProgressionService(profile);
        return profile;
    }
}
