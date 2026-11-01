using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class CollectionTests
{
    [Fact]
    public void CatalogHasTwelveFullThemesAndFourChaseThemesWithStableUniqueCards()
    {
        Assert.Equal(12, CollectionCatalog.Themes.Count(t => !t.UltraRare));
        Assert.Equal(4, CollectionCatalog.Themes.Count(t => t.UltraRare));
        Assert.All(CollectionCatalog.Themes, t => Assert.Equal(t.UltraRare ? 4 : 10, t.Cards.Count));
        Assert.Equal(136, CollectionCatalog.Cards.Count);
        Assert.Equal(136, CollectionCatalog.Cards.Select(c => c.Id).Distinct().Count());
        Assert.All(CollectionCatalog.Themes.Where(t => !t.UltraRare), t => Assert.Equal(Enum.GetValues<CollectionRarity>(), t.Cards.Select(c => c.Rarity).Distinct()));
        Assert.All(CollectionCatalog.Packs, p => Assert.Equal(1000, p.Weights.Sum()));
    }

    [Fact]
    public void MigrationGivesWelcomePacksOnceWithoutReplayingHistoricalRewards()
    {
        var profile = new PlayerProfileState { SchemaVersion = 5, Prestige = 8, Collection = null!, Coins = 456 };
        profile.Seasons[SeasonCatalog.Current.Id] = new() { Tier = 100, Experience = 120_000 };
        ProfileNormalization.Normalize(profile);
        ProfileNormalization.Normalize(profile);
        var reloaded = Reload(profile);
        Assert.Equal(6, reloaded.SchemaVersion);
        Assert.Equal(3, reloaded.Collection.Packs[CollectionRarity.Common]);
        Assert.Single(reloaded.Collection.Packs);
        Assert.Empty(reloaded.Collection.OwnedCardIds);
        Assert.Equal(456, reloaded.Coins);
    }

    [Theory]
    [InlineData(CollectionRarity.Common)]
    [InlineData(CollectionRarity.Uncommon)]
    [InlineData(CollectionRarity.Rare)]
    [InlineData(CollectionRarity.Epic)]
    [InlineData(CollectionRarity.Legendary)]
    public void EveryPackHasFiveCardsWithGuaranteedFloorAndBothPoolsRemainReachable(CollectionRarity rarity)
    {
        var profile = new PlayerProfileState();
        CollectionService.GrantPack(profile, rarity, 600);
        var random = new Random(7821 + (int)rarity);
        int ultra = 0, ordinary = 0;
        for (int i = 0; i < 600; i++)
        {
            CollectionOpening result = CollectionService.OpenPack(profile, rarity, random).Opening!;
            Assert.Equal(5, result.Cards.Count);
            Assert.Contains(result.Cards, d => CollectionCatalog.Find(d.CardId)!.Rarity >= rarity);
            foreach (CollectionDraw draw in result.Cards)
            {
                if (CollectionCatalog.Theme(CollectionCatalog.Find(draw.CardId)!).UltraRare) ultra++;
                else ordinary++;
            }
            Assert.True(CollectionService.CompleteOpening(profile, result.Id));
        }
        Assert.InRange(ultra, 1, 150);
        Assert.True(ordinary > 2800);
        Assert.InRange(profile.Collection.OwnedCardIds.Count, 1, 136);
    }

    [Fact]
    public void DuplicateCardsInsideOnePackConvertAutomaticallyAndKeepRemainder()
    {
        var profile = new PlayerProfileState();
        ProfileNormalization.Normalize(profile);
        profile.Collection.DuplicatePoints = 499;
        CollectionOpening result = CollectionService.OpenPack(profile, CollectionRarity.Common, new FixedRandom(false)).Opening!;
        Assert.Single(profile.Collection.OwnedCardIds);
        Assert.Single(result.Cards, c => c.IsNew);
        Assert.Equal(20, result.PointsEarned);
        Assert.Equal(1, result.PacksEarned);
        Assert.Equal(19, profile.Collection.DuplicatePoints);
        Assert.Equal(3, profile.Collection.Packs[CollectionRarity.Common]);
        Assert.Equal(new[] { 0, 5, 5, 5, 5 }, result.Cards.Select(c => c.DuplicatePoints));
    }

    [Fact]
    public void LegendaryDuplicatesCanCrossSeveralThresholdsInOneTransaction()
    {
        var profile = new PlayerProfileState();
        ProfileNormalization.Normalize(profile);
        profile.Collection.DuplicatePoints = 450;
        CollectionOpening result = CollectionService.OpenPack(profile, CollectionRarity.Common, new FixedRandom(true)).Opening!;
        Assert.All(result.Cards, c => Assert.True(CollectionCatalog.Theme(CollectionCatalog.Find(c.CardId)!).UltraRare));
        Assert.Equal(800, result.PointsEarned);
        Assert.Equal(2, result.PacksEarned);
        Assert.Equal(250, profile.Collection.DuplicatePoints);
        Assert.Equal(4, profile.Collection.Packs[CollectionRarity.Common]);
    }

    [Fact]
    public void PendingReceiptSurvivesReloadAndPreventsRerollsAndRepeatedConsumption()
    {
        var profile = new PlayerProfileState();
        CollectionOpening opening = CollectionService.OpenPack(profile, CollectionRarity.Common, new Random(1)).Opening!;
        var reloaded = Reload(profile);
        string before = JsonSerializer.Serialize(reloaded);
        Assert.False(CollectionService.OpenPack(reloaded, CollectionRarity.Common).Success);
        Assert.Equal(before, JsonSerializer.Serialize(reloaded));
        Assert.Equal(opening.Cards, reloaded.Collection.PendingOpening!.Cards);
        Assert.False(CollectionService.CompleteOpening(reloaded, Guid.NewGuid()));
        Assert.True(CollectionService.CompleteOpening(reloaded, opening.Id));
        Assert.False(CollectionService.CompleteOpening(reloaded, opening.Id));
        Assert.Equal(1, reloaded.Collection.PacksOpened);
    }

    [Fact]
    public void ConcurrentOpenConsumesOnlyOnePack()
    {
        var profile = new PlayerProfileState();
        Parallel.For(0, 20, _ => CollectionService.OpenPack(profile, CollectionRarity.Common));
        Assert.Equal(1, profile.Collection.PacksOpened);
        Assert.NotNull(profile.Collection.PendingOpening);
        Assert.Equal(2, profile.Collection.Packs[CollectionRarity.Common]);
    }

    [Fact]
    public void ShopChargesCoinsAndRejectsInsufficientFundsWithoutChanges()
    {
        var profile = new PlayerProfileState { Coins = 800 };
        ProfileNormalization.Normalize(profile);
        Assert.True(CollectionService.PurchasePack(profile, CollectionRarity.Epic).Success);
        Assert.Equal(0, profile.Coins);
        Assert.Equal(1, profile.Collection.Packs[CollectionRarity.Epic]);
        string snapshot = JsonSerializer.Serialize(profile);
        Assert.False(CollectionService.PurchasePack(profile, CollectionRarity.Legendary).Success);
        Assert.False(CollectionService.PurchasePack(profile, (CollectionRarity)99).Success);
        Assert.Equal(snapshot, JsonSerializer.Serialize(profile));
    }

    [Fact]
    public void ChallengeSingleAndBulkClaimsGrantExactlyOnePackPerNewClaim()
    {
        var profile = new PlayerProfileState { LifetimeStatistics = new() { RoundsPlayed = 100, HandsPlayed = 100, HandsWon = 60 } };
        var service = new ProgressionService(profile);
        ChallengeDefinition[] earned = ChallengeCatalog.All.Where(c => ChallengeCatalog.Complete(profile, c)).ToArray();
        Assert.NotEmpty(earned);
        int before = profile.Collection.Packs.Values.Sum();
        Assert.True(service.ClaimChallenge(earned[0].Id).Success);
        service.ClaimAllChallenges();
        Assert.Equal(before + earned.Length, profile.Collection.Packs.Values.Sum());
        foreach (CollectionRarity rarity in Enum.GetValues<CollectionRarity>())
            Assert.Equal((rarity == CollectionRarity.Common ? 3 : 0) + earned.Count(c => ProgressionService.ChallengePack(c) == rarity), profile.Collection.Packs.GetValueOrDefault(rarity));
        service = new ProgressionService(Reload(profile));
        Assert.False(service.ClaimChallenge(earned[0].Id).Success);
        Assert.False(service.ClaimAllChallenges().Success);
        Assert.Equal(before + earned.Length, service.State.Collection.Packs.Values.Sum());
    }

    [Fact]
    public void SeasonAwardsAllCrossedMilestonesOnceAndPrestigePreservesAlbum()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        service.GrantSeasonExperience(SeasonCatalog.Current.Id, 120_000);
        int packs = 3 + SeasonCatalog.Current.Tiers.Count(t => CollectionCatalog.SeasonPack(t.Tier) is not null);
        Assert.Equal(packs, profile.Collection.Packs.Values.Sum());
        service.GrantSeasonExperience(SeasonCatalog.Current.Id, 1);
        Assert.Equal(packs, profile.Collection.Packs.Values.Sum());
        var opening = CollectionService.OpenPack(profile, CollectionRarity.Common, new Random(4)).Opening!;
        string[] owned = profile.Collection.OwnedCardIds.ToArray();
        profile.Level = 100;
        Assert.True(service.Prestige().Success);
        Assert.Equal(0, profile.Level);
        Assert.Equal(owned, profile.Collection.OwnedCardIds);
        Assert.Equal(opening.Id, profile.Collection.PendingOpening!.Id);
        Assert.Equal(packs + 2, profile.Collection.Packs.Values.Sum());
        Assert.False(service.Prestige().Success);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void VaultGrantsPackAtPrepareOnlyAndRecoveryCannotDuplicateIt(int tierIndex)
    {
        var profile = new PlayerProfileState();
        ProfileNormalization.Normalize(profile);
        CasinoVaultTierDefinition tier = CasinoVaultTierCatalog.All[tierIndex];
        profile.LifetimeStatistics.TotalWagered = tier.RequiredLifetimeWager;
        profile.Vault.TierId = tier.Id;
        profile.Vault.CurrentAmount = tier.Capacity;
        int before = profile.Collection.Packs.GetValueOrDefault((CollectionRarity)tierIndex);
        CasinoVaultOpenResult result = CasinoVaultService.PrepareOpen(profile);
        Assert.Equal(before + 1, profile.Collection.Packs[(CollectionRarity)tierIndex]);
        var reloaded = Reload(profile);
        Assert.Throws<InvalidOperationException>(() => CasinoVaultService.PrepareOpen(reloaded));
        Assert.True(CasinoVaultService.CompletePendingPayout(reloaded, result.PayoutId));
        Assert.False(CasinoVaultService.CompletePendingPayout(reloaded, result.PayoutId));
        Assert.Equal(before + 1, reloaded.Collection.Packs[(CollectionRarity)tierIndex]);
    }

    [Fact]
    public void FailedPersistenceLeavesInventoryCoinsAndCardsUnchangedAndRetryWorks()
    {
        var profile = new PlayerProfileState { Coins = 1600 };
        bool fail = true;
        PlayerProfileState? saved = null;
        using var controller = new PlayerProgressionController(profile, collectionProfileSaver: candidate =>
        {
            if (fail) throw new IOException("disk full");
            saved = Reload(candidate);
        });
        string before = JsonSerializer.Serialize(profile);
        Assert.False(controller.OpenCollectionPack(CollectionRarity.Common).Success);
        Assert.Equal(before, JsonSerializer.Serialize(profile));
        Assert.False(controller.PurchaseCollectionPack(CollectionRarity.Legendary).Success);
        Assert.Equal(before, JsonSerializer.Serialize(profile));
        fail = false;
        CollectionActionResult result = controller.OpenCollectionPack(CollectionRarity.Common);
        Assert.True(result.Success);
        Assert.NotNull(saved!.Collection.PendingOpening);
        Assert.Equal(profile.Collection.OwnedCardIds, saved.Collection.OwnedCardIds);
        fail = true;
        Assert.False(controller.CompleteCollectionOpening(result.Opening!.Id).Success);
        Assert.NotNull(profile.Collection.PendingOpening);
        fail = false;
        Assert.True(controller.CompleteCollectionOpening(result.Opening.Id).Success);
    }

    [Fact]
    public void FailedChallengeSaveDoesNotLeakPackRewards()
    {
        var profile = new PlayerProfileState { LifetimeStatistics = new() { RoundsPlayed = 100, HandsPlayed = 100 } };
        using var controller = new PlayerProgressionController(profile, _ => throw new IOException("disk full"));
        string before = JsonSerializer.Serialize(profile);
        Assert.False(controller.ClaimAllChallenges().Success);
        Assert.Equal(before, JsonSerializer.Serialize(profile));
        var challenge = ChallengeCatalog.All.First(c => ChallengeCatalog.Complete(profile, c));
        Assert.False(controller.ClaimChallenge(challenge.Id).Success);
        Assert.Equal(before, JsonSerializer.Serialize(profile));
    }

    [Fact]
    public void MalformedCollectionDataIsSanitizedWithoutLosingTheProfile()
    {
        var profile = new PlayerProfileState { Coins = 789 };
        profile.Collection.Version = 1;
        profile.Collection.OwnedCardIds = ["unknown", null!];
        profile.Collection.Packs[(CollectionRarity)99] = 2;
        profile.Collection.Packs[CollectionRarity.Epic] = -3;
        profile.Collection.PendingOpening = new(Guid.NewGuid(), CollectionRarity.Common,
            Enumerable.Repeat(new CollectionDraw(null!, false, 0), 5).ToArray(), 0, 0);
        ProfileNormalization.Normalize(profile);
        Assert.Equal(789, profile.Coins);
        Assert.Empty(profile.Collection.OwnedCardIds);
        Assert.Equal(0, profile.Collection.Packs[CollectionRarity.Epic]);
        Assert.False(profile.Collection.Packs.ContainsKey((CollectionRarity)99));
        Assert.Null(profile.Collection.PendingOpening);
    }

    [Fact]
    public void AudioCuesAreFiniteBoundedAndScaleWithRarity()
    {
        foreach (bool opening in new[] { false, true })
        {
            int previous = 0;
            foreach (CollectionRarity rarity in Enum.GetValues<CollectionRarity>())
            {
                AudioClip clip = CollectionSoundSynthesis.Build(rarity, opening);
                Assert.True(clip.Frames > previous);
                Assert.Contains(clip.Samples, s => Math.Abs(s) > .001f);
                Assert.All(clip.Samples, s => Assert.True(float.IsFinite(s) && Math.Abs(s) <= .5f));
                previous = clip.Frames;
            }
        }
    }

    private static PlayerProfileState Reload(PlayerProfileState profile)
    {
        var reloaded = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;
        ProfileNormalization.Normalize(reloaded);
        return reloaded;
    }

    private sealed class FixedRandom(bool ultra) : Random
    {
        public override double NextDouble() => ultra ? 0 : .99;
        public override int Next(int maxValue) => 0;
    }
}
