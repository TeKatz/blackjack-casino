using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

/// <summary>Shared, WPF-independent names for the exact 13 score categories.</summary>
internal static class KniffelCategoryLabels
{
    internal static string For(KniffelCategory category) => category switch
    {
        KniffelCategory.Ones => "Ones", KniffelCategory.Twos => "Twos",
        KniffelCategory.Threes => "Threes", KniffelCategory.Fours => "Fours",
        KniffelCategory.Fives => "Fives", KniffelCategory.Sixes => "Sixes",
        KniffelCategory.ThreeOfAKind => "Three of a kind",
        KniffelCategory.FourOfAKind => "Four of a kind",
        KniffelCategory.FullHouse => "Full House", KniffelCategory.SmallStraight => "Small straight",
        KniffelCategory.LargeStraight => "Large straight",
        KniffelCategory.Kniffel => "Five of a kind", _ => "Chance",
    };
}
