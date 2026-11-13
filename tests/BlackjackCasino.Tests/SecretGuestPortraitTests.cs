using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class SecretGuestPortraitTests
{
    [Fact]
    public void ExclusiveChallengePortraitRetainsItsIdentityAndCrypticLore()
    {
        CosmeticDefinition guest = CosmeticCatalog.Get(CosmeticCatalog.SecretGuestPortraitId);
        Assert.Equal("portrait-secret-guest", guest.Id);
        Assert.Equal(CosmeticKind.Portrait, guest.Kind);
        Assert.Equal("The Secret Guest", guest.Name);
        Assert.Equal("portrait-secret-guest", guest.VisualVariant);
        Assert.Equal("She appears before the room remembers.", guest.Description);
        Assert.False(guest.ShopAvailable);
        Assert.Equal(0, guest.Price);
        Assert.Contains(CosmeticCatalog.All, x => x.Id == guest.Id);
    }
}
