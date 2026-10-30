using BlackjackCasino.Core;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Tests;

public sealed class CardSetAndNineLivesProgressionTests
{
    [Fact]
    public void ExistingProfile_GainsClassicCardSetDuringNormalization()
    {
        var profile = new PlayerProfileState
        {
            SchemaVersion = 4,
            EquippedCardSetId = string.Empty,
            OwnedCosmeticIds =
            [
                CosmeticCatalog.DefaultTitleId,
                CosmeticCatalog.DefaultFrameId,
                CosmeticCatalog.DefaultPortraitId,
            ],
        };

        _ = new ProgressionService(profile);

        Assert.Equal(5, profile.SchemaVersion);
        Assert.Equal(CosmeticCatalog.DefaultCardSetId, profile.EquippedCardSetId);
        Assert.Contains(CosmeticCatalog.DefaultCardSetId, profile.OwnedCosmeticIds);
    }

    [Fact]
    public void NormalCardSet_CanBePurchasedAndEquippedWithoutPrestige()
    {
        var profile = new PlayerProfileState { Coins = 2_000 };
        var service = new ProgressionService(profile);

        PurchaseResult result = service.PurchaseCosmetic("cardset-art-deco");

        Assert.True(result.Success);
        Assert.Equal(200, profile.Coins);
        Assert.True(service.EquipCosmetic("cardset-art-deco"));
        Assert.Equal("cardset-art-deco", profile.EquippedCardSetId);
    }

    [Fact]
    public void MidnightSovereign_RequiresPlayerPrestigeOneEvenWithEnoughCoins()
    {
        var profile = new PlayerProfileState { Coins = 20_000, Prestige = 0 };
        var service = new ProgressionService(profile);

        PurchaseResult locked = service.PurchaseCosmetic("cardset-midnight-sovereign");

        Assert.False(locked.Success);
        Assert.DoesNotContain("cardset-midnight-sovereign", profile.OwnedCosmeticIds);
        Assert.Equal(20_000, profile.Coins);

        profile.Prestige = 1;
        PurchaseResult unlocked = service.PurchaseCosmetic("cardset-midnight-sovereign");

        Assert.True(unlocked.Success);
        Assert.Equal(7_500, profile.Coins);
        Assert.True(service.EquipCosmetic("cardset-midnight-sovereign"));
        Assert.Equal("cardset-midnight-sovereign", profile.EquippedCardSetId);
    }

    [Fact]
    public void Shop_ContainsFourRegularCardSetsAndOnePrestigeCollection()
    {
        CosmeticDefinition[] cardSets = CosmeticCatalog.ShopItems.Where(item => item.Kind == CosmeticKind.CardSet).ToArray();

        Assert.Equal(5, cardSets.Length);
        Assert.Equal(4, cardSets.Count(item => item.RequiredPrestige == 0));
        CosmeticDefinition premium = Assert.Single(cardSets, item => item.RequiredPrestige == 1);
        Assert.Equal("cardset-midnight-sovereign", premium.Id);
    }

    [Fact]
    public void NineLivesChallenges_ReadTheDedicatedNineLivesStatistics()
    {
        var profile = new PlayerProfileState
        {
            NineLives = new NineLivesStatistics
            {
                Spins = 520,
                PaidSpins = 470,
                BonusSpins = 50,
                Wins = 111,
                TotalWagered = 30_000m,
                TotalReturned = 28_500m,
                BiggestPayout = 8_000m,
                BonusSessions = 12,
                BonusSpinsAwarded = 72,
                PounceWilds = 28,
                FullGridFrenzies = 1,
                BestPayoutMultiplier = 300m,
            },
        };

        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("nine-spins-500")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("nine-pounce-25")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("nine-frenzy-1")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("nine-multiplier-250")!));
        Assert.True(ChallengeCatalog.For(ChallengeCategory.NineLives).Count >= 14);
    }

    [Fact]
    public void LifetimeCasinoMetrics_IncludeNineLivesActivity()
    {
        var profile = new PlayerProfileState
        {
            NineLives = new NineLivesStatistics { Spins = 123, TotalWagered = 45_678m },
        };

        ChallengeDefinition wager = ChallengeCatalog.TryGet("lifetime-wager-10000000")!;
        ChallengeDefinition plays = ChallengeCatalog.TryGet("lifetime-plays-100000")!;

        Assert.Equal(45_678m, ChallengeCatalog.Progress(profile, wager));
        Assert.Equal(123m, ChallengeCatalog.Progress(profile, plays));
    }

    [Fact]
    public void ClaimedNineLivesAchievements_CanBoostEmpireRooms()
    {
        decimal slots = EmpireAchievementBoostCatalog.BonusForRoom(["nine-pounce-25"], "slots");
        decimal highLimit = EmpireAchievementBoostCatalog.BonusForRoom(["nine-frenzy-1"], "high-limit-slots");

        Assert.Equal(.05m, slots);
        Assert.Equal(.10m, highLimit);
    }
}
