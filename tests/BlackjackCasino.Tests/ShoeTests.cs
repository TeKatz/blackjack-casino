using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class ShoeTests
{
    [Fact]
    public void SixDeckShoe_HasExpectedCardCountAndCutCard()
    {
        var shoe = new Shoe(new GameRules { DeckCount = 6, Penetration = 0.75 });

        Assert.Equal(312, shoe.TotalCards);
        Assert.Equal(312, shoe.Remaining);
        Assert.Equal(234, shoe.CutCardIndex);
    }

    [Fact]
    public void Draw_ReducesRemainingCount()
    {
        var shoe = new Shoe(new GameRules { DeckCount = 1 });
        _ = shoe.Draw();

        Assert.Equal(51, shoe.Remaining);
        Assert.Equal(1, shoe.Dealt);
    }
}
