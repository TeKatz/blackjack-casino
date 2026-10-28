namespace BlackjackCasino.Core;

public enum TablePresetId
{
    Casual,
    LasVegasStrip,
    DowntownVegas,
    AtlanticCity,
    European,
    SingleDeck,
    Custom,
}

public sealed record TablePreset(
    TablePresetId Id,
    string Name,
    string Description,
    GameRules Rules);

public static class TablePresets
{
    private static readonly IReadOnlyList<TablePreset> Presets =
    [
        new(
            TablePresetId.Casual,
            "Casual 3:2",
            "Relaxed default table: six decks, S17, 3:2 blackjack and forgiving actions.",
            new GameRules
            {
                DeckCount = 6,
                DealerHitsSoft17 = false,
                DealerPeeksForBlackjack = true,
                DealerTakesHoleCard = true,
                AllowInsurance = true,
                AllowLateSurrender = true,
                DoubleRule = DoubleRule.AnyTwo,
                DoubleAfterSplit = true,
                ResplitAces = false,
                HitSplitAces = false,
                MaxSplitHands = 4,
                BlackjackPayout = 1.5m,
                Penetration = 0.75,
                MinimumBet = 1m,
                MaximumBet = 1_000m,
            }),
        new(
            TablePresetId.LasVegasStrip,
            "Las Vegas Strip",
            "A modern six-deck H17 table with peek, DAS and a conventional 3:2 payout.",
            new GameRules
            {
                DeckCount = 6,
                DealerHitsSoft17 = true,
                DealerPeeksForBlackjack = true,
                DealerTakesHoleCard = true,
                AllowInsurance = true,
                AllowLateSurrender = false,
                DoubleRule = DoubleRule.AnyTwo,
                DoubleAfterSplit = true,
                ResplitAces = false,
                HitSplitAces = false,
                MaxSplitHands = 4,
                BlackjackPayout = 1.5m,
                Penetration = 0.75,
                MinimumBet = 10m,
                MaximumBet = 5_000m,
            }),
        new(
            TablePresetId.DowntownVegas,
            "Downtown Vegas",
            "A punchier two-deck H17 game with 3:2 blackjack and no surrender.",
            new GameRules
            {
                DeckCount = 2,
                DealerHitsSoft17 = true,
                DealerPeeksForBlackjack = true,
                DealerTakesHoleCard = true,
                AllowInsurance = true,
                AllowLateSurrender = false,
                DoubleRule = DoubleRule.AnyTwo,
                DoubleAfterSplit = true,
                ResplitAces = false,
                HitSplitAces = false,
                MaxSplitHands = 4,
                BlackjackPayout = 1.5m,
                Penetration = 0.65,
                MinimumBet = 5m,
                MaximumBet = 2_500m,
            }),
        new(
            TablePresetId.AtlanticCity,
            "Atlantic City",
            "Eight decks, S17, 3:2 blackjack, DAS and late surrender.",
            new GameRules
            {
                DeckCount = 8,
                DealerHitsSoft17 = false,
                DealerPeeksForBlackjack = true,
                DealerTakesHoleCard = true,
                AllowInsurance = true,
                AllowLateSurrender = true,
                DoubleRule = DoubleRule.AnyTwo,
                DoubleAfterSplit = true,
                ResplitAces = true,
                HitSplitAces = false,
                MaxSplitHands = 4,
                BlackjackPayout = 1.5m,
                Penetration = 0.75,
                MinimumBet = 10m,
                MaximumBet = 5_000m,
            }),
        new(
            TablePresetId.European,
            "European No-Hole-Card",
            "Six-deck S17 ENHC: the dealer takes the second card only after player actions.",
            new GameRules
            {
                DeckCount = 6,
                DealerHitsSoft17 = false,
                DealerPeeksForBlackjack = false,
                DealerTakesHoleCard = false,
                AllowInsurance = true,
                AllowLateSurrender = false,
                DoubleRule = DoubleRule.NineToEleven,
                DoubleAfterSplit = true,
                ResplitAces = false,
                HitSplitAces = false,
                MaxSplitHands = 4,
                BlackjackPayout = 1.5m,
                Penetration = 0.75,
                MinimumBet = 5m,
                MaximumBet = 5_000m,
            }),
        new(
            TablePresetId.SingleDeck,
            "Single Deck",
            "One deck, H17, 3:2 blackjack and tighter doubling/splitting rules.",
            new GameRules
            {
                DeckCount = 1,
                DealerHitsSoft17 = true,
                DealerPeeksForBlackjack = true,
                DealerTakesHoleCard = true,
                AllowInsurance = true,
                AllowLateSurrender = false,
                DoubleRule = DoubleRule.TenToEleven,
                DoubleAfterSplit = false,
                ResplitAces = false,
                HitSplitAces = false,
                MaxSplitHands = 2,
                BlackjackPayout = 1.5m,
                Penetration = 0.55,
                MinimumBet = 5m,
                MaximumBet = 1_000m,
            }),
    ];

    public static IReadOnlyList<TablePreset> All => Presets;

    public static TablePreset Get(TablePresetId id)
    {
        if (id == TablePresetId.Custom)
        {
            throw new ArgumentException("Custom rules are stored by the caller and do not have a fixed preset.", nameof(id));
        }

        return Presets.First(x => x.Id == id);
    }
}
