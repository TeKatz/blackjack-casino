using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class ShopExpansionTests
{
    public static TheoryData<string, CosmeticKind> NewSets => new()
    {
        { "cardset-linen", CosmeticKind.CardSet }, { "cardset-harbor", CosmeticKind.CardSet },
        { "cardset-rosewood", CosmeticKind.CardSet }, { "cardset-jade-filigree", CosmeticKind.CardSet },
        { "cardset-celestial-opal", CosmeticKind.CardSet }, { "cardset-solar-eclipse", CosmeticKind.CardSet },
        { "diceset-porcelain", CosmeticKind.DiceSet }, { "diceset-forest", CosmeticKind.DiceSet },
        { "diceset-copper", CosmeticKind.DiceSet }, { "diceset-sapphire", CosmeticKind.DiceSet },
        { "diceset-astral-opal", CosmeticKind.DiceSet }, { "diceset-solar-crown", CosmeticKind.DiceSet },
    };

    [Theory]
    [MemberData(nameof(NewSets))]
    public void NewSet_PurchasesOnceEquipsAndSurvivesReload(string id, CosmeticKind kind)
    {
        CosmeticDefinition cosmetic = CosmeticCatalog.Get(id);
        var profile = new PlayerProfileState { Coins = cosmetic.Price };
        var service = new ProgressionService(profile);
        Assert.True(cosmetic.ShopAvailable);
        Assert.Equal(kind, cosmetic.Kind);
        Assert.False(service.EquipCosmetic(id));
        Assert.True(service.PurchaseCosmetic(id).Success);
        Assert.Equal(0, profile.Coins);
        Assert.True(service.EquipCosmetic(id));
        Assert.False(service.PurchaseCosmetic(id).Success);
        Assert.Equal(0, profile.Coins);

        PlayerProfileState restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;
        _ = new ProgressionService(restored);
        Assert.Contains(id, restored.OwnedCosmeticIds);
        Assert.Equal(id, kind == CosmeticKind.CardSet ? restored.EquippedCardSetId : restored.EquippedDiceSetId);
        Assert.Equal(kind == CosmeticKind.CardSet ? CosmeticCatalog.DefaultDiceSetId : CosmeticCatalog.DefaultCardSetId,
            kind == CosmeticKind.CardSet ? restored.EquippedDiceSetId : restored.EquippedCardSetId);
    }

    [Theory]
    [MemberData(nameof(NewSets))]
    public void NewSet_InsufficientCoinsCannotPurchaseOrEquip(string id, CosmeticKind kind)
    {
        CosmeticDefinition cosmetic = CosmeticCatalog.Get(id);
        var profile = new PlayerProfileState { Coins = cosmetic.Price - 1 };
        var service = new ProgressionService(profile);
        Assert.Equal(kind, cosmetic.Kind);
        Assert.False(service.PurchaseCosmetic(id).Success);
        Assert.False(service.EquipCosmetic(id));
        Assert.Equal(cosmetic.Price - 1, profile.Coins);
        Assert.DoesNotContain(id, profile.OwnedCosmeticIds);
    }
}
